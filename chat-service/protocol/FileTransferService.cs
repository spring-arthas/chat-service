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
    /// 文件上传服务，支持断点续传。对应 chat-storage 的 FileTransferService.uploadFile。
    ///
    /// 流程：
    /// 1. 连接到上传端口（默认 10087）；
    /// 2. 计算文件 MD5；
    /// 3. 发送断点检查帧 resumeCheck(0x05)，等待 resumeAck(0x06)：
    ///    - status="resume" -> 从 uploadedSize 继续传；
    ///    - status="new" -> 发送 metaFrame(0x01) 全新上传，等 ackFrame(0x04)；
    ///    - status="complete" -> 已完成，直接返回 fileId；
    /// 4. 循环发送 dataFrame(0x02)（payload = offset(8B大端) + 分块数据），按需请求进度 ACK；
    /// 5. 发送 endFrame(0x03)，等待 ackFrame(0x04) 完成。
    /// </summary>
    public class FileTransferService
    {
        private const int ChunkSize = 8 * 1024;             // 8KB 分块
        private const long UploadAckWindowBytes = 4L * 1024 * 1024; // 4MB 进度确认窗口

        private string uploadHost;
        private int uploadPort;

        public FileTransferService(string uploadHost, int uploadPort)
        {
            this.uploadHost = uploadHost;
            this.uploadPort = uploadPort;
        }

        /// <summary>
        /// 上传文件。返回服务端分配的 fileId。
        /// </summary>
        /// <param name="filePath">本地文件路径</param>
        /// <param name="targetDirId">目标目录 ID</param>
        /// <param name="userId">用户 ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="transferToken">传输令牌</param>
        /// <param name="taskId">任务 ID（唯一，用于断点续传匹配）</param>
        /// <param name="progressHandler">进度回调 (0.0-1.0, 速度字符串)</param>
        /// <param name="cancellationToken">取消令牌</param>
        public long? UploadFile(
            string filePath,
            long targetDirId,
            int userId,
            string userName,
            string transferToken,
            string taskId,
            Action<double, string> progressHandler = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("文件不存在", filePath);

            string fileName = Path.GetFileName(filePath);
            long fileSize = new FileInfo(filePath).Length;
            string fileType = Path.GetExtension(filePath).TrimStart('.');

            // 使用独立 socket 连到上传端口
            using (Socket uploadSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                uploadSocket.Connect(uploadHost, uploadPort);
                uploadSocket.NoDelay = true;

                // 1. 计算 MD5
                ReportProgress(progressHandler, 0, "计算MD5...");
                string md5 = Md5Helper.ComputeContentMd5(filePath);

                // 2. 发送断点检查
                var resumeRequest = new ResumeCheckRequest
                {
                    Md5 = md5,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileType = fileType,
                    DirId = targetDirId,
                    UserId = userId,
                    UserName = userName,
                    TaskId = taskId,
                    TransferToken = transferToken
                };
                Frame resumeFrame = FrameBuilder.Build(FrameTypeEnum.ResumeCheck, resumeRequest);

                long offset = 0;
                var resumeAck = SendTransferAndWait<ResumeAckResponse>(uploadSocket, resumeFrame, FrameTypeEnum.ResumeAck, taskId, 30000);

                if (resumeAck.Status == "complete")
                {
                    ReportProgress(progressHandler, 1.0, "完成");
                    return resumeAck.FileId;
                }

                if (resumeAck.Status == "resume")
                {
                    offset = resumeAck.UploadedSize ?? 0;
                }
                else if (resumeAck.Status == "new")
                {
                    // 全新上传：发送 metaFrame
                    var metaRequest = new FileMetaRequest
                    {
                        Md5 = md5,
                        FileName = fileName,
                        FileSize = fileSize,
                        FileType = fileType,
                        DirId = targetDirId,
                        UserId = userId,
                        UserName = userName,
                        TaskId = taskId,
                        TransferToken = transferToken
                    };
                    Frame metaFrame = FrameBuilder.Build(FrameTypeEnum.MetaFrame, metaRequest);
                    StandardAckResponse ack = SendTransferAndWait<StandardAckResponse>(uploadSocket, metaFrame, FrameTypeEnum.AckFrame, taskId, 30000);
                    if (ack.Status != "ready")
                    {
                        throw new TransferException("服务端未就绪: " + (ack.Message ?? "未知错误"));
                    }
                    offset = ack.UploadedSize ?? 0;
                }
                else if (resumeAck.Status == "error")
                {
                    // 服务端拒绝（如登录凭证/目录/令牌无效），直接回显服务端原因，便于排查。
                    throw new TransferException("上传被拒绝: " + (resumeAck.Message ?? "未知错误"));
                }
                else
                {
                    throw new TransferException("未知断点状态: " + (resumeAck.Status ?? "null"));
                }

                // 3. 发送文件数据
                if (offset < fileSize)
                {
                    offset = SendFileData(uploadSocket, filePath, offset, fileSize, taskId, progressHandler, cancellationToken);
                }

                // 4. 发送结束帧
                var endRequest = new EndUploadRequest { TaskId = taskId };
                Frame endFrame = FrameBuilder.Build(FrameTypeEnum.EndFrame, endRequest);
                StandardAckResponse finalAck = SendTransferAndWait<StandardAckResponse>(uploadSocket, endFrame, FrameTypeEnum.AckFrame, taskId, 60000);

                if (finalAck.Status != "success")
                {
                    throw new TransferException("上传最终确认失败: " + (finalAck.Message ?? "未知错误"));
                }

                ReportProgress(progressHandler, 1.0, "完成");
                return finalAck.FileId;
            }
        }

        /// <summary>发送文件数据分块，返回最终偏移量。</summary>
        private long SendFileData(
            Socket socket,
            string filePath,
            long offset,
            long fileSize,
            string taskId,
            Action<double, string> progressHandler,
            CancellationToken cancellationToken)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                fs.Seek(offset, SeekOrigin.Begin);

                long currentOffset = offset;
                long lastAckOffset = offset;
                DateTime lastLogTime = DateTime.Now;
                long lastOffsetForSpeed = offset;
                byte[] buffer = new byte[ChunkSize];

                while (currentOffset < fileSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    long remaining = fileSize - currentOffset;
                    int readLength = (int)Math.Min(ChunkSize, remaining);
                    int read = fs.Read(buffer, 0, readLength);
                    if (read <= 0) break;

                    long nextOffset = currentOffset + read;
                    bool needsAck = ShouldRequestUploadAck(nextOffset, fileSize, lastAckOffset);

                    // payload = offset(8B大端) + data
                    byte[] payload = BuildDataPayload(currentOffset, buffer, read);
                    byte flags = Frame.FLAG_HAS_OFFSET;
                    if (needsAck) flags |= Frame.FLAG_NEED_ACK;

                    Frame dataFrame = new Frame(FrameTypeEnum.DataFrame, payload, flags);

                    if (needsAck)
                    {
                        StandardAckResponse ack = SendTransferAndWait<StandardAckResponse>(socket, dataFrame, FrameTypeEnum.AckFrame, taskId, 30000);
                        long confirmedOffset = ack.UploadedSize ?? 0;
                        if (confirmedOffset < nextOffset)
                        {
                            // 服务端进度落后，回退重传
                            fs.Seek(confirmedOffset, SeekOrigin.Begin);
                            currentOffset = confirmedOffset;
                            lastAckOffset = confirmedOffset;
                            lastOffsetForSpeed = confirmedOffset;
                            continue;
                        }
                        lastAckOffset = confirmedOffset;
                    }
                    else
                    {
                        SendFrame(socket, dataFrame);
                    }

                    currentOffset = nextOffset;

                    // 进度回调（节流）
                    DateTime now = DateTime.Now;
                    double timeDelta = (now - lastLogTime).TotalSeconds;
                    if (timeDelta >= 0.5 || currentOffset == fileSize)
                    {
                        double bytesSinceLast = currentOffset - lastOffsetForSpeed;
                        double speed = timeDelta > 0 ? bytesSinceLast / timeDelta : 0;
                        ReportProgress(progressHandler, (double)currentOffset / fileSize, FormatSpeed(speed));
                        lastLogTime = now;
                        lastOffsetForSpeed = currentOffset;
                    }
                }

                return currentOffset;
            }
        }

        private static bool ShouldRequestUploadAck(long nextOffset, long fileSize, long lastAckOffset)
        {
            if (nextOffset >= fileSize) return true;
            return (nextOffset - lastAckOffset) >= UploadAckWindowBytes;
        }

        private static byte[] BuildDataPayload(long offset, byte[] data, int length)
        {
            byte[] offsetBytes = BitConverter.GetBytes((ulong)offset);
            if (BitConverter.IsLittleEndian) Array.Reverse(offsetBytes); // 大端

            byte[] payload = new byte[8 + length];
            Buffer.BlockCopy(offsetBytes, 0, payload, 0, 8);
            Buffer.BlockCopy(data, 0, payload, 8, length);
            return payload;
        }

        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec < 1024) return string.Format("{0:0} B/s", bytesPerSec);
            if (bytesPerSec < 1024 * 1024) return string.Format("{0:0.0} KB/s", bytesPerSec / 1024);
            return string.Format("{0:0.0} MB/s", bytesPerSec / (1024 * 1024));
        }

        private static void ReportProgress(Action<double, string> handler, double progress, string speed)
        {
            if (handler != null)
            {
                try { handler(progress, speed); } catch { }
            }
        }

        // ============ 独立 socket 上的同步请求-响应 ============

        /// <summary>
        /// 在独立传输 socket 上发送帧并同步等待响应，按 taskId 匹配。
        /// </summary>
        private static T SendTransferAndWait<T>(Socket socket, Frame frame, FrameTypeEnum responseType, string taskId, int timeoutMs)
        {
            byte[] data = frame.ToBytes();
            socket.Send(data);

            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            List<byte> buffer = new List<byte>();
            byte[] recvBuf = new byte[64 * 1024];

            while (true)
            {
                int remaining = (int)(deadline - DateTime.Now).TotalMilliseconds;
                if (remaining <= 0) throw new TimeoutException("传输响应超时: " + frame.Type.Describe());

                socket.ReceiveTimeout = remaining;
                int read;
                try
                {
                    read = socket.Receive(recvBuf);
                }
                catch (SocketException)
                {
                    throw;
                }
                if (read <= 0) throw new SocketException(10054);

                for (int i = 0; i < read; i++) buffer.Add(recvBuf[i]);

                // 从缓冲区中逐个抽取完整帧，匹配响应
                while (TryExtractFrame(buffer, out Frame candidate))
                {
                    if (candidate.Type == responseType && FrameMatchesTaskId(candidate, taskId))
                    {
                        string json = candidate.GetDataAsString();
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                    // 否则丢弃该帧，继续等待
                }
            }
        }

        /// <summary>
        /// 从缓冲区头部抽取一个完整帧；遇到魔数错位时跳过 1 字节重新对齐。
        /// 抽取成功后从缓冲区移除该帧及其之前的字节。
        /// </summary>
        private static bool TryExtractFrame(List<byte> buffer, out Frame frame)
        {
            frame = null;
            if (buffer.Count < Frame.HEADER_LENGTH) return false;

            // 找到魔数起始位置
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
                // 未找到魔数，清空缓冲区重新累积
                buffer.Clear();
                return false;
            }

            // 去掉魔数之前的错位字节
            if (start > 0)
            {
                buffer.RemoveRange(0, start);
            }

            // 读取长度字段
            if (buffer.Count < Frame.HEADER_LENGTH) return false;
            uint length = ((uint)buffer[4] << 24) | ((uint)buffer[5] << 16)
                        | ((uint)buffer[6] << 8) | buffer[7];
            int total = Frame.HEADER_LENGTH + (int)length;
            if (buffer.Count < total) return false; // 数据不完整

            byte[] frameBytes = buffer.Take(total).ToArray();
            try
            {
                frame = Frame.FromBytes(frameBytes, 0, total);
            }
            catch
            {
                // 解析失败，丢弃 1 字节重新对齐
                buffer.RemoveAt(0);
                return false;
            }

            buffer.RemoveRange(0, total);
            return true;
        }

        private static bool FrameMatchesTaskId(Frame frame, string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return true;
            try
            {
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(frame.GetDataAsString());
                if (obj == null) return false;
                return obj.ContainsKey("taskId") && Convert.ToString(obj["taskId"]) == taskId;
            }
            catch
            {
                return false;
            }
        }

        private static void SendFrame(Socket socket, Frame frame)
        {
            byte[] data = frame.ToBytes();
            socket.Send(data);
        }
    }

    public class TransferException : Exception
    {
        public TransferException(string message) : base(message) { }
    }
}
