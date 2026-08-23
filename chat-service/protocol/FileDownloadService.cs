using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 文件下载服务，支持断点续传。对应 chat-storage 的 FileDownloadService。
    ///
    /// 流程（独立 socket 连接到下载端口，默认 10088）：
    /// 1. 发送 metaFrame(0x01) 请求 {fileId, taskId, startOffset, userId, userName, transferToken}；
    /// 2. 服务端返回 metaFrame/ackFrame 元数据（含 fileSize/startOffset）；
    /// 3. 客户端回 ackFrame {taskId, status:"ready"}；
    /// 4. 服务端持续推 dataFrame(0x02 原始字节) 并写入 .part 临时文件；
    /// 5. 服务端发 endFrame(0x03) {status:"success", sentBytes, endOffset}，客户端校验完整性后将 .part 更名为最终文件。
    /// </summary>
    public class FileDownloadService
    {
        private string downloadHost;
        private int downloadPort;

        public FileDownloadService(string downloadHost, int downloadPort)
        {
            this.downloadHost = downloadHost;
            this.downloadPort = downloadPort;
        }

        public delegate void ProgressHandler(double progress, string speed);

        /// <summary>
        /// 下载文件到本地目标路径。
        /// </summary>
        public void DownloadFile(
            long fileId,
            string targetPath,
            long fileSize,
            int userId,
            string userName,
            string transferToken,
            string taskId,
            long startOffset,
            ProgressHandler progressHandler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string partPath = targetPath + ".part";
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 确定断点偏移：以本地 .part 已存在大小为准
            long localPartSize = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
            long resolvedOffset = (startOffset > 0) ? Math.Min(startOffset, localPartSize) : localPartSize;
            // 校验完整性：若本地只有部分且与 fileSize 不一致，保持部分大小；否则归零重下
            if (fileSize >= 0 && localPartSize > fileSize)
            {
                resolvedOffset = 0;
                if (File.Exists(partPath)) File.Delete(partPath);
            }

            using (Socket downloadSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                downloadSocket.Connect(downloadHost, downloadPort);
                downloadSocket.NoDelay = true;

                // 准备 .part 文件的写入句柄
                FileStream writeStream = new FileStream(
                    partPath,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.None);
                writeStream.SetLength(resolvedOffset);
                writeStream.Seek(resolvedOffset, SeekOrigin.Begin);

                long receivedSize = resolvedOffset;
                long totalSize = fileSize;
                DateTime lastLogTime = DateTime.Now;
                long lastBytes = resolvedOffset;

                var requestDict = new Dictionary<string, object>
                {
                    { "fileId", fileId },
                    { "taskId", taskId },
                    { "startOffset", resolvedOffset },
                    { "userId", userId },
                    { "userName", userName },
                    { "transferToken", transferToken }
                };
                Frame requestFrame = FrameBuilder.Build(FrameTypeEnum.MetaFrame, requestDict);
                downloadSocket.Send(requestFrame.ToBytes());

                // 接收循环
                byte[] recvBuf = new byte[64 * 1024];
                List<byte> buffer = new List<byte>();
                bool completed = false;

                try
                {
                    while (!completed)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int read = downloadSocket.Receive(recvBuf);
                        if (read <= 0) throw new SocketException(10054);
                        for (int i = 0; i < read; i++) buffer.Add(recvBuf[i]);

                        while (TryExtractFrame(buffer, out Frame frame))
                        {
                            switch (frame.Type)
                            {
                                case FrameTypeEnum.MetaFrame:
                                case FrameTypeEnum.AckFrame:
                                    HandleMetaOrAck(frame, taskId, resolvedOffset, downloadSocket, ref totalSize);
                                    break;

                                case FrameTypeEnum.DataFrame:
                                    // 原始数据帧，直接写入文件
                                    byte[] payload = frame.Data;
                                    writeStream.Write(payload, 0, payload.Length);
                                    writeStream.Flush();
                                    receivedSize += payload.Length;

                                    DateTime now = DateTime.Now;
                                    double timeDelta = (now - lastLogTime).TotalSeconds;
                                    if (timeDelta >= 0.5)
                                    {
                                        double deltaBytes = receivedSize - lastBytes;
                                        double speed = timeDelta > 0 ? deltaBytes / timeDelta : 0;
                                        double progress = totalSize > 0 ? (double)receivedSize / totalSize : 0;
                                        ReportProgress(progressHandler, Math.Min(progress, 1.0), FormatSpeed(speed));
                                        lastLogTime = now;
                                        lastBytes = receivedSize;
                                    }
                                    break;

                                case FrameTypeEnum.EndFrame:
                                    HandleEndFrame(frame, taskId, ref receivedSize, totalSize, resolvedOffset);
                                    completed = true;
                                    break;

                                case FrameTypeEnum.FileResponse:
                                    // 错误响应
                                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(frame.GetDataAsString());
                                    if (dict != null && dict.ContainsKey("code"))
                                    {
                                        int code = Convert.ToInt32(dict["code"]);
                                        if (code != 200)
                                        {
                                            string msg = dict.ContainsKey("message") || dict.ContainsKey("msg")
                                                ? Convert.ToString(dict.ContainsKey("message") ? dict["message"] : dict["msg"])
                                                : "下载失败";
                                            throw new TransferException(msg);
                                        }
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
                finally
                {
                    writeStream.Close();
                }

                // 完成：将 .part 更名为最终文件
                if (File.Exists(targetPath)) File.Delete(targetPath);
                File.Move(partPath, targetPath);
                ReportProgress(progressHandler, 1.0, "完成");
            }
        }

        private void HandleMetaOrAck(Frame frame, string taskId, long resolvedOffset, Socket socket, ref long totalSize)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(frame.GetDataAsString());
            if (dict == null) throw new TransferException("下载元数据无效");

            if (dict.ContainsKey("status"))
            {
                string status = Convert.ToString(dict["status"]);
                if (status == "error" || status == "fail")
                {
                    string msg = dict.ContainsKey("message") ? Convert.ToString(dict["message"]) : "下载失败";
                    throw new TransferException(msg);
                }
            }

            if (dict.ContainsKey("fileSize"))
            {
                totalSize = Convert.ToInt64(dict["fileSize"]);
            }

            // 回 ready
            var ready = new Dictionary<string, object>
            {
                { "taskId", taskId },
                { "status", "ready" }
            };
            Frame readyFrame = FrameBuilder.Build(FrameTypeEnum.AckFrame, ready);
            socket.Send(readyFrame.ToBytes());
        }

        private void HandleEndFrame(Frame frame, string taskId, ref long receivedSize, long totalSize, long resolvedOffset)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(frame.GetDataAsString());
            if (dict == null) throw new TransferException("结束帧无效");

            string status = dict.ContainsKey("status") ? Convert.ToString(dict["status"]) : "";
            if (status != "success")
            {
                string msg = dict.ContainsKey("message") ? Convert.ToString(dict["message"]) : "下载失败";
                throw new TransferException(msg);
            }
        }

        private static void ReportProgress(ProgressHandler handler, double progress, string speed)
        {
            if (handler != null)
            {
                try { handler(progress, speed); } catch { }
            }
        }

        private static string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024) return string.Format("{0:0} B/s", bytesPerSecond);
            if (bytesPerSecond < 1024 * 1024) return string.Format("{0:0.0} KB/s", bytesPerSecond / 1024);
            return string.Format("{0:0.0} MB/s", bytesPerSecond / (1024 * 1024));
        }

        /// <summary>从缓冲区头部抽取完整帧。</summary>
        private static bool TryExtractFrame(List<byte> buffer, out Frame frame)
        {
            frame = null;
            if (buffer.Count < Frame.HEADER_LENGTH) return false;

            int start = -1;
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                if (buffer[i] == Frame.MAGIC[0] && buffer[i + 1] == Frame.MAGIC[1])
                {
                    start = i;
                    break;
                }
            }
            if (start < 0)
            {
                buffer.Clear();
                return false;
            }
            if (start > 0)
            {
                buffer.RemoveRange(0, start);
            }

            if (buffer.Count < Frame.HEADER_LENGTH) return false;
            uint length = ((uint)buffer[4] << 24) | ((uint)buffer[5] << 16)
                        | ((uint)buffer[6] << 8) | buffer[7];
            int total = Frame.HEADER_LENGTH + (int)length;
            if (buffer.Count < total) return false;

            byte[] frameBytes = buffer.Take(total).ToArray();
            try
            {
                frame = Frame.FromBytes(frameBytes, 0, total);
            }
            catch
            {
                buffer.RemoveAt(0);
                return false;
            }
            buffer.RemoveRange(0, total);
            return true;
        }
    }
}
