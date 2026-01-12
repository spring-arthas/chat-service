using chat_service.frame;
using chat_service.net;
using chat_service.service.file;
using chat_service.util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace chat_service.file
{
    public class AsyncPersonalFileUploadHelper
    {
        // 当前处理中的FrameModel
        private FrameModel currentFrameModel = new FrameModel("1", 0);

        // 文件上传每次读取或写入字节数大小
        public int fileReadSize = 0, loopCount = 0;

        public static int taskCount = 0;

        private static long sleepTime = 1000;

        public static object obj = new object();

        /// <summary>
        /// BackgroundWorker组件
        /// </summary>
        public BackgroundWorker Bg_Worker { get; set; }

        /// <summary>
        /// 文件上传Socket
        /// </summary>
        private Socket fileSendSocket;

        /// <summary>
        /// 表格内的进度条
        /// </summary>
        private DataGridViewProgressBarCell progressBarCell;

        /// <summary>
        /// 当前行
        /// </summary>
        private DataGridViewRow dataGridViewRow;

        /// <summary>
        /// 上传文件数据
        /// </summary>
        private Dictionary<string, object> dictionary;

        /// <summary>
        /// 任务取消状态
        /// </summary>
        public DoWorkEventArgs doWorkEventArgs;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="number"></param>
        public AsyncPersonalFileUploadHelper(DataGridViewProgressBarCell progressBarCell, DataGridViewRow dataGridViewRow, Dictionary<string, object> dictionary)
        {
            this.dataGridViewRow = dataGridViewRow;
            this.dictionary = dictionary;
            this.progressBarCell = progressBarCell;

            if (this.fileReadSize == 0)
            {
                this.fileReadSize = NetServiceContext.fileOperateSize;
            }

            this.progressBarCell.Maximum = 100;
            this.progressBarCell.Mimimum = 0;

            long fileSize = long.Parse(this.dictionary["fileSize"].ToString());
            this.loopCount = fileSize < this.fileReadSize ? 1 : (int)(fileSize / this.fileReadSize);

            // 创建组件
            this.Bg_Worker = new BackgroundWorker();
            this.Bg_Worker.WorkerReportsProgress = true;
            this.Bg_Worker.WorkerSupportsCancellation = true;

            // 绑定事件
            this.Bg_Worker.DoWork += backgroundWorker_executePersonalUploadTransport_DoWork;
            this.Bg_Worker.ProgressChanged += bg_ProgressChanged;
            this.Bg_Worker.RunWorkerCompleted += bg_RunWorkerCompleted;
        }

        /// <summary>
        /// DoWork事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_executePersonalUploadTransport_DoWork(object sender, DoWorkEventArgs e)
        {
            // 文件传输过程中存在强制退出主解面，此处需要根据DoWorkEventArgs判断来终止任务执行
            doWorkEventArgs = e;

            // 当前上传线程获取锁，获取成功执行文件上传，上传任务数+1，保证上传任务使用保持在规定个数，避免文件伤上传数量过大
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

                    // 判断任务数是否达到5个，未达到，则开辟任务执行
                    if (taskCount < 2)
                    {
                        taskCount = taskCount + 1;
                        break;
                    }
                    else
                    {
                        // 任务数刚好为5个，则调整休眠时间，尽量多睡会
                        Interlocked.CompareExchange(ref sleepTime, 1000, 5000);
                    }
                }

                Thread.Sleep((int) Interlocked.Read(ref sleepTime));
            }

            // 1、判断文件发送连接是否建立,没建立则建立，建立则直接复用
            this.fileSendSocket = NetServiceContext.getSendFileSocket();
            NetResponse netResponse = NetServiceContext.initSendFileOnlineTransportSocketAndConnect(this.fileSendSocket, "FILE.UPLOAD");
            if (netResponse.getResponse() != NetResponse.Response.CONNECTION_SUCCESS)
            {
                Main_Form.main_Form.Invoke(new MethodInvoker(delegate ()
                {
                    MessageBox.Show("文件上传连接服务器失败！失败文件 [" + this.dictionary["fileName"] + "], 连接失败原因 [ " + netResponse.getResult() + " ]", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }));

                this.dataGridViewRow.Cells[3].Value = "上传失败";
                this.fileSendSocket.Close();

                releaseTaskCount(e);
                return;
            }

            // 2、发送当前批次在线传输文件数(以一批作为确认接收)
            int fileFrameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.UPLOAD.ToString());
            int fileType = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString());
            int fileOperateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.STORE.ToString());
            Dictionary<string, object> sendDic = new Dictionary<string, object>();
            sendDic.Add("fileName", this.dictionary["fileName"]);
            sendDic.Add("fileCount", 1);
            sendDic.Add("fileSize", this.dictionary["fileSize"]);
            sendDic.Add("launchUserName", this.dictionary["launchUserName"]);
            sendDic.Add("group", this.dictionary["uploadFolderPath"]);
            sendDic.Add("tag", this.dictionary["tag"].ToString());//文件标识
            NetServiceContext.sendFileOnlineTransportMessageNotWaiting(this.fileSendSocket, fileFrameType.ToString(), fileType.ToString(), fileOperateType.ToString(), JsonConvert.SerializeObject(sendDic), long.Parse(this.dictionary["fileSize"].ToString()));

            // 4、同步等待文件是否上传的服务端确认消息
            bool result = waitingForFileIsNeedToOnlineTransport(e);
            if (result)
            {
                // 5、执行在线传输
                this.executeUpload(e);
                releaseTaskCount(e);
            }
            else
            {
                // 终止在线传送文件，可能为文件名称重复或是其他原因
                Main_Form.main_Form.file_upload_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件上传失败\r\n");
                }));

                this.dataGridViewRow.Cells[3].Value = "上传失败";
                if (NetServiceContext.isSocketConnected(this.fileSendSocket)) 
                {
                    this.fileSendSocket.Shutdown(SocketShutdown.Both);
                    this.fileSendSocket.Close();
                }

                // 释放资源
                releaseTaskCount(e);
            }
        }

        // 同步等待接收端用户是否确定需要在线传送文件
        private bool waitingForFileIsNeedToOnlineTransport(DoWorkEventArgs ex)
        {
            try
            {
                NetServiceContext.isSocketConnected(this.fileSendSocket);
                byte[] receiveBuffer = new byte[NetServiceContext.bufferSize];
                StringBuilder stringBuilder = new StringBuilder("");

                while (true)
                {
                    //开始接收信息
                    int readByteLength = this.fileSendSocket.Receive(receiveBuffer);
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
                    // 不足基本帧数据，则不处理只缓存
                    if (readByteLength < 10) // 长度不为8，即连基本帧信息数据都不够，则不进行解析
                    {
                        continue;
                    }


                    if (currentFrameModel.getStatus() == "1") // 从未处理，执行首次处理
                    {
                        parseBytes(readByteLength, receiveBuffer, null);
                        // 如果刚好字节读取完成则直接处理
                        if (currentFrameModel.getStatus() == "3")
                        {
                            bool result = receiveHandler(currentFrameModel, null, ex);
                            if (result)
                            {
                                this.resetFrameModelAll();
                                return result;
                            }
                            else
                            {
                                //  未处理成功,清空当前，且存在剩余
                                if (currentFrameModel.getRestBytes() != null && currentFrameModel.getRestBytes().Length > 0)
                                {
                                    this.resetFrameModelBasic();
                                    currentFrameModel.setStatus("4"); // 设置为剩余处理状态，下次继续处理
                                    continue;
                                }
                                else
                                {
                                    this.resetFrameModelAll();
                                    return false;
                                }
                            }
                        }
                    }
                    else if (currentFrameModel.getStatus() == "2") // 处理中
                    {
                        int index = currentFrameModel.getIndex();
                        // 获取剩余需要从receiveBuffer中读取的字节数
                        int appendBytesCount = currentFrameModel.getOriginDataBytesLength() - currentFrameModel.getOrigiDataBytes().Length;
                        // 如果当前接收的字节数据长度大于完整帧剩余需要的字节数，则重新构建原始字节数组,并缓存剩余字节数组
                        if (readByteLength >= appendBytesCount)
                        {
                            // 1、读取并设置剩余字节数组
                            byte[] appendBytes = new byte[appendBytesCount];
                            Buffer.BlockCopy(receiveBuffer, 0, appendBytes, 0, appendBytesCount);

                            // 2、将剩余字节数组以及原有的字节数组拼接变为新数组
                            byte[] perfectOriginBytes = new byte[appendBytes.Length + currentFrameModel.getOrigiDataBytes().Length];
                            Buffer.BlockCopy(currentFrameModel.getOrigiDataBytes(), 0, perfectOriginBytes, 0, currentFrameModel.getOrigiDataBytes().Length);
                            Buffer.BlockCopy(appendBytes, 0, perfectOriginBytes, currentFrameModel.getOrigiDataBytes().Length, appendBytes.Length);
                            currentFrameModel.setOrigiDataBytes(perfectOriginBytes);
                            currentFrameModel.setStatus("3");

                            // 3、执行业务处理
                            bool result = receiveHandler(currentFrameModel, null, ex);
                            if (result)
                            {
                                this.resetFrameModelAll();
                                return result;
                            }
                            else
                            {
                                //  未处理成功,清空当前，且存在剩余
                                if (currentFrameModel.getRestBytes().Length > 0)
                                {
                                    this.resetFrameModelBasic();
                                    currentFrameModel.setStatus("4"); // 设置为剩余处理状态，下次继续处理
                                }
                            }
                        }
                        else
                        {
                            // 1、不够则继续缓存，重新构建缓存字节数组,读取并设置剩余字节数组
                            byte[] appendBytes = new byte[receiveBuffer.Length];
                            Buffer.BlockCopy(receiveBuffer, 0, appendBytes, 0, receiveBuffer.Length);

                            // 2、将追加的字节数组以及原有的字节数组拼接变为新数组
                            byte[] perfectOriginBytes = new byte[appendBytes.Length + currentFrameModel.getOrigiDataBytes().Length];
                            Buffer.BlockCopy(currentFrameModel.getOrigiDataBytes(), 0, perfectOriginBytes, 0, currentFrameModel.getOrigiDataBytes().Length);
                            Buffer.BlockCopy(appendBytes, 0, perfectOriginBytes, currentFrameModel.getOrigiDataBytes().Length, appendBytes.Length);
                            currentFrameModel.setOrigiDataBytes(perfectOriginBytes);
                            currentFrameModel.setStatus("2");
                        }

                    }
                    else if (currentFrameModel.getStatus() == "4") // 存在剩余，此时status必须为4，且restBytes不为空，其他都为初始值
                    {
                        // 将当前缓存数组restBytes和新接收的数组receiveBuffer数组合并
                        byte[] sumBytes = new byte[currentFrameModel.getRestBytes().Length + receiveBuffer.Length];
                        // 拷贝缓存字节数组
                        Buffer.BlockCopy(currentFrameModel.getRestBytes(), 0, sumBytes, 0, currentFrameModel.getRestBytes().Length);
                        // 拷贝新接收的字节数组
                        Buffer.BlockCopy(receiveBuffer, 0, sumBytes, currentFrameModel.getRestBytes().Length, receiveBuffer.Length);
                        parseBytes(readByteLength, sumBytes, null);
                    }
                }
            }
            catch (Exception e)
            {
                // 任务执行异常
                lock (obj)
                {
                    Main_Form.main_Form.file_upload_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                    {
                        Main_Form.main_Form.file_upload_log_richTextBox.Text = "文件 [ " + this.dictionary["fileName"].ToString() + " ] 上传校验异常, error [ " + e.Message.ToString() + " ]";
                    }));
                }
                return false;
            }
            return false;
        }

        // 解析数据
        private void parseBytes(int readByteLength, byte[] receiveBuffer, object obj)
        {
            // 解析总长度4B
            if (currentFrameModel.getSumLength() == 0)
            {
                int index = currentFrameModel.getIndex();
                int value = (int)((receiveBuffer[index] & 0xFF)
                    | ((receiveBuffer[index + 1] & 0xFF) << 8)
                    | ((receiveBuffer[index + 2] & 0xFF) << 16)
                    | ((receiveBuffer[index + 3] & 0xFF) << 24));

                currentFrameModel.setSumLength(IPAddress.NetworkToHostOrder(value));
                currentFrameModel.setIndex(index + 4);
            }

            // 解析帧类型 1B
            if (currentFrameModel.getFrameType() == (byte)0)
            {
                currentFrameModel.setFrameType(receiveBuffer[currentFrameModel.getIndex()]);
                currentFrameModel.setIndex(currentFrameModel.getIndex() + 1);
            }

            // 解析是否为结尾帧 1B
            if (currentFrameModel.getEndFrame() == (byte)0)
            {
                currentFrameModel.setEndFrame(receiveBuffer[currentFrameModel.getIndex()]);
                currentFrameModel.setIndex(currentFrameModel.getIndex() + 1);
            }

            // 解析数据长度 4B
            if (currentFrameModel.getOriginDataBytesLength() == (short)0)
            {
                int index = currentFrameModel.getIndex();
                int value = (int)((receiveBuffer[index] & 0xFF)
                    | ((receiveBuffer[index + 1] & 0xFF) << 8)
                    | ((receiveBuffer[index + 2] & 0xFF) << 16)
                    | ((receiveBuffer[index + 3] & 0xFF) << 24));
                currentFrameModel.setOriginDataBytesLength(IPAddress.NetworkToHostOrder(value));
                currentFrameModel.setIndex(index + 4);
            }

            // 解析数据
            if (currentFrameModel.getOrigiDataBytes() == null)
            {
                int index = currentFrameModel.getIndex();

                // 如果真实字节数据长度刚好全部存储与receiveBuffer中，即解析完成，则设置其状态
                if ((readByteLength - 10) == currentFrameModel.getOriginDataBytesLength())
                {
                    byte[] dataBytes = new byte[currentFrameModel.getOriginDataBytesLength()];
                    Buffer.BlockCopy(receiveBuffer, index, dataBytes, 0, currentFrameModel.getOriginDataBytesLength());
                    currentFrameModel.setOrigiDataBytes(dataBytes);

                    currentFrameModel.setIndex(0);
                    currentFrameModel.setStatus("3"); // 处理完成
                }
                else if ((readByteLength - 10) > currentFrameModel.getOriginDataBytesLength()) // 大于真实字节数据长度，则发生了半包,存在剩余字节
                {
                    // 处理正常数据
                    byte[] dataBytes = new byte[currentFrameModel.getOriginDataBytesLength()];
                    Buffer.BlockCopy(receiveBuffer, index, dataBytes, 0, currentFrameModel.getOriginDataBytesLength());
                    currentFrameModel.setOrigiDataBytes(dataBytes);
                    currentFrameModel.setStatus("3");

                    // 处理剩余部分字节
                    byte[] restBytes = new byte[receiveBuffer.Length - (10 + currentFrameModel.getOrigiDataBytes().Length)];
                    Buffer.BlockCopy(receiveBuffer, (10 + currentFrameModel.getOrigiDataBytes().Length), restBytes, 0, restBytes.Length);
                    currentFrameModel.setIndex(0);
                    currentFrameModel.setRestBytes(restBytes); // 记录剩余字节
                }
                else if ((readByteLength - 10) < currentFrameModel.getOriginDataBytesLength()) // 小于真实字节数据长度，不能进行处理，等待下次继续接收
                {
                    // 处理正常数据
                    byte[] dataBytes = new byte[readByteLength - 10];
                    Buffer.BlockCopy(receiveBuffer, index, dataBytes, 0, dataBytes.Length);
                    currentFrameModel.setOrigiDataBytes(dataBytes);

                    currentFrameModel.setIndex(0);
                    currentFrameModel.setStatus("2"); // 处理中
                }
            }
        }

        // 字节数据处理
        private bool receiveHandler(FrameModel perfectFrameModel, object obj, DoWorkEventArgs ex)
        {
            if (!(perfectFrameModel.getStatus() == "3"))
            {
                return false;
            }

            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.Append(Encoding.UTF8.GetString(perfectFrameModel.getOrigiDataBytes(), 0, perfectFrameModel.getOriginDataBytesLength())); // 获取帧数据
            perfectFrameModel.setData(stringBuilder.ToString());

            // 转换数据并追加；
            CommonRes commonRes = (CommonRes)JsonConvert.DeserializeObject(perfectFrameModel.getData(), typeof(CommonRes));
            return dataHandler(perfectFrameModel.getFrameType(), NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), ""), obj, ex);
        }

        // 业务员数据处理
        private bool dataHandler(byte frameType, NetResponse netResponse, object obj, DoWorkEventArgs ex)
        {
            if (frameType == (byte)5) // 5: 在线完成或是结束帧
            {
                if (((CommonRes)netResponse.getCommonRes()).getStatus() == "FILE.STREAM.SEND.END")
                {
                    return true;
                }
            }

            if (frameType == (byte)6) // 6: 个人网盘文件夹刷新
            {
                if (((CommonRes)netResponse.getCommonRes()).getOperate() == "UPLOAD.TRANSPORT.CONFIRM")
                {
                    return true;
                }

                if (((CommonRes)netResponse.getCommonRes()).getOperate() == "UPLOAD.TRANSPORT.CONFIRM.REPEAT")
                {
                    // 打印日志，修改当前行状态为未上传
                    Main_Form.main_Form.file_upload_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                    {
                        Main_Form.main_Form.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] " + ((CommonRes)netResponse.getCommonRes()).getMessage() + "\r\n");
                    }));

                    // 关闭当前socket
                    this.fileSendSocket.Close();
                    // 移除当前上传文件的FileStream，后期将会重新构建FileStream
                    this.dictionary.Remove("fileStream");
                    this.dictionary.Add("fileStream", new FileStream(this.dictionary["selectFilePath"].ToString(), FileMode.Open, FileAccess.Read, FileShare.Read));
                    ex.Cancel = true;
                    return false;
                }
            }

            return false;
        }

        private void resetFrameModelBasic()
        {
            currentFrameModel.setSumLength(0);
            currentFrameModel.setFrameType((byte)0);
            currentFrameModel.setEndFrame((byte)0);
            currentFrameModel.setIndex(0);
            currentFrameModel.setOriginDataBytesLength(0);
            currentFrameModel.setOrigiDataBytes(null);
            currentFrameModel.setData("");
            currentFrameModel.setStatus("1");
        }

        private void resetFrameModelAll()
        {
            currentFrameModel.setSumLength(0);
            currentFrameModel.setFrameType((byte)0);
            currentFrameModel.setEndFrame((byte)0);
            currentFrameModel.setIndex(0);
            currentFrameModel.setOriginDataBytesLength(0);
            currentFrameModel.setOrigiDataBytes(null);
            currentFrameModel.setData("");
            currentFrameModel.setRestBytes(null);
            currentFrameModel.setStatus("1");
        }

        /// <summary>
        /// 执行上传
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="eventArgs"></param>
        private int executeUpload(DoWorkEventArgs e)
        {
            int result = 0;
            // 获取文件基本数据
            FileStream fileStream = (FileStream)this.dictionary["fileStream"];
            if (executeUploadTransportHandler(fileStream, e))
            {
                this.dictionary["fileStatus"] = "WellDone";
                result = 1;
            }
            else
            {
                this.dataGridViewRow.Cells[10].Value = null; // helper
                this.dataGridViewRow.Cells[11].Value = null; // fileStream
            }

            // 不论是异常还是成功，最后都关闭文件在线传输通道
            fileStream.Close();// 移除当前上传文件的FileStream，后期将会重新构建FileStream
            this.fileSendSocket.Shutdown(SocketShutdown.Both);
            this.fileSendSocket.Close();

            return result;
        }

        /// <summary>
        /// 执行在线文件流实时传输
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="eventArgs"></param>
        private bool executeUploadTransportHandler(FileStream fileStream, DoWorkEventArgs eventArgs)
        {
            // 1、获取基本数据
            string launchUserName = this.dictionary["launchUserName"].ToString();
            long fileSize = (long)this.dictionary["fileSize"];
            string tag = this.dictionary["tag"].ToString();

            // 2、用于文件流读取数据缓存字节数组， 默认3072个字节
            byte[] fileStreamBytes = new byte[this.fileReadSize];
            int packetIndex = 1;
            long currentReadSize = 0;
            byte FileFrameTypeEnum = Convert.ToByte(FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.DATA_TRANSPORT.ToString()).ToString(), 2);
            byte fileType = Convert.ToByte(FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString()).ToString(), 2);
            byte fileOperateType = Convert.ToByte(FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.TRANSPORT.ToString()).ToString(), 2);
            byte[] sendDataBytes = Encoding.UTF8.GetBytes(launchUserName + "," + tag);
            byte[] sendDataLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sendDataBytes.Length));

            // 3、判断文件传送连接是否正常
            if (!NetServiceContext.isSocketConnected(this.fileSendSocket))
            {
                return false;
            }

            // 4、文件字节数据开始读取的位置:  
            // 4B -> 当前帧的总长度；
            // 1B -> 文件是否读取完[0:未读取完, 1:读取完], 
            // 4B -> 包序号 
            // 3B -> 帧类型，文件类型，文件操作类型；
            // ..... -> 剩余未文件流真实数据长度
            int orginStreamDataIndex = 4 + 1 + 4 + 3 + sendDataLengthBytes.Length + sendDataBytes.Length;

            // 5、开始发送文件内容
            int currentLoopCount = 0;
            while (true)
            {
                try
                {
                    // 6、上传任务被取消
                    if (Bg_Worker.CancellationPending)
                    {
                        // 任务被取消,回滚进度条
                        for (int k = currentLoopCount; k >= 0; k--)
                        {
                            this.Bg_Worker.ReportProgress(k);
                        }
                        doWorkEventArgs.Cancel = eventArgs.Cancel = true;

                        // 如果点击取消后刚好文件发送成功，则直接返回true表示文件发送成功 
                        if (Interlocked.Read(ref currentReadSize).Equals(fileSize))
                        {
                            return true;
                        }
                        else
                        {
                            // 向服务端发送文件取消发送消息，用于服务端关闭文件接收通道，暂时不用关闭socket通道，客户端会关闭socket，服务端也将自动关闭
                            FileService fileService = new FileService(this.dataGridViewRow);
                            fileService.cancelFileUploadTransport(this.dictionary, this.fileSendSocket);
                            return false;
                        }
                    }

                    // 7、读取文件内容
                    int readCount = fileStream.Read(fileStreamBytes, 0, fileStreamBytes.Length);
                    if (readCount > 0)
                    {

                        byte[] byteArrayRead = new byte[orginStreamDataIndex + readCount];

                        // 原子设置进度条当前数值,该进度条表示发送出去的进度
                        Interlocked.Add(ref currentReadSize, readCount);

                        // 记录当前发送帧总长度 4B
                        byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(byteArrayRead.Length));

                        // 拷贝标记数据(当前帧总长度,4B + 1B + 4B + 3B + ...)
                        frameSumLengthBytes.CopyTo(byteArrayRead, 0);
                        byteArrayRead[4] = Interlocked.Read(ref currentReadSize).Equals(fileSize) ? (byte)1 : (byte)0; //是否是结束帧

                        byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetIndex)); packetIndex++; // 当前包序号,int表示占据4个字节
                        frameIndexBytes.CopyTo(byteArrayRead, 5);

                        byteArrayRead[9] = fileFrameType; // 帧类型
                        byteArrayRead[10] = fileType; // 文件类型
                        byteArrayRead[11] = fileOperateType; // 文件操作类型

                        sendDataLengthBytes.CopyTo(byteArrayRead, 12);
                        sendDataBytes.CopyTo(byteArrayRead, sendDataLengthBytes.Length + 12);
                        Buffer.BlockCopy(fileStreamBytes, 0, byteArrayRead, orginStreamDataIndex, readCount);

                        // 8、socket远程发送, 从文件中读取到多少个字节，则发送多少个字节
                        int sendBytes = this.fileSendSocket.Send(byteArrayRead, byteArrayRead.Length, 0);

                        Thread.Sleep(100);

                        // 更新进度条
                        ++currentLoopCount;
                        this.Bg_Worker.ReportProgress(currentLoopCount);

                        // 清空字节数组
                        Array.Clear(fileStreamBytes, 0, fileStreamBytes.Length);
                    }
                    else if (readCount == 0)
                    {
                        // 文件发送完成: 如果从文件中读取的字节数与发送字节数相同，则发送在线文件发送完成的文件流结束通知,之后进行阻塞，等待服务端处理完成后关闭发送端文件以及socket通道
                        if (Interlocked.Read(ref currentReadSize).Equals(fileSize))
                        {
                            Thread.Sleep(30);
                            //// 发送文件传送成功消息
                            string frameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.DATA_TRANSPORT_END.ToString()).ToString();
                            string type = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString()).ToString();
                            string operateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.TRANSPORT.ToString()).ToString();

                            Dictionary<string, string> dic = new Dictionary<string, string>();
                            dic.Add("CLOSE.USER", launchUserName);
                            dic.Add("FILE.STREAM.SEND.END", "FILE.STREAM.SEND.END");
                            dic.Add("TAG", tag);
                            dic.Add("PID", this.dictionary["pid"].ToString());
                            dic.Add("FILE.NAME", this.dictionary["fileName"].ToString());
                            dic.Add("FILE.SIZE", this.dictionary["fileSize"].ToString());
                            dic.Add("FILE.PATH", this.dictionary["uploadFolderPath"].ToString());
                            dic.Add("FILE.TYPE", "FILE");
                            byte[] sendDataBytes1 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic));
                            byte[] sendDataLengthBytes1 = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sendDataBytes1.Length));

                            byte[] sendBytes = new byte[4 + 1 + 4 + 3 + sendDataBytes1.Length + sendDataLengthBytes1.Length];

                            byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sendBytes.Length));
                            frameSumLengthBytes.CopyTo(sendBytes, 0);
                            sendBytes[4] = (byte)1; // 结束帧

                            byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packetIndex)); packetIndex++; // 当前包序号,int表示占据4个字节
                            frameIndexBytes.CopyTo(sendBytes, 5);

                            sendBytes[9] = Convert.ToByte(frameType, 2);
                            sendBytes[10] = Convert.ToByte(type, 2);
                            sendBytes[11] = Convert.ToByte(operateType, 2);
                            sendDataLengthBytes1.CopyTo(sendBytes, 12);
                            sendDataBytes1.CopyTo(sendBytes, sendDataLengthBytes1.Length + 12);
                            this.fileSendSocket.Send(sendBytes, sendBytes.Length, 0);

                            Main_Form.main_Form.file_upload_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                            {
                                Main_Form.main_Form.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 成功发送文件 [ " + this.dictionary["fileName"].ToString() + " ] DB持久化消息\r\n");
                            }));

                            // 此处阻塞等待服务器发送关闭文件通道的通知
                            //waitingForFileIsNeedToOnlineTransport(eventArgs);
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

        /// <summary>
        /// ProgressChanged事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bg_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int value = e.ProgressPercentage;
            progressBarCell.Value = (int)(value * 1.0 / this.loopCount * (this.progressBarCell.Maximum));
        }

        /// <summary>
        /// RunWorkerCompleted事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bg_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("文件 [ " + this.dataGridViewRow.Cells[1].Value.ToString() + " ]上传出错！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.dataGridViewRow.Cells[4].Value = "上传失败";
                return;
            }
            else if (e.Cancelled)
            {
                doWorkEventArgs.Cancel = true;
                // 如果上传过程出现点击退出，会终止helper任务，会触发完成方法，此处会抛出空指针
                if (Main_Form.main_Form != null)
                {
                    Main_Form.main_Form.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + this.dataGridViewRow.Cells[1].Value.ToString() + " ] 上传取消成功\r\n");
                    this.dataGridViewRow.Cells[4].Value = "未上传";
                }
            }
            else
            {
                lock (obj)
                {
                    Main_Form.main_Form.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + this.dataGridViewRow.Cells[1].Value.ToString() + " ] 上传成功\r\n");
                    this.dataGridViewRow.Cells[4].Value = "上传成功";
                }
            }
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public void Do()
        {
            Bg_Worker.RunWorkerAsync();
        }

        /// <summary>
        /// 释放任务数量
        /// </summary>
        private void releaseTaskCount(DoWorkEventArgs e)
        {
            while (true)
            {
                lock (obj)
                {
                    if (Bg_Worker.CancellationPending)
                    {
                        // 任务取消，任务总数减1
                        if (taskCount > 0)
                        {
                            taskCount = taskCount - 1;
                        }
                        doWorkEventArgs.Cancel = e.Cancel = true;
                        break;
                    }

                    // 判断任务数是否达大于0，是则减去1个任务数量
                    if (taskCount > 0)
                    {
                        // 任务取消，任务总数减1
                        taskCount = taskCount - 1;
                        break;
                    }
                    else if (taskCount == 0)
                    {
                        Interlocked.CompareExchange(ref sleepTime, 5000, 1000);
                        break;
                    }
                }

                Thread.Sleep((int)Interlocked.Read(ref sleepTime));
            }
        }
    }
}
