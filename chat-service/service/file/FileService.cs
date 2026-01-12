
using chat_service.file;
using chat_service.frame;
using chat_service.net;
using chat_service.user;
using chat_service.util;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_service.service.file
{
    public class FileService
    {
        // 文件当前读到的大小
        private long currentReadSize = 0, fileSize = 0;
    
        // 文件上传每次读取或写入字节数大小
        public int fileReadSize = 0, fileWriteSize = 0;

        // 发起用户和接收用户
        private string launchUserName = "", receiveUserName = "";

        // progressbar后台工作者线程
        public BackgroundWorker  backGroundWorkerSendOnlineTransport = null,  backGroundWorkerReceiveOnlineTransport = null;

        // 任务状态
        public bool isSendBusy = false, isReceiveBusy = false, isConfirmReceive = false;

        // 待处理的在线传输文件响应
        public ConcurrentDictionary<string, List<CommonRes>> fileTaskDictionary = new ConcurrentDictionary<string, List<CommonRes>>();

        // 文件在线传输任务集
        private List<Dictionary<string, object>> fileSendOnlineTransportDicList;

        // 按行接收文件时对应的行对象
        private DataGridViewRow dataGridViewRow;

        public FileService(DataGridViewRow dataGridViewRow)
        {
            this.dataGridViewRow = dataGridViewRow;
        }

        public FileService(string launchUserName, string receiveUserName, List<Dictionary<string, object>> list, long fileSize, DataGridViewRow dataGridViewRow)
        {
            this.launchUserName = launchUserName;
            this.receiveUserName = receiveUserName;
            this.fileSendOnlineTransportDicList = list;
            this.fileSize = fileSize;
            this.dataGridViewRow = dataGridViewRow;

            // 初始化文件传输异步后台发送和接收对象
            // 初始化后台异步在线发送委托
            backGroundWorkerSendOnlineTransport = new BackgroundWorker();
            backGroundWorkerSendOnlineTransport.WorkerReportsProgress = true;  //允许报告进度
            backGroundWorkerSendOnlineTransport.DoWork += new DoWorkEventHandler(backgroundWorker_executeSendOnlineTransport_DoWork);
            backGroundWorkerSendOnlineTransport.ProgressChanged += new ProgressChangedEventHandler(Main_Form.main_Form.backGroundWorkerSendOnlineTransport_ProgressChanged);  //当调用ReportProgress会触发该事件
            backGroundWorkerSendOnlineTransport.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Main_Form.main_Form.backGroundWorkerSendOnlineTransport_WorkerCompleted); //成功后回调
        }



        /****************************************************** 处理文件在线传输开始 ********************************************************/

        // 发起端异步后台任务在线发送传输
        public void sendOnlineTransportHandler()
        {
            if (this.fileReadSize == 0)
            {
                this.fileReadSize = 3072;
            }

            // 发送文件内容信息用于服务端校验
            if (null != this.fileSendOnlineTransportDicList && this.fileSendOnlineTransportDicList.Count > 0)
            {
                //ProgressBar.getProgressBar().getBackGroundWorker().DoWork += new DoWorkEventHandler(backgroundWorker_executeUpload_DoWork);  //产生新的线程来处理任务
                // 追加异步任务
                //ProgressBarUtil.getProgressBar().getBackGroundWorker().DoWork += (o, ea) =>
                //{
                //    backgroundWorker_executeUpload_DoWork<T>(fileDicList, fileSize);
                //};

                // 异步启动传输文件任务
                this.backGroundWorkerSendOnlineTransport.RunWorkerAsync();
            }
            else
            {

            }   
        }

        //  发起端文件在线传输异步执行
        public void backgroundWorker_executeSendOnlineTransport_DoWork(object sender, DoWorkEventArgs e)
        {
            // 1、设置任务执行中
            this.isSendBusy = true;

            // 2、判断文件发送连接是否建立,没建立则建立，建立则直接复用
            Socket fileSendSocket = NetServiceContext.getSendFileSocket();
            NetResponse netResponse = NetServiceContext.initSendFileOnlineTransportSocketAndConnect(fileSendSocket,"FILE.UPLOAD");
            if (netResponse.getResponse() != NetResponse.Response.CONNECTION_SUCCESS)
            {
                Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] " + netResponse.getResult() + "\r\n");
                }));
                return;
            }

            // 3、封装在线传输任务
            string fileNames = "";
            long fileSumSize = 0L;
            Task<int>[] tasks = new Task<int>[this.fileSendOnlineTransportDicList.Count];
            for (int i = 0; i < this.fileSendOnlineTransportDicList.Count; i++)
            {
                Dictionary<string, object> dic = this.fileSendOnlineTransportDicList[i];
                fileNames += dic["fileName"].ToString() + ",";
                fileSumSize += (long)dic["fileSize"];
                dic.Add("socket", fileSendSocket);
                Task<int> task = new Task<int>(() => executeUpload(dic));
                tasks[i] = task;

                // 等待任务的执行结果
                //Task.WaitAll(tasks);
            }

            // 4、发送当前批次在线传输文件数(以一批作为确认接收)
            int fileFrameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.UPLOAD.ToString());
            int fileType = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString());
            int fileOperateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.TRANSPORT.ToString());
            Dictionary<string, object> sendDic = new Dictionary<string, object>();
            sendDic.Add("fileName", fileNames.Substring(0, fileNames.LastIndexOf(",")));
            sendDic.Add("fileCount", fileSendOnlineTransportDicList.Count);
            sendDic.Add("fileSize", fileSumSize);
            sendDic.Add("launchUserName", launchUserName);
            sendDic.Add("receiveUserName", receiveUserName);
            sendDic.Add("group", "NO_GROUP");
            NetServiceContext.sendFileOnlineTransportMessageNotWaiting(fileSendSocket, fileFrameType.ToString(), fileType.ToString(), fileOperateType.ToString(), JsonConvert.SerializeObject(sendDic), fileSumSize);

            // 5、同步等待文件是否上传的服务端确认消息
            bool result = waitingForFileIsNeedToOnlineTransport(fileSendSocket);
            if (result)
            {
                // 6、执行在线传输
                for (int i = 0; i < tasks.Length; i++)
                {
                    Task task = tasks[i];
                    task.Start();

                    // 设置文件上传进度条上的当前上传文件显示
                    Main_Form.main_Form.upload_waiting_label.Invoke(new MethodInvoker(delegate ()
                    { Main_Form.main_Form.upload_waiting_label.Text = "[ " + fileSendOnlineTransportDicList[i]["fileName"] + " ]"; }));

                    // 等待任务的完成
                    Task.WaitAll(task);
                    
                    // 设置当前文件处理成功
                    Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                    {
                        Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] [ " + fileSendOnlineTransportDicList[i]["fileName"] + " ] 传送成功\r\n");
                    }));
                }

                // 7、获取任务执行结果
                int fileUploadSuccess = 0;
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i].ContinueWith(t => (fileUploadSuccess += t.Result));
                }
            }
            else
            {
                // 终止在线传送文件，则
                Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 在线传送失败或对方拒绝在线接收\r\n");
                }));
            }

            // 取消任务执行中
            this.isSendBusy = false;
        }

        // 同步等待接收端用户是否确定需要在线传送文件
        private bool waitingForFileIsNeedToOnlineTransport(Socket fileSendSocket)
        {
            try
            {
                NetServiceContext.isSocketConnected(fileSendSocket);
                byte[] receiveBuffer = new byte[NetServiceContext.bufferSize];
                StringBuilder stringBuilder = new StringBuilder("");

                while (true)
                {
                    try
                    {
                        //开始接收信息
                        int readByteLength = fileSendSocket.Receive(receiveBuffer);
                        if (readByteLength == 0)
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                        if (readByteLength == -1)
                        {
                            fileSendSocket.Close();
                            break;
                        }

                        // 解析数据(第一个字节为响应帧类型)
                        byte frameType = receiveBuffer[0]; // 获取帧类型
                        stringBuilder.Append(Encoding.UTF8.GetString(receiveBuffer, 1, readByteLength - 1)); // 获取帧数据
                        NetResponse netResponse = receiveHandler(stringBuilder.ToString());
                        stringBuilder.Clear();
                        Array.Clear(receiveBuffer, 0, receiveBuffer.Length);

                        // 处理数据
                        if (netResponse.getResult() == "") // 没有任何数据
                        {
                            return false;
                        }
                        else
                        {
                            if (frameType == (byte)2) // 10：处理文件在线传输帧
                            {
                                if (((CommonRes)netResponse.getCommonRes()).getOperate() == "CONFIRM.ONLINE.TRANSPORT")
                                {
                                    return true;
                                }

                                return false;
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        // 判断socket连接状态，如果是非连接，则直接终止接收线程
                        try
                        {
                            NetServiceContext.isSocketConnected(fileSendSocket);
                            // 处于连接
                        }
                        catch (SocketException ex)
                        {
                            // 代码 10035也保证socket连接状态正常
                            if (ex.NativeErrorCode.Equals(10035))
                            {
                                continue;
                            }

                            // 否则直接结束当前socket接收数据线程
                            return false;
                        }

                    }
                    catch (Exception e)
                    {
                        // 设置主窗体连接状态
                        if (null != Main_Form.main_Form)
                        {
                            Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                            {
                                Main_Form.main_Form.result_label.Text = "连接中......";
                            }));
                        }

                        return false;
                    }
                }
            }
            catch (SocketException e)
            {
                Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.result_label.Text = "网络连接异常......";
                }));
                return false;
            }

            return false;
        }

        // 线程上传
        public int executeUpload(Dictionary<string, object> dictionary)
        {
            // 获取文件基本数据
            FileStream fileStream = (FileStream)dictionary["fileStream"];
            Socket fileSendSocket = (Socket)dictionary["socket"];

            if (executeSendOnlineTransport(fileSendSocket, fileStream, dictionary))
            {
                // 不论是异常还是成功，最后都关闭文件流通道和文件在线传输通道
                fileStream.Close();
                //NetServiceContext.fileSendSocket.Shutdown(SocketShutdown.Send);
                fileSendSocket.Close();
                fileSendSocket = null;
                Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 成功关闭文件发送\r\n");
                }));
                return 1;
            }
            else
            {
                // 不论是异常还是成功，最后都关闭文件流通道和文件在线传输通道
                fileStream.Close();
                //NetServiceContext.fileSendSocket.Shutdown(SocketShutdown.Send);
                fileSendSocket.Close();
                fileSendSocket = null;
                return 0;
            }
        }

        // 执行在线文件流实时传输
        private bool executeSendOnlineTransport(Socket fileSendSocket, FileStream fileStream, Dictionary<string, object> dictionary)
        {
            // 1、获取基本数据
            string launchUserName = dictionary["launchUserName"].ToString();
            string receiveUserName = dictionary["receiveUserName"].ToString();
            long fileSize = (long)dictionary["fileSize"];
            string fileName = dictionary["fileName"].ToString();
            
            // 2、用于文件流读取数据缓存字节数组， 默认2048个字节
            byte[] fileStreamBytes = new byte[this.fileReadSize];
            int packetIndex = 1;
            long currentReadSize = 0;
            byte fileFrameType = Convert.ToByte(FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.DATA_TRANSPORT.ToString()).ToString(), 2);
            byte fileType = Convert.ToByte(FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString()).ToString(), 2);
            byte fileOperateType = Convert.ToByte(FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.TRANSPORT.ToString()).ToString(), 2);
            byte[] sendDataBytes = Encoding.UTF8.GetBytes(receiveUserName);
            byte[] sendDataLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sendDataBytes.Length));

            // 3、判断文件传送连接是否正常
            try
            {
                NetServiceContext.isSocketConnected(fileSendSocket);
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (!e.NativeErrorCode.Equals(10035))
                {
                    return false;
                }
            }

            // 4、文件字节数据开始读取的位置:  
            // 2B -> 当前帧的总长度；
            // 1B -> 文件是否读取完[0:未读取完, 1:读取完], 
            // 4B -> 包序号 
            // 3B -> 帧类型，文件类型，文件操作类型；
            // ..... -> 剩余未文件流真实数据长度
            int orginStreamDataIndex = 2 + 1 + 4 + 3 + sendDataLengthBytes.Length + sendDataBytes.Length;

            // 5、开始发送文件内容
            while (true)
            {
                try
                {
                    // readCount 这个是保存真正从文件中读取到的字节数，即readCount并不一定等于文件缓冲流字节数组大小
                    int readCount = fileStream.Read(fileStreamBytes, 0, fileStreamBytes.Length);
                    if (readCount > 0)
                    {

                        byte[] byteArrayRead = new byte[orginStreamDataIndex + readCount];

                        // 原子设置进度条当前数值,该进度条表示发送出去的进度
                        Interlocked.Add(ref currentReadSize, readCount);

                        // 记录当前发送帧总长度
                        byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) (byteArrayRead.Length)));

                        // 拷贝标记数据(当前帧总长度,2B + 1B + 4B + 3B + ...)
                        byteArrayRead[0] = frameSumLengthBytes[0]; // 当前帧总长度字节数组第一个字节
                        byteArrayRead[1] = frameSumLengthBytes[1]; // 当前帧总长度字节数组第二个字节
                        byteArrayRead[2] = Interlocked.Read(ref currentReadSize).Equals(fileSize) ? (byte)1 : (byte)0; //是否是结束帧

                        byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetIndex)); packetIndex++; // 当前包序号,int表示占据4个字节
                        frameIndexBytes.CopyTo(byteArrayRead, 3);

                        byteArrayRead[7] = fileFrameType; // 帧类型
                        byteArrayRead[8] = fileType; // 文件类型
                        byteArrayRead[9] = fileOperateType; // 文件操作类型
                        
                        sendDataLengthBytes.CopyTo(byteArrayRead, 10);
                        sendDataBytes.CopyTo(byteArrayRead, sendDataLengthBytes.Length + 10);
                        Buffer.BlockCopy(fileStreamBytes, 0, byteArrayRead, orginStreamDataIndex, readCount);

                        // socket远程发送, 从文件中读取到多少个字节，则发送多少个字节
                        int sendBytes = fileSendSocket.Send(byteArrayRead, byteArrayRead.Length, 0);

                        Thread.Sleep(5);

                        // 更新进度条
                        this.backGroundWorkerSendOnlineTransport.ReportProgress(Convert.ToInt32(Interlocked.Read(ref currentReadSize)));

                        // 清空字节数组
                        Array.Clear(fileStreamBytes, 0, fileStreamBytes.Length);
                    }
                    else if (readCount == 0)
                    {
                        // 文件发送完成: 如果从文件中读取的字节数与发送字节数相同，则发送在线文件发送完成的文件流结束通知,之后进行阻塞，等待服务端处理完成后关闭发送端文件以及socket通道
                        if (Interlocked.Read(ref currentReadSize).Equals(fileSize))
                        {
                            // 发送文件传送成功消息
                            string frameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.DATA_TRANSPORT_END.ToString()).ToString();
                            string type = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString()).ToString();
                            string operateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.TRANSPORT.ToString()).ToString();

                            Dictionary<string, string> dic = new Dictionary<string, string>();
                            dic.Add("CLOSE.USER", launchUserName);
                            dic.Add("FILE.STREAM.SEND.END", "FILE.STREAM.SEND.END");
                            byte[] sendDataBytes1 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic));
                            byte[] sendDataLengthBytes1 = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sendDataBytes1.Length));

                            byte[] sendBytes = new byte[2 + 1 + 4 + 3 + sendDataBytes1.Length + sendDataLengthBytes1.Length];
                            byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)(sendBytes.Length)));
                            sendBytes[0] = frameSumLengthBytes[0]; // 当前帧总长度字节数组第一个字节
                            sendBytes[1] = frameSumLengthBytes[1]; // 当前帧总长度字节数组第二个字节
                            sendBytes[2] = (byte)1; // 结束帧

                            byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetIndex)); packetIndex++; // 当前包序号,int表示占据4个字节
                            frameIndexBytes.CopyTo(sendBytes, 3);

                            sendBytes[7] = Convert.ToByte(frameType, 2);
                            sendBytes[8] = Convert.ToByte(type, 2);
                            sendBytes[9] = Convert.ToByte(operateType, 2);
                            sendDataLengthBytes1.CopyTo(sendBytes, 10);
                            sendDataBytes1.CopyTo(sendBytes, sendDataLengthBytes1.Length + 10);
                            fileSendSocket.Send(sendBytes, sendBytes.Length, 0);

                            // 此处阻塞等待服务器发送关闭文件通道的通知
                            byte[] receiveBuffer = new byte[1024];
                            int readByteLength = fileSendSocket.Receive(receiveBuffer);
                            if (readByteLength > 0)
                            {
                                // 解析数据(第一个字节为响应帧类型)
                                byte frameType1 = receiveBuffer[0]; // 获取帧类型
                                StringBuilder stringBuilder = new StringBuilder();
                                stringBuilder.Append(Encoding.UTF8.GetString(receiveBuffer, 1, readByteLength - 1)); // 获取帧数据
                                NetResponse netResponse = receiveHandler(stringBuilder.ToString());

                                CommonRes commonRes = (CommonRes)netResponse.getCommonRes();
                                if (commonRes.getStatus() == "FILE.STREAM.SEND.END")
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }
        }

        // 文件操作数据解析
        private static NetResponse receiveHandler(string receiveData)
        {
            // 转换数据并追加；
            CommonRes commonRes = (CommonRes)JsonConvert.DeserializeObject(receiveData, typeof(CommonRes));
            return NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), "");
        }

        /****************************************************** 处理文件在线传输结束 ********************************************************/








        /****************************************************** 处理文件在线接收开始 ********************************************************/

        // 判断当前客户端作为文件在线传输接收端时，判断以聊天服务的文本帧传输过来的数据是否是需要执行文件在线传输
        public static void isNeedToFileOnlineTransport(NetResponse netResponse)
        {
            CommonRes commonRes = (CommonRes)netResponse.getCommonRes();
            if (null != commonRes.getOperate() && commonRes.getOperate() == "ONLINE.TRANSPORT.NEED.CONFIRM")
            {
                // 1、将在线传输任务添加到任务列表中
                //addFileTaskToDictionary(commonRes);

                // 2、任务回显至待处理任务表格中
                setFileTransportTaskToDataGridView(commonRes);
            }
        }

        // 任务回显至待处理任务表格中
        private static void setFileTransportTaskToDataGridView(CommonRes commonRes)
        {
            Main_Form.main_Form.user_list_dataGridView.Invoke(new MethodInvoker(delegate ()
            {
                // 返回行数，如果为0标识没有数据
                int rowCount = Main_Form.main_Form.task_list_dataGridView.RowCount;


                Main_Form.main_Form.task_list_dataGridView.Rows.Add();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[0].Value = rowCount + 1;
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[1].Value = commonRes.getMessage();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[2].Value = commonRes.getLaunchUserName();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[3].Value = Utils.ToDateTime(commonRes.getTime());
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[4].Value = (commonRes.getStatus() == "1" ? "未下载" : (commonRes.getStatus() == "2" ? "处理中" : "处理完成"));
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[9].Value = Utils.ToDateTime(commonRes.getTime()).ToLongDateString();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[10].Value = commonRes.getFileName();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[11].Value = commonRes.getFileSize();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[12].Value = commonRes.getLaunchUserName();
                Main_Form.main_Form.task_list_dataGridView.Rows[rowCount].Cells[13].Value = commonRes.getReceiveUserName();
            }));
        }

        // 开始执行接收文件任务
        public void receiveOnlineTransportHandler()
        {
            // 初始化后台异步在线接收委托
            backGroundWorkerReceiveOnlineTransport = new BackgroundWorker();
            backGroundWorkerReceiveOnlineTransport.WorkerReportsProgress = true;  //允许报告进度
            backGroundWorkerReceiveOnlineTransport.DoWork += new DoWorkEventHandler(backgroundWorker_executeReceiveOnlineTransport_DoWork);
            backGroundWorkerReceiveOnlineTransport.ProgressChanged += new ProgressChangedEventHandler(Main_Form.main_Form.backGroundWorkerReceiveOnlineTransport_ProgressChanged);  //当调用ReportProgress会触发该事件
            backGroundWorkerReceiveOnlineTransport.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Main_Form.main_Form.backGroundWorkerReceiveOnlineTransport_WorkerCompleted); //成功后回调

            // 异步启动传输文件任务
            this.backGroundWorkerReceiveOnlineTransport.RunWorkerAsync();
        }

        // 异步开始接收待处理文件 --> 发送在线传输帧类型 --> 010
        public void backgroundWorker_executeReceiveOnlineTransport_DoWork(object sender, DoWorkEventArgs e)
        {
            // 1、判断文件接收连接是否建立,没建立则建立，建立则直接复用
            Socket fileReceiveSocket = NetServiceContext.getReceiveFileSocket();
            NetResponse netResponse = NetServiceContext.initReceiveFileOnlineTransportSocketAndConnect(fileReceiveSocket);
            if (netResponse.getResponse() != NetResponse.Response.CONNECTION_SUCCESS)
            {
                Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] " + netResponse.getResult() + "\r\n");
                }));
                return;
            }

            Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
            {
                Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件接收连接建立成功\r\n");
            }));

            // 2、创建本地文件用于接收在线传输的文件
            string filePath = "D:\\chatService\\download\\" + this.dataGridViewRow.Cells[10].Value.ToString(); //Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + "\\" +currentWaitReceiveRes.getFileName();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (fileWriteSize == 0)
            {
                fileWriteSize = 3072;
            }
            FileStream fileStream = new FileStream(filePath, FileMode.Create);

            // 3、发送当前待接收文件确认数据
            int fileFrameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.ONLINE_TRANSPORT.ToString());
            int fileType = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString());
            int fileOperateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.TRANSPORT.ToString());
            Dictionary<string, object> sendDic = new Dictionary<string, object>();
            sendDic.Add("fileName", this.dataGridViewRow.Cells[10].Value.ToString());
            sendDic.Add("fileCount", 1);
            sendDic.Add("fileSize", this.dataGridViewRow.Cells[11].Value.ToString());
            sendDic.Add("launchUserName", this.dataGridViewRow.Cells[12].Value.ToString());
            sendDic.Add("receiveUserName", this.dataGridViewRow.Cells[13].Value.ToString());
            sendDic.Add("group", "NO_GROUP");
            sendDic.Add("isConfirmReceive", this.dataGridViewRow.Cells[9].Value.ToString());
            NetServiceContext.receiveFileOnlineTransportMessageNotWaiting(fileReceiveSocket, fileFrameType.ToString(), fileType.ToString(), fileOperateType.ToString(), JsonConvert.SerializeObject(sendDic), long.Parse(this.dataGridViewRow.Cells[11].Value.ToString()));

            // 4、开始阻塞等待接收文件流数据，直到文件数据接收成功后
            if (executeReceiveOnlineTransport(fileReceiveSocket, fileStream))
            {
                // 不论是异常还是成功，最后都关闭文件流通道和文件在线传输通道
                fileStream.Close();
                //NetServiceContext.fileSendSocket.Shutdown(SocketShutdown.Send);
                fileReceiveSocket.Close();
                fileReceiveSocket = null;

                // 追加成功接日志
                Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件名: [ " + this.dataGridViewRow.Cells[10].Value.ToString() + " ], 文件存储位置为: [ " + filePath + " ] \r\n");
                }));

                // 删除成功接收的文件记录
                Main_Form.main_Form.task_list_dataGridView.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.task_list_dataGridView.Rows.Remove(this.dataGridViewRow);
                }));
            }
            else
            {
                // 不论是异常还是成功，最后都关闭文件流通道和文件在线传输通道
                fileStream.Close();
                //NetServiceContext.fileSendSocket.Shutdown(SocketShutdown.Send);
                fileReceiveSocket.Close();
                fileReceiveSocket = null;
            }
        }

        // 执行在线文件流实时接收
        private bool executeReceiveOnlineTransport(Socket fileReceiveSocket, FileStream fileStream)
        {
            byte[] byteArrayRead = new byte[fileWriteSize];

            try
            {
                NetServiceContext.isSocketConnected(fileReceiveSocket);
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (!e.NativeErrorCode.Equals(10035))
                {
                    return false;
                }
            }

            long currentWriteSize = 0;
            // 开始发送文件内容
            while (true)
            {
                try
                {
                    //接收文件流数据
                    int readCount = fileReceiveSocket.Receive(byteArrayRead, byteArrayRead.Length, 0);
                    if (readCount > 0)
                    {
                        // 写入文件
                        fileStream.Write(byteArrayRead, 0, readCount);
                        fileStream.Flush();

                        // 原子设置进度条当前数值
                        Interlocked.Add(ref currentWriteSize, readCount);

                        if (Interlocked.Read(ref currentWriteSize) <= Convert.ToInt32(this.dataGridViewRow.Cells[11].Value.ToString()))
                        {
                            // 更新进度条
                            this.backGroundWorkerReceiveOnlineTransport.ReportProgress(Convert.ToInt32(Interlocked.Read(ref currentWriteSize)));
                        }

                        
                        //Thread.Sleep(1000);
                        Array.Clear(byteArrayRead, 0, byteArrayRead.Length);

                        if (Interlocked.Read(ref currentWriteSize) >= Convert.ToInt32(this.dataGridViewRow.Cells[11].Value.ToString()))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }
        }

        /****************************************************** 处理文件在线接收接收 ********************************************************/








        /****************************************************** 处理文件在线取消开始 ********************************************************/

        // 文件在线接收取消


        /****************************************************** 处理文件在线取消开始 ********************************************************/


















        /****************************************************** 个人网盘文件夹上传处理开始 ********************************************************/

        // 取消个人网盘文件上传，发送出去即可，不用关系服务器返回
        public void cancelFileUploadTransport(Dictionary<string, object> dictionary, Socket fileSendSocket)
        {
            int fileFrameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.UPLOAD.ToString());
            int fileType = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString());
            int fileOperateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.STORE.ToString());
            Dictionary<string, object> sendDic = new Dictionary<string, object>();
            sendDic.Add("tag", dictionary["tag"]);
            sendDic.Add("launchUserName", dictionary["launchUserName"]);
            sendDic.Add("operate", "CLOSE.FILE.CHANNEL;UPLOAD");
            NetServiceContext.sendFileOnlineTransportMessageNotWaiting(fileSendSocket, fileFrameType.ToString(), fileType.ToString(), fileOperateType.ToString(), JsonConvert.SerializeObject(sendDic), long.Parse(dictionary["fileSize"].ToString()));
        }

        // 取消个人网盘文件下载，此处需要等待服务器返回文件取消下载成功消息，如果不等待客户端关闭文件下载socket通道后，由于服务端下载线程并未来得及被中断，还处于write
        // 传输导致报错，所以此处得等服务端结束掉下载线程方可返回
        public void cancelFileDonwloadTransport(Dictionary<string, object> dictionary, Socket fileDownloadSocket)
        {
            int fileFrameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.DOWNLOAD.ToString());
            int fileType = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString());
            int fileOperateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.STORE.ToString());
            Dictionary<string, object> sendDic = new Dictionary<string, object>();
            sendDic.Add("tag", dictionary["tag"]);
            sendDic.Add("launchUserName", dictionary["launchUserName"]);
            sendDic.Add("operate", "CLOSE.FILE.CHANNEL;DOWNLOAD");
            NetServiceContext.sendFileOnlineTransportMessageNotWaiting(fileDownloadSocket, fileFrameType.ToString(), fileType.ToString(), fileOperateType.ToString(), JsonConvert.SerializeObject(sendDic), long.Parse(dictionary["fileSize"].ToString()));
        }

        /****************************************************** 个人网盘文件夹上传处理结束 ********************************************************/

    }
}
