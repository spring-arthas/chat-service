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
    public class AsyncPersonalFileDownloadHelper
    {
        // 当前处理中的FrameModel
        private FrameModel currentFrameModel = new FrameModel("1", 0);

        // 文件上传每次读取或写入字节数大小
        private int fileWriteSize = 0, loopCount = 0, currentLoopCount = 0;

        // 当前待下载文件大小
        private long fileSize = 0, alreadyWriteBytes = 0;

        public static int taskCount = 0;

        private static long sleepTime = 1000;

        private static object obj = new object();

        /// <summary>
        /// BackgroundWorker组件
        /// </summary>
        public BackgroundWorker Bg_Worker { get; set; }

        /// <summary>
        /// 文件上传Socket
        /// </summary>
        private Socket fileDownloadSocket;

        /// <summary>
        /// 表格内的进度条
        /// </summary>
        private DataGridViewProgressBarCell progressBarCell;

        /// <summary>
        /// 当前行
        /// </summary>
        private DataGridViewRow dataGridViewRow;

        /// <summary>
        /// 待删除文件数据
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
        public AsyncPersonalFileDownloadHelper(DataGridViewProgressBarCell progressBarCell, DataGridViewRow dataGridViewRow, Dictionary<string, object> dictionary)
        {
            this.dataGridViewRow = dataGridViewRow;
            this.dictionary = dictionary;
            this.progressBarCell = progressBarCell;

            if (this.fileWriteSize == 0)
            {
                this.fileWriteSize = NetServiceContext.fileOperateSize; // 10240
            }

            this.progressBarCell.Maximum = 100;
            this.progressBarCell.Mimimum = 0;

            // 创建组件
            this.Bg_Worker = new BackgroundWorker();
            this.Bg_Worker.WorkerReportsProgress = true;
            this.Bg_Worker.WorkerSupportsCancellation = true;

            // 绑定事件
            this.Bg_Worker.DoWork += backgroundWorker_executePersonalDownloadTransport_DoWork;
            this.Bg_Worker.ProgressChanged += bg_ProgressChanged;
            this.Bg_Worker.RunWorkerCompleted += bg_RunWorkerCompleted;
        }

        /// <summary>
        /// DoWork事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_executePersonalDownloadTransport_DoWork(object sender, DoWorkEventArgs e)
        {
            doWorkEventArgs = e;

            // 当前上传线程获取锁，获取成功执行文件上传，上传任务数+1
            while (true)
            {
                lock (obj)
                {
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
                    if (taskCount < 5)
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

                Thread.Sleep((int)Interlocked.Read(ref sleepTime));
            }

            // 1、判断文件发送连接是否建立,没建立则建立，建立则直接复用
            this.fileDownloadSocket = NetServiceContext.getSendFileSocket();
            NetResponse netResponse = NetServiceContext.initSendFileOnlineTransportSocketAndConnect(this.fileDownloadSocket, "FILE.DOWNLOAD");
            if (netResponse.getResponse() != NetResponse.Response.CONNECTION_SUCCESS)
            {
                Main_Form.main_Form.Invoke(new MethodInvoker(delegate ()
                {
                    MessageBox.Show("文件删除连接服务器失败！失败文件 [" + this.dictionary["fileName"] + "], 连接失败原因 [ " + netResponse.getResult() + " ]", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }));

                this.dataGridViewRow.Cells[4].Value = "删除失败";
                this.fileDownloadSocket.Close();
                releaseTaskCount(e);
                return;
            }

            // 2、发送当前批次在线传输文件数(以一批作为确认接收)
            int fileFrameType = FileFrameTypeEnum.GetEnumValue<FileFrameTypeEnum.TypeEnum>(FileFrameTypeEnum.TypeEnum.DOWNLOAD.ToString());
            int fileType = FileTypeEnum.GetEnumValue<FileTypeEnum.TypeEnum>(FileTypeEnum.TypeEnum.ALL.ToString());
            int fileOperateType = FileOperateTypeEnum.GetEnumValue<FileOperateTypeEnum.TypeEnum>(FileOperateTypeEnum.TypeEnum.STORE.ToString());
            Dictionary<string, object> sendDic = new Dictionary<string, object>();
            sendDic.Add("fileName", this.dictionary["fileName"]);
            sendDic.Add("fileCount", 1);
            sendDic.Add("fileSize", this.dictionary["fileSize"]);
            sendDic.Add("launchUserName", this.dictionary["launchUserName"]);
            sendDic.Add("tag", this.dictionary["tag"].ToString());//文件标识
            NetServiceContext.sendFileOnlineTransportMessageNotWaiting(this.fileDownloadSocket, fileFrameType.ToString(), fileType.ToString(), fileOperateType.ToString(), JsonConvert.SerializeObject(sendDic), long.Parse(this.dictionary["fileSize"].ToString()));

            // 4、同步等待文件是否上传的服务端确认消息
            bool result = waitingForFileIsNeedToOnlineTransport(e);
            if (result)
            {
                // 5、执行在线传输
                this.executeDownload(e);
                releaseTaskCount(e);
            }
            else
            {
                // 终止在线传送文件，可能为文件名称重复或是其他原因
                Main_Form.main_Form.file_upload_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件上传失败\r\n");
                }));

                this.dataGridViewRow.Cells[4].Value = "删除失败";
                this.fileDownloadSocket.Shutdown(SocketShutdown.Both);
                this.fileDownloadSocket.Close();

                // 释放资源
                releaseTaskCount(e);
            }
        }

        // 同步等待接收端用户是否确定需要在线传送文件
        private bool waitingForFileIsNeedToOnlineTransport(DoWorkEventArgs ex)
        {
            try
            {
                NetServiceContext.isSocketConnected(this.fileDownloadSocket);
                byte[] receiveBuffer = new byte[NetServiceContext.bufferSize];
                StringBuilder stringBuilder = new StringBuilder("");

                while (true)
                {
                    //开始接收信息
                    int readByteLength = this.fileDownloadSocket.Receive(receiveBuffer);
                    if (readByteLength == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    if (readByteLength == -1)
                    {
                        fileDownloadSocket.Close();
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
                    // 任务执行异常
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

            // 解析数据长度 2B
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
                    Buffer.BlockCopy(receiveBuffer, index, dataBytes, 0, readByteLength - 10);
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

        // 业务数据处理
        private bool dataHandler(byte frameType, NetResponse netResponse, object obj, DoWorkEventArgs ex)
        {
            if (((CommonRes)netResponse.getCommonRes()).getOperate() == "DOWNLOAD.TRANSPORT.CONFIRM")
            {
                this.loopCount = ((CommonRes)netResponse.getCommonRes()).getDownloadLoopCount();
                this.fileSize = ((CommonRes)netResponse.getCommonRes()).getFileSize();
                return true;
            }

            if (((CommonRes)netResponse.getCommonRes()).getOperate() == "DOWNLOAD.TRANSPORT.CONFIRM.NOT.EXIST")
            {
                // 打印日志，修改当前行状态为未上传
                Main_Form.main_Form.file_download_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] " + ((CommonRes)netResponse.getCommonRes()).getMessage() + "\r\n");
                }));

                // 关闭当前socket
                this.fileDownloadSocket.Close();
                // 移除当前上传文件的FileStream，后期将会重新构建FileStream
                this.dictionary.Remove("fileStream");
                this.dictionary.Add("fileStream", new FileStream(this.dictionary["downloadPath"].ToString(), FileMode.Open, FileAccess.Read, FileShare.Read));
                ex.Cancel = true;
                return true;
            }

            // 文件下载终止
            if (((CommonRes)netResponse.getCommonRes()).getOperate() == "DOWNLOAD.TRANSPORT.STOP")
            {
                // 打印日志，修改当前行状态为未上传
                Main_Form.main_Form.file_download_log_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] " + ((CommonRes)netResponse.getCommonRes()).getMessage() + "\r\n");
                }));
                
                // 移除当前上传文件的FileStream，后期将会重新构建FileStream
                this.dictionary.Remove("fileStream");
                this.dictionary.Add("fileStream", new FileStream(this.dictionary["downloadPath"].ToString(), FileMode.Open, FileAccess.Read, FileShare.Read));
                return true;
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
            currentFrameModel.setFrameIndex(0);
            currentFrameModel.setOperateType((byte)0);
            currentFrameModel.setCurrentWriteIndex(0);
            currentFrameModel.setNeedToWriteBytesLength(0);
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
            currentFrameModel.setFrameIndex(0);
            currentFrameModel.setOperateType((byte) 0);
            currentFrameModel.setCurrentWriteIndex(0);
            currentFrameModel.setNeedToWriteBytesLength(0);
            currentFrameModel.setRestBytes(null);
            currentFrameModel.setStatus("1");
        }

        /// <summary>
        /// 执行下载
        /// </summary>
        /// <param name="eventArgs"></param>
        private int executeDownload(DoWorkEventArgs e)
        {
            int result = 0;
            // 获取文件基本数据
            FileStream fileStream = (FileStream)this.dictionary["fileStream"];
            resetFrameModelAll();

            if (executeDownloadTransportHandler(fileStream, e))
            {
                this.dictionary["fileStatus"] = "WellDone";
                result = 1;
            }
            else
            {
                lock (obj)
                {
                    // 删除还未下载完成的文件
                    string filePath = this.dictionary["downloadPath"].ToString();
                    FileAttributes attr = File.GetAttributes(filePath);
                    if (attr != FileAttributes.Directory)
                    {
                        File.Delete(filePath);
                    }
                }

                // 清空当前下载文件行附加信息
                this.dataGridViewRow.Cells[9].Value = null; // helper
                this.dataGridViewRow.Cells[10].Value = null; // fileStream
            }

            // 不论是异常还是成功，最后都关闭文件在线传输通道
            // 移除当前上传文件的FileStream，后期将会重新构建FileStream
            fileStream.Close();
            this.fileDownloadSocket.Shutdown(SocketShutdown.Both);
            this.fileDownloadSocket.Close();

            return result;
        }

        /// <summary>
        /// 执行在线文件流实时传输
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="eventArgs"></param>
        private bool executeDownloadTransportHandler(FileStream fileStream, DoWorkEventArgs eventArgs)
        {
            // 1、获取基本数据
            string launchUserName = this.dictionary["launchUserName"].ToString();
            string tag = this.dictionary["tag"].ToString();

            // 2、判断文件传送连接是否正常
            if (!NetServiceContext.isSocketConnected(this.fileDownloadSocket))
            {
                return false;
            }
            
            // 3、远程接收服务器文件流信息
            int currentReadSize = 0;
            while (true)
            {
                try
                {
                    // 4、上传任务被取消,即使服务端依旧在传输文件下载流，客户端都不会接收写入本地文件
                    if (Bg_Worker.CancellationPending)
                    {
                        // 任务被取消,回滚进度条
                        for (int k = currentLoopCount; k >= 0; k--)
                        {
                            this.Bg_Worker.ReportProgress(k);
                        }
                        doWorkEventArgs.Cancel = eventArgs.Cancel = true;

                        // 如果点击取消后刚好文件发送成功，则直接返回true表示文件发送成功 
                        if (alreadyWriteBytes == fileSize)
                        {
                            return true;
                        }
                        else
                        {
                            // 向服务端发送文件取消发送消息，用于服务端关闭文件接收通道，暂时不用关闭socket通道，客户端会关闭socket，服务端也将自动关闭
                            FileService fileService = new FileService(this.dataGridViewRow);
                            fileService.cancelFileDonwloadTransport(this.dictionary, this.fileDownloadSocket);
                            return false;
                            //bool result = waitingForFileIsNeedToOnlineTransport(null);
                            //if (result)
                            //{
                            //    return false;
                            //}
                        }
                    }

                    // 5、接收服务端文件流数据
                    byte[] receiveBuffer = new byte[this.fileWriteSize];
                    currentReadSize = this.fileDownloadSocket.Receive(receiveBuffer);
                    if (currentReadSize > 0)
                    {
                        // 解决半包粘包问题
                        this.stickingAndAalfWrapping(fileStream, currentReadSize, receiveBuffer);

                        if (alreadyWriteBytes == this.fileSize)
                        {
                            // 文件流数据接收完成，向服务端发送文件下载完成的通知，服务端关闭文件通道
                            return true;
                        }
                    }
                    else if (currentReadSize == 0)
                    {
                        // 文件流数据接收完成，向服务端发送文件下载完成的通知，服务端关闭文件通道
                        if (alreadyWriteBytes == this.fileSize)
                        {

                        }
                    }
                }
                catch (Exception e)
                {
                    Main_Form.main_Form.file_download_log_richTextBox.Text = e.Message.ToString();
                    return false;
                }
            }
        }

        /// <summary>
        /// 处理文件流数据, 总共  2+1+4+1+1+1 = 10
        ///     帧总长度 2B 
        ///     结束帧   1B
        ///     帧序号   4B
        ///     帧类型   1B
        ///   文件类型   1B
        ///文件操作类型  1B
        /// </summary>
        /// <param name="fileStream"> 文件流对象 </param>
        /// <param name="currentReadSize"> 当前读取的字节数,小于等于receiveBuffer长度 </param>
        /// <param name="receiveBuffer"> 字节数组 </param>
        private void stickingAndAalfWrapping(FileStream fileStream, int currentReadSize, byte[] receiveBuffer)
        {
            // 如果缓存中有数据，那先将缓存中的数据与当前接收到的数据进行合并
            if (currentFrameModel.getRestBytes() != null && currentFrameModel.getRestBytes().Length > 0)
            {
                // 合并后判断可处理字节数是否大于等于10，为真方可处理，否则继续缓存
                int currentAvaliableHandleBytesLength = currentFrameModel.getRestBytes().Length + currentReadSize;
                if (currentAvaliableHandleBytesLength <= 10)
                {
                    // 依旧无法正常读取，继续放入缓存
                    byte[] appendBytes = new byte[currentAvaliableHandleBytesLength];
                    Buffer.BlockCopy(currentFrameModel.getRestBytes(), 0, appendBytes, 0, currentFrameModel.getRestBytes().Length);
                    // 有可能在读取到文件末尾时，剩余文件字节数组个数是小于receiveBuffer，所以不能已receiveBuffer大小来拷贝数组
                    Buffer.BlockCopy(receiveBuffer, 0, appendBytes, currentFrameModel.getRestBytes().Length, currentReadSize);
                    currentFrameModel.setRestBytes(appendBytes);
                    return;
                }

                // 可以处理，则进行处理
                cacheAppendHandler(fileStream, currentFrameModel.getRestBytes().Length, currentFrameModel.getRestBytes());

                currentFrameModel.setRestBytes(null);
                currentFrameModel.setIndex(0);
            }
            else
            {
                // 缓存中没有数据，直接进行receiveBuffer处理
                if (currentReadSize <= 10)
                {
                    // 直接将数据放入缓存数组
                    byte[] cacheBytes = new byte[currentReadSize];
                    Buffer.BlockCopy(receiveBuffer, 0, cacheBytes, 0, currentReadSize);
                    currentFrameModel.setRestBytes(cacheBytes);
                    return;
                }
                else
                {
                    // 可以处理，则进行处理
                    normalHandler(fileStream, currentReadSize, receiveBuffer);
                }
            }
        }

        /// <summary>
        /// 缓存追加字节处理，此方法处理时，appendReadSize一定等于appendBuffer
        /// </summary>
        /// <param name="fileStream"> 文件流对象 </param>
        /// <param name="appendReadSize"></param>
        /// <param name="appendBuffer"></param>
        /// <param name="currentLoopCount">当前循环次数</param>
        private void cacheAppendHandler(FileStream fileStream, int appendReadSize, byte[] appendBuffer)
        {
            bool isReadDoneBasicData = false;
            int index = 0;
            while (index < appendBuffer.Length)
            {
                if (currentFrameModel.getStatus() == "NOT_WELL_DONE")
                {
                    // 获取剩余需要写入的字节个数
                    int restWriteBytesLength = currentFrameModel.getNeedToWriteBytesLength();
                    if (appendBuffer.Length < restWriteBytesLength)
                    {
                        // 依旧不能写入当前帧规定的完整字节流数据，则再次能写多少写多少
                        fileStream.Write(appendBuffer, 0, appendBuffer.Length);
                        alreadyWriteBytes += appendBuffer.Length; // 统计已经写入的文件流字节大小

                        // 更新进度条
                        ++currentLoopCount;
                        this.Bg_Worker.ReportProgress(currentLoopCount);

                        currentFrameModel.setNeedToWriteBytesLength(restWriteBytesLength - appendBuffer.Length);
                        return;
                    }
                    else if(appendBuffer.Length >= restWriteBytesLength)
                    {
                        // 刚好够，则直接进行写入
                        fileStream.Write(appendBuffer,0, restWriteBytesLength);
                        alreadyWriteBytes += restWriteBytesLength; // 统计已经写入的文件流字节大小

                        // 更新进度条
                        ++currentLoopCount;
                        this.Bg_Worker.ReportProgress(currentLoopCount);

                        resetFrameModelAll();
                    }
                }

                // 解析前12个字节基础数据
                if (!isReadDoneBasicData)
                {
                    // 1、解析帧总长度 4B
                    if (currentFrameModel.getSumLength() == 0)
                    {
                        int value = (int)((appendBuffer[index] & 0xFF)
                            | ((appendBuffer[index + 1] & 0xFF) << 8)
                            | ((appendBuffer[index + 2] & 0xFF) << 16)
                            | ((appendBuffer[index + 3] & 0xFF) << 24));
                        currentFrameModel.setSumLength(IPAddress.NetworkToHostOrder(value));
                    }

                    // 2、解析是否结束帧 1B
                    if (currentFrameModel.getEndFrame() == (byte)0)
                    {
                        currentFrameModel.setEndFrame(appendBuffer[index + 4] == (byte)0 ? (byte)0 : (byte)1);
                    }

                    // 3、帧序号 4B
                    if (currentFrameModel.getFrameIndex() == 0)
                    {
                        int value = (int)((appendBuffer[index + 5] & 0xFF)
                            | ((appendBuffer[index + 6] & 0xFF) << 8)
                            | ((appendBuffer[index + 7] & 0xFF) << 16)
                            | ((appendBuffer[index + 8] & 0xFF) << 24));
                        currentFrameModel.setFrameIndex(IPAddress.NetworkToHostOrder(value));
                    }

                    // 4、文件流数据长度 4B
                    if (currentFrameModel.getOriginDataBytesLength() == 0)
                    {
                        int value = (int)((appendBuffer[index + 9] & 0xFF)
                           | ((appendBuffer[index + 10] & 0xFF) << 8)
                           | ((appendBuffer[index + 11] & 0xFF) << 16)
                           | ((appendBuffer[index + 12] & 0xFF) << 24));
                        currentFrameModel.setOriginDataBytesLength(IPAddress.NetworkToHostOrder(value));
                    }

                    index = index + 13;
                    isReadDoneBasicData = true;
                }
                else
                {
                    // 根据当前帧总长度，判断当前帧应该写入进文件的字节数是否足够，不足则不能清空当前帧，因为并没有实际处理完帧所表示的文件流大小
                    // 读取成功前10个字节以及消息数据，剩余为文件真实字节流数据,直接写入
                    int canWriteBytesLength = appendBuffer.Length - index;

                    // 当前帧中需要被写入的文件流字节数据长度
                    int needBytesLength = currentFrameModel.getSumLength() - index;

                    if (canWriteBytesLength == needBytesLength)
                    {
                        // 刚好够，则直接进行写入
                        fileStream.Write(appendBuffer, index, canWriteBytesLength);
                        alreadyWriteBytes += canWriteBytesLength; // 统计已经写入的文件流字节大小

                        // 更新进度条
                        ++currentLoopCount;
                        this.Bg_Worker.ReportProgress(currentLoopCount);

                        resetFrameModelAll();
                    }
                    else if (canWriteBytesLength < needBytesLength)
                    {
                        // 写入文件，此处能写多少写多少
                        fileStream.Write(appendBuffer, index, canWriteBytesLength);
                        alreadyWriteBytes += canWriteBytesLength; // 统计已经写入的文件流字节大小

                        // 更新进度条
                        ++currentLoopCount;
                        this.Bg_Worker.ReportProgress(currentLoopCount);
                        
                        currentFrameModel.setNeedToWriteBytesLength(needBytesLength - canWriteBytesLength);

                        // 能写入的字节个数小于当前帧指定需要写入文件的字节数，这中情况发生在当前帧数据被拆包的下一个帧中，及下一个帧的前部分字节为文件流字节
                        // 所以此处需要设置当前帧的状态为未处理成功
                        currentFrameModel.setStatus("NOT_WELL_DONE");
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// 正常处理，此方法处理时，appendReadSize小于等于appendBuffer
        /// </summary>
        /// <param name="fileStream"> 文件流对象 </param>
        /// <param name="appendReadSize"></param>
        /// <param name="appendBuffer"></param>
        /// <param name="currentLoopCount">当前循环次数</param>
        private void normalHandler(FileStream fileStream, int appendReadSize, byte[] appendBuffer)
        {
            bool isReadDoneBasicData = false;
            int index = 0;
            while (index < appendReadSize)
            {
                if (currentFrameModel.getStatus() == "NOT_WELL_DONE")
                {
                    // 获取剩余需要写入的字节个数
                    int restWriteBytesLength = currentFrameModel.getNeedToWriteBytesLength();
                    if (currentFrameModel.getOrigiDataBytes() != null)
                    {
                        byte[] completeWriteBytes = new byte[restWriteBytesLength + currentFrameModel.getOrigiDataBytes().Length];
                        if (appendReadSize < restWriteBytesLength)
                        {
                            Buffer.BlockCopy(currentFrameModel.getOrigiDataBytes(), 0, completeWriteBytes, 0, currentFrameModel.getOrigiDataBytes().Length);
                            Buffer.BlockCopy(appendBuffer, 0, completeWriteBytes, currentFrameModel.getOrigiDataBytes().Length, appendReadSize);
                            index = index + appendReadSize;
                        }
                        else if (appendReadSize >= restWriteBytesLength)
                        {
                            Buffer.BlockCopy(currentFrameModel.getOrigiDataBytes(), 0, completeWriteBytes, 0, currentFrameModel.getOrigiDataBytes().Length);
                            Buffer.BlockCopy(appendBuffer, 0, completeWriteBytes, currentFrameModel.getOrigiDataBytes().Length, restWriteBytesLength);
                            index = index + restWriteBytesLength;
                        }

                        if (completeWriteBytes.Length == currentFrameModel.getOriginDataBytesLength())
                        {
                            fileStream.Write(completeWriteBytes, 0, completeWriteBytes.Length);
                            alreadyWriteBytes += completeWriteBytes.Length; // 统计已经写入的文件流字节大小
                            
                            // 更新进度条
                            ++currentLoopCount;
                            this.Bg_Worker.ReportProgress(currentLoopCount);

                            resetFrameModelAll();
                        }
                        else
                        {
                            currentFrameModel.setOrigiDataBytes(completeWriteBytes);
                            currentFrameModel.setNeedToWriteBytesLength(currentFrameModel.getOriginDataBytesLength() - completeWriteBytes.Length);
                            return;
                        }
                    }
                }

                // 解析前12个字节基础数据
                if (!isReadDoneBasicData)
                {
                    // 1、解析帧总长度 4B
                    if (currentFrameModel.getSumLength() == 0)
                    {
                        int value = (int)((appendBuffer[index] & 0xFF)
                            | ((appendBuffer[index + 1] & 0xFF) << 8)
                            | ((appendBuffer[index + 2] & 0xFF) << 16)
                            | ((appendBuffer[index + 3] & 0xFF) << 24));
                        currentFrameModel.setSumLength(IPAddress.NetworkToHostOrder(value));
                    }

                    // 2、解析是否结束帧 1B
                    if (currentFrameModel.getEndFrame() == (byte)0)
                    {
                        currentFrameModel.setEndFrame(appendBuffer[index + 4] == (byte)0 ? (byte)0 : (byte)1);
                    }

                    // 3、帧序号 4B
                    if (currentFrameModel.getFrameIndex() == 0)
                    {
                        int value = (int)((appendBuffer[index + 5] & 0xFF)
                            | ((appendBuffer[index + 6] & 0xFF) << 8)
                            | ((appendBuffer[index + 7] & 0xFF) << 16)
                            | ((appendBuffer[index + 8] & 0xFF) << 24));
                        currentFrameModel.setFrameIndex(IPAddress.NetworkToHostOrder(value));
                    }

                    // 4、文件流数据长度 4B
                    if (currentFrameModel.getOriginDataBytesLength() == 0)
                    {
                        int value = (int)((appendBuffer[index + 9] & 0xFF)
                           | ((appendBuffer[index + 10] & 0xFF) << 8)
                           | ((appendBuffer[index + 11] & 0xFF) << 16)
                           | ((appendBuffer[index + 12] & 0xFF) << 24));
                        currentFrameModel.setOriginDataBytesLength(IPAddress.NetworkToHostOrder(value));
                    }

                    index = index + 13;
                    isReadDoneBasicData = true;
                }
                else
                {
                    // 根据当前帧总长度，判断当前帧应该写入进文件的字节数是否足够，不足则不能清空当前帧，因为并没有实际处理完帧所表示的文件流大小
                    // 读取成功前10个字节以及消息数据，剩余为文件真实字节流数据,直接写入
                    int bufferRestBytesLength = appendReadSize - index;

                    if (bufferRestBytesLength == currentFrameModel.getOriginDataBytesLength())
                    {
                        // 刚好够写直接写入
                        fileStream.Write(appendBuffer, index, bufferRestBytesLength);
                        alreadyWriteBytes += bufferRestBytesLength; // 统计已经写入的文件流字节大小

                        // 更新进度条
                        ++currentLoopCount;
                        this.Bg_Worker.ReportProgress(currentLoopCount);

                        resetFrameModelAll();
                    }
                    else if (bufferRestBytesLength < currentFrameModel.getOriginDataBytesLength())
                    {
                        // 剩余字节数小于文件流字节数，则进行缓存，不触发写，必须凑够个数方可写入，否则进度条将显示不全
                        byte[] restBytes = new byte[bufferRestBytesLength];
                        Buffer.BlockCopy(appendBuffer, index, restBytes, 0, restBytes.Length);
                        currentFrameModel.setOrigiDataBytes(restBytes);
                        currentFrameModel.setNeedToWriteBytesLength(currentFrameModel.getOriginDataBytesLength() - bufferRestBytesLength);
                        
                        // 能写入的字节个数小于当前帧指定需要写入文件的字节数，这中情况发生在当前帧数据被拆包的下一个帧中，及下一个帧的前部分字节为文件流字节
                        // 所以此处需要设置当前帧的状态为未处理成功
                        currentFrameModel.setStatus("NOT_WELL_DONE");
                    }

                    return;
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
                MessageBox.Show("文件 [ " + this.dataGridViewRow.Cells[1].Value.ToString() + " ] 下载出错！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.dataGridViewRow.Cells[3].Value = "下载失败";
                return;
            }
            else if (e.Cancelled)
            {
                doWorkEventArgs.Cancel = true;
                if (Main_Form.main_Form != null)
                {
                    Main_Form.main_Form.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + this.dataGridViewRow.Cells[1].Value.ToString() + " ] 下载取消成功\r\n");
                    this.dataGridViewRow.Cells[3].Value = "待下载";
                }
            }
            else
            {
                lock (obj)
                {
                    Main_Form.main_Form.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + this.dataGridViewRow.Cells[1].Value.ToString() + " ] 下载成功\r\n");
                    this.dataGridViewRow.Cells[3].Value = "下载成功";
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
