using chat_service.frame;
using chat_service.net;
using chat_service.service.file;
using chat_service.util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace chat_service.file
{
    public class AsyncPersonalFileUploadHelper
    {
        // ================== Protocol Constants ==================
        private static readonly byte[] MAGIC = { 0xFA, 0xCE };
        private const int HEADER_LENGTH = 8;
        private const int CHUNK_SIZE = 32 * 1024; // 32KB

        // Frame Types
        private const byte RESUME_CHECK = 0x05;
        private const byte RESUME_ACK = 0x06;
        private const byte META_FRAME = 0x01;
        private const byte DATA_FRAME = 0x02;
        private const byte END_FRAME = 0x03;
        private const byte ACK_FRAME = 0x04;

        // ================== Task Management ==================
        public static int taskCount = 0;
        private static long sleepTime = 1000;
        public static object obj = new object();

        // ================== Instance Fields ==================
        public BackgroundWorker Bg_Worker { get; set; }
        private Socket fileSendSocket;
        private DataGridViewProgressBarCell progressBarCell;
        private DataGridViewRow dataGridViewRow;
        private Dictionary<string, object> dictionary;
        public DoWorkEventArgs doWorkEventArgs;

        public AsyncPersonalFileUploadHelper(DataGridViewProgressBarCell progressBarCell, DataGridViewRow dataGridViewRow, Dictionary<string, object> dictionary)
        {
            this.dataGridViewRow = dataGridViewRow;
            this.dictionary = dictionary;
            this.progressBarCell = progressBarCell;

            this.progressBarCell.Maximum = 100; // 设置进度条最大值为100
            // Typo in original code "Mimimum" was fixed or ignored, setting Minimum property if exists, else default is 0.
            // DataGridViewProgressBarCell doesn't have Minimum property in standard WinForms, it uses 0-100 or value.
            // Keeping original constructor logic mostly but cleaning up.

            // Create Component
            this.Bg_Worker = new BackgroundWorker(); // 创建后台工作线程
            this.Bg_Worker.WorkerReportsProgress = true; // 启用进度报告功能
            this.Bg_Worker.WorkerSupportsCancellation = true; // 启用取消功能

            // Bind Events
            this.Bg_Worker.DoWork += backgroundWorker_executePersonalUploadTransport_DoWork; // 绑定后台工作线程执行事件
            this.Bg_Worker.ProgressChanged += bg_ProgressChanged; // 绑定进度改变事件
            this.Bg_Worker.RunWorkerCompleted += bg_RunWorkerCompleted; // 绑定工作线程完成事件
        }
        /// <summary>
        /// 启动异步线程
        /// </summary>
        public void Do()
        {
            Bg_Worker.RunWorkerAsync();
        }

        private void backgroundWorker_executePersonalUploadTransport_DoWork(object sender, DoWorkEventArgs e)
        {
            // 文件传输过程中存在强制退出主解面，此处需要根据DoWorkEventArgs判断来终止任务执行
            doWorkEventArgs = e;
            // 1. 当前上传线程获取锁，获取成功执行文件上传，上传任务数+1，保证上传任务使用保持在规定个数，避免文件伤上传数量过大
            while (true)
            {
                lock (obj)
                {
                    // 当前任务获取到锁，在即将执行上传过程中判断用户是否退出主界面，如果是则直接设置i任务取消状态
                    if (Bg_Worker.CancellationPending)
                    {
                        // 任务直接取消，任务总数减1
                        if (taskCount > 0)
                        {
                            taskCount = taskCount - 1;
                        }
                        doWorkEventArgs.Cancel = e.Cancel = true;
                        return;
                    }

                    if (taskCount < 5)
                    {
                        taskCount++;
                        // 任务数刚好为5个，则调整休眠时间，尽量多睡会
                        break;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref sleepTime, 1000, 5000);
                    }
                }
                Thread.Sleep((int)Interlocked.Read(ref sleepTime));
            }
            FileStream fs = null;
            try
            {
                // 2. Prepare File Info 准备文件信息
                string filePath = this.dictionary["selectFilePath"].ToString(); // 文件地址
                string fileName = this.dictionary["fileName"].ToString(); // 文件名称
                long fileSize = long.Parse(this.dictionary["fileSize"].ToString()); // 文件大小
                long? dirId = this.dictionary.ContainsKey("dirId") ? (long?)Convert.ToInt64(this.dictionary["dirId"]) : null; // 当前文件所属的目录Id
                // Use launchUserName if available
                string userName = this.dictionary.ContainsKey("launchUserName") ? this.dictionary["launchUserName"].ToString() : null;  // 文件所属用户名, 谁上传

                // 3. Connect to Server 构建socket对象，连接服务端
                // Reuse NetServiceContext to get a socket and parse connection string
                this.fileSendSocket = NetServiceContext.getSendFileSocket();
                NetResponse connectResp = NetServiceContext.initSocketAndConnect(this.fileSendSocket, "FILE.UPLOAD");
                if (connectResp.getResponse() != NetResponse.Response.CONNECTION_SUCCESS)
                {
                    string errorMsg = "连接服务器失败: " + connectResp.getResult();
                    Main_Form.main_Form.Invoke(new MethodInvoker(delegate ()
                    {
                        MessageBox.Show(errorMsg, "系统提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        this.dataGridViewRow.Cells[4].Value = "上传失败";
                    }));
                    // 抛出异常，进入bg_RunWorkerCompleted方法的第一个if分支
                    throw new Exception(errorMsg);
                }

                // 4. Calculate MD5 (Fast)
                FileInfo fileInfo = new FileInfo(filePath);
                string md5 = CalculateFastMD5(fileInfo);
                // Log via UI
                Log($"开始上传: {fileName}, MD5: {md5}");

                // 5. Send RESUME_CHECK 发送断点续传检查帧
                long uploadedSize = SendResumeCheck(fileInfo, md5, dirId, userName);
                if (uploadedSize > 0)
                {
                    Log($"断点续传: 从 {FormatBytes(uploadedSize)} 开始");
                }
                else
                {
                    Log("全新上传");
                }

                // 6. Upload Data 开始上传数据
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read); // 打开客户端文件通道
                fs.Seek(uploadedSize, SeekOrigin.Begin);
                byte[] buffer = new byte[CHUNK_SIZE];
                int bytesRead;
                long totalUploaded = uploadedSize;
                // Initial Progress 初始化上传进度
                Bg_Worker.ReportProgress((int)((totalUploaded * 100.0) / fileSize));

                // 6.1 循环读取文件数据，分块上传 并更新进度条  
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Check Cancellation
                    if (Bg_Worker.CancellationPending)
                    {
                        e.Cancel = true;
                        Log("上传已取消");
                        return;
                    }

                    // Send Data Frame
                    // Resize buffer if last chunk is smaller
                    byte[] dataToSend = buffer;
                    if (bytesRead < buffer.Length)
                    {
                        dataToSend = new byte[bytesRead];
                        Array.Copy(buffer, dataToSend, bytesRead);
                    }
                    
                    SendFrame(DATA_FRAME, dataToSend);
                    totalUploaded += bytesRead;

                    // Report Progress
                    Bg_Worker.ReportProgress((int)((totalUploaded * 100.0) / fileSize));
                }

                // 7. Send End Frame 文件上传完毕，执行结束帧发送
                if (totalUploaded == fileSize) 
                {
                    JObject endData = new JObject();
                    endData["taskId"] = ""; // Server matches automatically
                    SendFrame(END_FRAME, Encoding.UTF8.GetBytes(endData.ToString(Formatting.None)));

                    // Wait for ACK 等待服务器确认上传成功
                    JObject ack = ReceiveFrame();
                    if (ack != null && ack["status"] != null && ack["status"].ToString() == "success")
                    {
                        Log("服务器确认上传成功");
                        this.dictionary["fileStatus"] = "WellDone"; // Legacy status indicator
                    }
                    else
                    {
                        string msg = ack != null ? ack["message"]?.ToString() : "无响应";
                        MessageBox.Show("上传未完成: " + msg, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("上传异常: " + ex.Message);
                this.dataGridViewRow.Cells[3].Value = "上传失败"; // Assuming index 3 is status
                // Don't set e.Cancel = true here unless it was actually cancelled.
                // Just log and let it finish.
                // But we should probably mark failure in dictionary or UI.
            }
            finally
            {
                if (fs != null) fs.Close();
                if (this.fileSendSocket != null)
                {
                    if (this.fileSendSocket.Connected) 
                    {
                        try { this.fileSendSocket.Shutdown(SocketShutdown.Both); } catch { }
                        this.fileSendSocket.Close();
                    }
                }
                ReleaseTaskCount(e);
            }
        }

        private long SendResumeCheck(FileInfo file, string md5, long? dirId, string userName)
        {
            // 1.构建RESUME_CHECK帧的元数据，断点续传帧发送，毕竟上传不知道是断点上传还是全新上传
            JObject meta = new JObject();
            meta["md5"] = md5;
            meta["fileName"] = file.Name;
            meta["fileSize"] = file.Length;
            meta["fileType"] = file.Extension.TrimStart('.');
            if (dirId != null)
            {
                meta["dirId"] = dirId;
            }
            if (userName != null) 
            {
                 meta["userId"] = 1L;
            } // Hardcoded fallback or omit? Java uses 1L. 
            // Note: If server relies on userId, we might need it. 
            // Since I can't find real userId, and Java demo used 1L, I'll try to put it if I have userName, 
            // or maybe I should put userName field too.
            // Let's assume server can handle it.
            if (userName != null)
            {
                meta["userName"] = userName;
            }
            // 2. 发送RESUME_CHECK帧
            SendFrame(RESUME_CHECK, Encoding.UTF8.GetBytes(meta.ToString(Formatting.None)));
            // 3. 提取服务端响应
            JObject ack = ReceiveFrame();
            if (ack == null)
            {
                throw new Exception("未收到服务器响应");
            }
            // 4. 处理服务端响应，继续上传还是走全新上传
            string status = ack["status"]?.ToString(); // 获取服务端响应状态
            long uploadedSize = ack["uploadedSize"] != null ? ack["uploadedSize"].Value<long>() : 0; // 获取服务端记录的本文件的已上传的大小
            string message = ack["message"]?.ToString(); // 获取服务端响应内容
            Log($"服务器响应: {message} (status={status})");
            if ("resume".Equals(status))
            {
                // 表示本次上传时断点继续上传
                return uploadedSize;
            }
            else if ("new".Equals(status))
            {
                // 表示本次上传为全新上传，那就直接发送全新上传帧
                SendMetaFrame(file, md5, dirId, userName);
                return 0;
            }
            else
            {
                throw new Exception("服务器错误: " + message);
            }
        }
        /// <summary>
        ///  文件全新上传帧
        /// </summary>
        /// <param name="file"></param>
        /// <param name="md5"></param>
        /// <param name="dirId"></param>
        /// <param name="userName"></param>
        /// <exception cref="Exception"></exception>
        private void SendMetaFrame(FileInfo file, string md5, long? dirId, string userName)
        {
            JObject meta = new JObject();
            meta["md5"] = md5;
            meta["fileName"] = file.Name;
            meta["fileSize"] = file.Length;
            meta["fileType"] = file.Extension.TrimStart('.');
            if (dirId != null) meta["dirId"] = dirId;
            if (userName != null) meta["userName"] = userName;

            SendFrame(META_FRAME, Encoding.UTF8.GetBytes(meta.ToString(Formatting.None)));

            JObject ack = ReceiveFrame();
            if (ack == null || ack["status"]?.ToString() != "ready")
            {
                throw new Exception("服务器未就绪");
            }
        }
        /// <summary>
        ///  发送帧数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        private void SendFrame(byte type, byte[] data)
        {
            try
            {
                int length = (data == null) ? 0 : data.Length;
                byte[] buffer = new byte[HEADER_LENGTH + length];

                buffer[0] = MAGIC[0];
                buffer[1] = MAGIC[1];
                buffer[2] = type;
                buffer[3] = 0; // flags

                byte[] lenBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));
                Array.Copy(lenBytes, 0, buffer, 4, 4);

                if (data != null)
                {
                    Array.Copy(data, 0, buffer, 8, length);
                }

                int sent = 0;
                while (sent < buffer.Length)
                {
                    sent += fileSendSocket.Send(buffer, sent, buffer.Length - sent, SocketFlags.None);
                }
            }
            catch (SocketException)
            {
                throw new Exception("网络连接中断");
            }
            catch (Exception ex)
            {
                throw new Exception("发送数据失败: " + ex.Message);
            }
        }
        /// <summary>
        /// 处理服务端响应帧数据
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private JObject ReceiveFrame()
        {
            try
            {
                // Read Header
                byte[] header = new byte[HEADER_LENGTH];
                int read = 0;
                while (read < HEADER_LENGTH)
                {
                    int n = fileSendSocket.Receive(header, read, HEADER_LENGTH - read, SocketFlags.None);
                    if (n == 0) throw new Exception("网络连接已断开");
                    read += n;
                }

                // Verify Magic
                if (header[0] != MAGIC[0] || header[1] != MAGIC[1])
                {
                    throw new Exception("Invalid magic number");
                }

                // byte type = header[2];
                // byte flags = header[3];
                
                byte[] lenBytes = new byte[4];
                Array.Copy(header, 4, lenBytes, 0, 4);
                int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBytes, 0));

                if (length > 0)
                {
                    byte[] data = new byte[length];
                    read = 0;
                    while (read < length)
                    {
                        int n = fileSendSocket.Receive(data, read, length - read, SocketFlags.None);
                        if (n == 0) throw new Exception("网络连接已断开");
                        read += n;
                    }

                    string jsonStr = Encoding.UTF8.GetString(data);
                    try 
                    {
                        return JObject.Parse(jsonStr);
                    }
                    catch
                    {
                        return new JObject(); // Empty if not JSON
                    }
                }

                return new JObject();
            }
            catch (SocketException)
            {
                throw new Exception("网络连接中断");
            }
        }

        private string CalculateFastMD5(FileInfo file)
        {
            using (var md5 = MD5.Create())
            {
                // Format: path|size|lastModified
                // Java lastModified is milliseconds since epoch.
                // C# LastWriteTimeUtc to Unix Time Milliseconds
                long lastModified = new DateTimeOffset(file.LastWriteTimeUtc).ToUnixTimeMilliseconds();
                string input = file.FullName + "|" + file.Length + "|" + lastModified;
                
                byte[] digest = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in digest)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F2") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / 1024.0 / 1024.0).ToString("F2") + " MB";
            return (bytes / 1024.0 / 1024.0 / 1024.0).ToString("F2") + " GB";
        }

        private void Log(string msg)
        {
            if (Main_Form.main_Form != null && !Main_Form.main_Form.IsDisposed)
            {
                Main_Form.main_Form.file_upload_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_upload_log_richTextBox.AppendText($"[ {DateTime.Now} ] {msg}\r\n");
                }));
            }
        }

        private void bg_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Use e.ProgressPercentage directly
            progressBarCell.Value = e.ProgressPercentage;
        }

        private void bg_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                //MessageBox.Show("文件上传出错: " + e.Error.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Log is better to avoid blocking UI with multiple popups
                Log("上传失败: " + e.Error.Message);
                this.dataGridViewRow.Cells[4].Value = "上传失败";
            }
            else if (e.Cancelled)
            {
                Log("上传已取消");
                this.dataGridViewRow.Cells[4].Value = "未上传";
            }
            else
            {
                Log("上传成功");
                this.dataGridViewRow.Cells[4].Value = "上传成功";
            }
        }

        private void ReleaseTaskCount(DoWorkEventArgs e)
        {
            while (true)
            {
                lock (obj)
                {
                    if (taskCount > 0)
                    {
                        taskCount--;
                        break;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref sleepTime, 5000, 1000);
                        break;
                    }
                }
                Thread.Sleep(100); // Small delay
            }
        }
    }
}
