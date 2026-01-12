using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using chat_service.net;
using Newtonsoft.Json;
using chat_service.frame;
using chat_service.user;
using System.Threading;
using System.Windows.Forms;
using chat_service.service.file;
using chat_service.file;

namespace chat_service.util
{
    public class NetServiceContext
    {
        // 远程服务地址
        public static string remoteServiceAddress = "", remoteFileServiceAddress = "", remoteFileDownloadServiceAddress = "";
        // 下载路径
        public static string globalDownloadPath = "";
        // socket send buffer size
        public static int socketSendBufferSize = 0;
        // socket receive buffer size
        public static int socketReceiveBufferSize = 0;
        // file read buffer size
        public static int fileOperateSize = 0;
        // 接收缓冲数据大小
        public static int bufferSize = 0;
        // 远程ip
        public static IPAddress remoteIp;
        // 远程端口
        public static int remotePort;
        // 聊天服务socket套接字
        public static Socket socket = null;
        // 文件服务地址
        public static string[] address = null;
        // 当前处理中的FrameModel
        private static FrameModel currentFrameModel = new FrameModel("1", 0);
        // socket建立事件
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        // socket接收事件
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        // socket接收事件
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // 建立聊天连接
        public static NetResponse chatInitSocketAndConnect()
        {
            // 初始化socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Linger, 10);
            string[] address = remoteServiceAddress.Split(':');

            // 获取连接地址
            remoteIp = IPAddress.Parse(address[0]);
            remotePort = Convert.ToInt32(address[1]);
            IPEndPoint remoteEP = new IPEndPoint(remoteIp, remotePort);
            try
            {
                // 建立连接直到连接完成
                socket.BeginConnect(remoteEP, new AsyncCallback(chatConnectCallback), socket);
                //bool connect = connectDone.WaitOne();

                // 判断socket连接状态
                isSocketConnected(socket);
                return NetResponse.ofConnectionSuccess("与服务器连接成功 [" + (remoteIp.ToString() + ":" + remotePort) + "]");
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (e.NativeErrorCode.Equals(10035))
                {
                    return NetResponse.ofConnectionSuccess("与服务器连接成功 [" + (remoteIp.ToString() + ":" + remotePort) + "]");
                }

                return NetResponse.ofConnectFail("尝试与服务端连接异常, 请尝试重连或重新设置, [" + (remoteIp.ToString() + ":" + remotePort) + "]");
            }
        }

        // 聊天连接回调
        private static void chatConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.     
                Socket client = (Socket)ar.AsyncState;
                // 完成连接    
                client.EndConnect(ar);
                // Signal that the connection has been made.     
                connectDone.Set();
            }
            catch (Exception e)
            {
                string message = e.Message;
            }
        }

        // 聊天消息同步发送无需等待返回
        public static void sendMessageNotWaiting(byte frameType, string message, object obj)
        {
            if (!socket.Connected)
            {
                Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.result_label.Text = "与服务端连接状态异常, 尝试重启客户端......";
                }));
                return;
            }


            // 1 、消息内容数据
            byte[] frameContextBytes = Encoding.UTF8.GetBytes(message);
            // 2 、消息内容长度
            byte[] frameLenthBytes = BitConverter.GetBytes((short)frameContextBytes.Length); Array.Reverse(frameLenthBytes);
            // 3、是否是结束帧
            byte endFrame = (byte)1;
            // 4、帧总长度数据
            byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(4 + 1 + 1 + 4 + frameLenthBytes.Length + frameContextBytes.Length));
            // 5、构造发送字节数组  4B: 帧总长度数据  1B: 是否是结束帧(1:是，0：不是)1B: 帧类型
            byte[] sendBytes = new byte[4 + 1 + 1 + 4 + frameLenthBytes.Length + frameContextBytes.Length];

            // 6、封装数据
            sendBytes[0] = frameSumLengthBytes[0];
            sendBytes[1] = frameSumLengthBytes[1];
            sendBytes[2] = frameSumLengthBytes[2];
            sendBytes[3] = frameSumLengthBytes[3];
            sendBytes[4] = endFrame;
            byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)); // 当前真实数据,int表示占据4个字节
            frameIndexBytes.CopyTo(sendBytes, 5);

            sendBytes[9] = (byte)frameType;
            frameLenthBytes.CopyTo(sendBytes, 10);
            frameContextBytes.CopyTo(sendBytes, frameLenthBytes.Length + 10);
            // 7、主线程阻塞进行数据发送，不采取异步线程
            Dictionary<string, object> stateDictionary = new Dictionary<string, object>();
            stateDictionary.Add("socket", socket);
            stateDictionary.Add("object", obj);
            socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, new AsyncCallback(sendChatMesageCallback), stateDictionary);
        }

        // 聊天消息发送回调
        private static void sendChatMesageCallback(IAsyncResult ar)
        {
            Object obj = null;
            try
            {
                Dictionary<string, object> stateDictionary = (Dictionary<string, object>)ar.AsyncState;
                obj = stateDictionary["object"];
                Socket socket = (Socket)stateDictionary["socket"];
                socket.EndSend(ar);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                // 设置主窗体连接状态
                if (obj is Login_Register_Form)
                {
                    ((Login_Register_Form)obj).Invoke(new MethodInvoker(delegate () { ((Login_Register_Form)obj).connect_label.Text = "连接中......"; }));
                }

                // 设置登录窗体连接状态
                if (obj is Main_Form)
                {
                    ((Main_Form)obj).Invoke(new MethodInvoker(delegate () { ((Main_Form)obj).result_label.Text = "连接中......"; }));
                }

                // 尝试重连
            }

        }





        // 实例化文件发送socket
        public static Socket getSendFileSocket()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, 10);
            return socket;
        }

        // 实例化文件接收socket
        public static Socket getReceiveFileSocket()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Linger, 10);
            return socket;
        }







        // 同步建立文件在线传输连接
        public static NetResponse initSendFileOnlineTransportSocketAndConnect(Socket fileSendSocket, string taskType)
        {
            if (taskType == "FILE.UPLOAD")
            {
                address = remoteFileServiceAddress.Split(':');
            }
            else if (taskType == "FILE.DOWNLOAD")
            {
                address = remoteFileDownloadServiceAddress.Split(':');
            }
            // 获取连接地址
            remoteIp = IPAddress.Parse(address[0]);
            remotePort = Convert.ToInt32(address[1]);
            IPEndPoint remoteEP = new IPEndPoint(remoteIp, remotePort);
            try
            {
                // 建立连接直到连接完成
                fileSendSocket.Connect(remoteEP);
                //fileSendSocket.BeginConnect(remoteEP, new AsyncCallback(sendOnlineTransportConnectCallback), fileSendSocket);
                //bool connect = connectDone.WaitOne();

                // 判断socket连接状态
                isSocketConnected(fileSendSocket);
                return NetResponse.ofConnectionSuccess("与服务器连接成功 [" + (remoteIp.ToString() + ":" + remotePort) + "]");
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (e.NativeErrorCode.Equals(10035))
                {
                    return NetResponse.ofConnectionSuccess("与服务器连接成功 [" + (remoteIp.ToString() + ":" + remotePort) + "]");
                }

                return NetResponse.ofConnectFail("尝试与服务端连接异常, 请尝试重连或重新设置, [" + (remoteIp.ToString() + ":" + remotePort) + "]");
            }
        }

        /// <summary>
        /// 文件在线传输发送数据无需等待返回
        /// </summary>
        /// <param name="fileSendSocket">文件上传socket</param>
        /// <param name="frameType">帧类型</param>
        /// <param name="fileType">文件类型</param>
        /// <param name="fileOperateType">文件操作类型</param>
        /// <param name="message">待发送的文件数据</param>
        /// <param name="fileSize">当前文件大小</param>
        public static void sendFileOnlineTransportMessageNotWaiting(Socket fileSendSocket, string frameType, string fileType, string fileOperateType, string message, long fileSize)
        {
            if (!NetServiceContext.isSocketConnected(fileSendSocket))
            {
                if (Main_Form.main_Form != null)
                {
                    Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                    {
                        Main_Form.main_Form.result_label.Text = "与服务端连接状态异常, 尝试重启客户端......";
                    }));
                    return;
                }
            }

            // 1、帧头长度 3B --> 帧类型、文件类型、文件操作类型
            byte fileFrameTypeByte = Convert.ToByte(frameType, 2);
            byte fileTypeByte = Convert.ToByte(fileType, 2);
            byte fileOperateTypeByte = Convert.ToByte(fileOperateType, 2);
            // 2、文件名称字节数组长度 2B
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(message);
            byte[] fileNameLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)fileNameBytes.Length));
            // 文件内容大小字节长度 8B
            byte[] fileSizeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(fileSize));
            // 帧总长度 4B  
            byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(4 + 1 + 4 + 3 + fileNameLengthBytes.Length + fileSizeBytes.Length + fileNameBytes.Length));

            // 3、封装文件发送字节数组 --> 帧总长度字节数: 4B + 是否结束帧: 1B + 帧类型: 1B + 文件类型: 1B + 文件操作类型: 1B + 文件名称长度字节数: 1B + 文件大小字节数: 8B
            byte[] sendBytes = new byte[8 + fileNameLengthBytes.Length + fileSizeBytes.Length + fileNameBytes.Length + frameSumLengthBytes.Length];
            frameSumLengthBytes.CopyTo(sendBytes, 0);
            sendBytes[4] = (byte)1;

            byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)); // 当前包序号,int表示占据4个字节
            frameIndexBytes.CopyTo(sendBytes, 5);

            sendBytes[9] = fileFrameTypeByte;
            sendBytes[10] = fileTypeByte;
            sendBytes[11] = fileOperateTypeByte;
            fileNameLengthBytes.CopyTo(sendBytes, 12);
            fileSizeBytes.CopyTo(sendBytes, fileNameLengthBytes.Length + 12);
            fileNameBytes.CopyTo(sendBytes, fileSizeBytes.Length + fileNameLengthBytes.Length + 12);

            // 主线程阻塞进行数据发送，不采取异步线程
            Dictionary<string, object> stateDictionary = new Dictionary<string, object>();
            stateDictionary.Add("socket", fileSendSocket);
            fileSendSocket.BeginSend(sendBytes, 0, sendBytes.Length, 0, new AsyncCallback(sendOrReceiveFileOnlineTransportMesageCallback), stateDictionary);
        }








        // 文件在线传输连接回调
        private static void sendOnlineTransportConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.     
                Socket client = (Socket)ar.AsyncState;
                // 完成连接    
                client.EndConnect(ar);
                // Signal that the connection has been made.     
                connectDone.Set();
            }
            catch (Exception e)
            {

            }
        }

        // 文件在线传输或接受发送数据回调
        private static void sendOrReceiveFileOnlineTransportMesageCallback(IAsyncResult ar)
        {
            try
            {
                Dictionary<string, object> stateDictionary = (Dictionary<string, object>)ar.AsyncState;
                Socket socket = (Socket)stateDictionary["socket"];
                socket.EndSend(ar);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                // 设置登录窗体连接状态
                //Main_Form.main_Form.Invoke(new MethodInvoker(delegate () { Main_Form.main_Form.result_label.Text = "连接中......"; }));

                // 尝试重连
            }

        }







        // 建立文件在线接收连接
        public static NetResponse initReceiveFileOnlineTransportSocketAndConnect(Socket fileReceiveSocket)
        {
            // 初始化socket
            string[] address = remoteFileServiceAddress.Split(':');

            // 获取连接地址
            remoteIp = IPAddress.Parse(address[0]);
            remotePort = Convert.ToInt32(address[1]);
            IPEndPoint remoteEP = new IPEndPoint(remoteIp, remotePort);
            try
            {
                // 建立连接直到连接完成
                fileReceiveSocket.BeginConnect(remoteEP, new AsyncCallback(sendOnlineTransportConnectCallback), fileReceiveSocket);
                bool connect = connectDone.WaitOne();

                // 判断socket连接状态
                isSocketConnected(fileReceiveSocket);
                return NetResponse.ofConnectionSuccess("与服务器连接成功 [" + (remoteIp.ToString() + ":" + remotePort) + "]");
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (e.NativeErrorCode.Equals(10035))
                {
                    return NetResponse.ofConnectionSuccess("与服务器连接成功 [" + (remoteIp.ToString() + ":" + remotePort) + "]");
                }

                return NetResponse.ofConnectFail("尝试与服务端连接异常, 请尝试重连或重新设置, [" + (remoteIp.ToString() + ":" + remotePort) + "]");
            }
        }

        // 接收在线传输发送数据无需等待返回
        public static void receiveFileOnlineTransportMessageNotWaiting(Socket fileReceiveSocket, string frameType, string fileType, string fileOperateType, string message, long fileSize)
        {
            try
            {
                isSocketConnected(fileReceiveSocket);
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (!e.NativeErrorCode.Equals(10035))
                {
                    Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                    {
                        Main_Form.main_Form.result_label.Text = "与服务端连接状态异常, 尝试重启客户端......";
                    }));
                    return;
                }
            }

            // 1、帧头长度 3B --> 帧类型、文件类型、文件操作类型
            byte fileFrameTypeByte = Convert.ToByte(frameType, 2);
            byte fileTypeByte = Convert.ToByte(fileType, 2);
            byte fileOperateTypeByte = Convert.ToByte(fileOperateType, 2);
            // 2、文件名称字节数组长度 2B
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(message);
            byte[] fileNameLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)fileNameBytes.Length));
            // 文件内容大小字节长度 8B
            byte[] fileSizeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(fileSize));
            // 帧总长度 2B
            byte[] frameSumLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)(2 + 1 + 4 + 3 + fileNameLengthBytes.Length + fileSizeBytes.Length + fileNameBytes.Length)));

            //封装文件发送字节数组-- > 帧总长度字节数: 2B + 是否结束帧: 1B + 帧类型: 1B + 文件类型: 1B + 文件操作类型: 1B + 文件名称长度字节数: 1B + 文件大小字节数: 8B
            byte[] sendBytes = new byte[8 + fileNameLengthBytes.Length + fileSizeBytes.Length + fileNameBytes.Length + frameSumLengthBytes.Length];
            frameSumLengthBytes.CopyTo(sendBytes, 0);
            sendBytes[2] = (byte)1;

            byte[] frameIndexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)); // 当前包序号,int表示占据4个字节
            frameIndexBytes.CopyTo(sendBytes, 3);
            sendBytes[7] = fileFrameTypeByte;
            sendBytes[8] = fileTypeByte;
            sendBytes[9] = fileOperateTypeByte;
            fileNameLengthBytes.CopyTo(sendBytes, 10);
            fileSizeBytes.CopyTo(sendBytes, fileNameLengthBytes.Length + 10);
            fileNameBytes.CopyTo(sendBytes, fileSizeBytes.Length + fileNameLengthBytes.Length + 10);
            // 主线程阻塞进行数据发送，不采取异步线程
            Dictionary<string, object> stateDictionary = new Dictionary<string, object>();
            stateDictionary.Add("socket", fileReceiveSocket);
            fileReceiveSocket.BeginSend(sendBytes, 0, sendBytes.Length, 0, new AsyncCallback(sendOrReceiveFileOnlineTransportMesageCallback), stateDictionary);
        }







        // 数据接收  Array.Clear(bytes, 0, bytes.Length);
        public static void loopReceiveServiceData(object obj)
        {
            // Begin receiving the data from the remote device.     
            if (socket.Connected)
            {
                byte[] receiveBuffer = new byte[bufferSize];
                StringBuilder stringBuilder = new StringBuilder("");

                while (true)
                {
                    try
                    {
                        //开始接收信息
                        int readByteLength = socket.Receive(receiveBuffer);
                        if (readByteLength == 0)
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                        if (readByteLength == -1)
                        {
                            socket.Close();
                            break;
                        }

                        // 不足基本帧数据，则不处理只缓存
                        if (readByteLength < 10) // 长度不为8，即连基本帧信息数据都不够，则不进行解析
                        {
                            continue;
                        }

                        if (currentFrameModel.getStatus() == "1") // 从未处理，执行首次处理
                        {
                            parseBytes(readByteLength, receiveBuffer, obj);
                            // 如果刚好字节读取完成则直接处理
                            if (currentFrameModel.getStatus() == "3")
                            {
                                receiveHandler(currentFrameModel, obj);
                                resetFrameModelAll();
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
                                receiveHandler(currentFrameModel, obj);

                                if (readByteLength > appendBytes.Length)
                                {
                                    // 4、处理当前receiveBuffer中剩余字节数组,进入缓存
                                    resetFrameModelBasic();
                                    byte[] restBytes = new byte[receiveBuffer.Length - appendBytesCount];
                                    Buffer.BlockCopy(receiveBuffer, appendBytesCount, restBytes, 0, restBytes.Length);
                                    currentFrameModel.setRestBytes(restBytes);
                                    currentFrameModel.setStatus("4");
                                    if (restBytes.Length > 10)
                                    {
                                        // 尝试解析剩余字节
                                        parseBytes(restBytes.Length, restBytes, obj);
                                    }
                                }
                                else
                                {
                                    resetFrameModelAll();
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
                            parseBytes(readByteLength, sumBytes, obj);
                        }

                        Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                    }
                    //catch (ThreadAbortException e)
                    //{
                    //    // 重置线程状态为Abort
                    //    Thread.ResetAbort();
                    //}
                    catch (SocketException e)
                    {
                        // 判断socket连接状态，如果是非连接，则直接终止接收线程
                        try
                        {
                            isSocketConnected(socket);
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
                            return;
                        }

                    }
                    catch (Exception e)
                    {
                        if (e is SocketException)
                        {
                            // 设置主窗体连接状态
                            if (null != Main_Form.main_Form)
                            {
                                Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                                {
                                    Main_Form.main_Form.result_label.Text = "连接中......";
                                }));
                            }

                            // 设置登录窗体连接状态
                            Login_Register_Form login_Register_Form = (Login_Register_Form)obj;
                            login_Register_Form.connect_label.Invoke(new MethodInvoker(delegate ()
                            {
                                login_Register_Form.connect_label.Text = "连接中......";
                            }));
                        }

                        resetFrameModelAll();
                    }
                }
            }
            else
            {
                MessageBox.Show("客户端与服务端socket连接状态异常，请尝试关闭应用后重启");
            }
        }

        // 解析数据
        public static void parseBytes(int readByteLength, byte[] receiveBuffer, object obj)
        {
            // 解析字节总长度4B
            if (currentFrameModel.getSumLength() == 0)
            {
                int index = currentFrameModel.getIndex();
                int value = (int)((receiveBuffer[index] & 0xFF)
                    | ((receiveBuffer[index + 1] & 0xFF) << 8)
                    | ((receiveBuffer[index + 2] & 0xFF) << 16)
                    | ((receiveBuffer[index + 3] & 0xFF) << 24));

                currentFrameModel.setSumLength(IPAddress.NetworkToHostOrder(value) - 4);
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

            // 解析实际数据内容长度 4B
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

                    // 执行业务处理
                    receiveHandler(currentFrameModel, obj);

                    // 处理剩余部分字节
                    resetFrameModelBasic();
                    byte[] restBytes = new byte[receiveBuffer.Length - (10 + currentFrameModel.getOrigiDataBytes().Length)];
                    Buffer.BlockCopy(receiveBuffer, (10 + currentFrameModel.getOrigiDataBytes().Length), restBytes, 0, restBytes.Length);
                    currentFrameModel.setIndex(0);
                    currentFrameModel.setStatus("4"); // 存在剩余
                    currentFrameModel.setRestBytes(restBytes); // 记录剩余字节
                    if (restBytes.Length > 10)
                    {
                        // 尝试解析剩余字节
                        parseBytes(restBytes.Length, restBytes, obj);
                    }
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
        private static void receiveHandler(FrameModel perfectFrameModel, object obj)
        {
            if (!(perfectFrameModel.getStatus() == "3"))
            {
                return;
            }

            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.Append(Encoding.UTF8.GetString(perfectFrameModel.getOrigiDataBytes(), 0, perfectFrameModel.getOriginDataBytesLength())); // 获取帧数据
            perfectFrameModel.setData(stringBuilder.ToString());

            // 刷新用户列表帧或搜索用户帧，返回为数组json
            if (perfectFrameModel.getFrameType() == (byte)3 || perfectFrameModel.getFrameType() == (byte) 12)
            {
                List<UserModel> userModels = (List<UserModel>)JsonConvert.DeserializeObject(perfectFrameModel.getData(), typeof(List<UserModel>));
                dataHandler(perfectFrameModel.getFrameType(), NetResponse.of(NetResponse.Response.SUCCESS, userModels, "Success", ""), obj);
                return;
            }

            // 个人网盘帧文件夹操作
            if ((perfectFrameModel.getFrameType() == (byte)6) 
                || (perfectFrameModel.getFrameType() == (byte)7)
                || (perfectFrameModel.getFrameType() == (byte)8))
            {
                FileDto fileDto = JsonConvert.DeserializeObject<FileDto>(perfectFrameModel.getData());
                dataHandler(perfectFrameModel.getFrameType(), NetResponse.of(NetResponse.Response.SUCCESS, fileDto, "Success", ""), obj);
                return;
            }

            // 个人网盘文件删除操作
            if (perfectFrameModel.getFrameType() == (byte)11)
            {
                List<long> fileIdList = (List<long>)JsonConvert.DeserializeObject(perfectFrameModel.getData(), typeof(List<long>));
                dataHandler(perfectFrameModel.getFrameType(), NetResponse.of(NetResponse.Response.SUCCESS, fileIdList, "Success", ""), obj);
                return;
            }

            // 转换数据并追加；
            CommonRes commonRes = (CommonRes)JsonConvert.DeserializeObject(perfectFrameModel.getData(), typeof(CommonRes));
            dataHandler(perfectFrameModel.getFrameType(), NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), ""), obj);
        }

        // 业务数据处理
        public static void dataHandler(byte frameType, NetResponse netResponse, object obj)
        {
            if (frameType == (byte)0) // 0：登录帧响应
            {
                Login_Register_Form.loginDelegateHandler(obj, netResponse);

            }
            else if (frameType == (byte)1) // 1： 登出帧响应
            {
                ((Login_Register_Form)obj).Invoke(new MethodInvoker(delegate () { ((Login_Register_Form)obj).Show(); }));
            }
            else if (frameType == (byte)2) // 2： 文本帧响应
            {
                Main_Form.main_Form.message_richTextBox.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 来自用户 [ " + ((CommonRes)netResponse.getCommonRes()).getUserName() + " ] 数据: "
                    + ((CommonRes)netResponse.getCommonRes()).getMessage() + "\r\n");
                }));

                // 由于文件在线传输帧被服务端以聊天服务的文本帧发送，此处需要判断是否为在线文件传输帧
                FileService.isNeedToFileOnlineTransport(netResponse);

            }
            else if (frameType == (byte)3) // 返回请求帧为3的数据，刷新在线用户列表
            {
                List<UserModel> list = (List<UserModel>)netResponse.getCommonRes();
                Main_Form.main_Form.user_list_dataGridView.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.user_list_dataGridView.Rows.Clear();
                    for (int i = 0; i < list.Count; i++)
                    {
                        Main_Form.main_Form.user_list_dataGridView.Rows.Add();
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[0].Value = i + 1;
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[1].Value = list[i].getUserName();
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[2].Value = Utils.ToDateTime(list[i].getLastLoginDate());
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[3].Value = Utils.ToDateTime(list[i].getRegisterDate());
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[4].Value = (list[i].getStatus() == "1" ? "在线" : "未知状态");
                    }
                }));
            }
            else if (frameType == (byte)4) // 4: 注册帧响应
            {
                Register_Form.registerDelegateHandler(obj, netResponse);
            }
            else if (frameType == (byte)5) // 5: 心跳帧响应
            {
                Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                {
                    if ("HEART_RESPONSE".Equals(((CommonRes)netResponse.getCommonRes()).getMessage()))
                    {
                        Main_Form.main_Form.result_label.Text = "网络连接正常......";
                    }
                }));
            }
            else if (frameType == (byte)6) // 6: 个人网盘文件夹刷新
            {
                FileDto fileDto = (FileDto)netResponse.getCommonRes();
                // 设置当前文件夹下的文件数量
                Main_Form.main_Form.file_sum_count_label.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_sum_count_label.Text = fileDto.getFileCount().ToString();
                }));

                Main_Form.main_Form.personal_file_treeView.Invoke(new MethodInvoker(delegate ()
                {
                    // 判断当前节点是否为最顶层节点
                    if (fileDto.getPid() == -1)
                    {
                        // 清空所有节点
                        Main_Form.main_Form.personal_file_treeView.Nodes.Clear();
                        // 创建根节点
                        TreeNode rootTreeNode = Main_Form.main_Form.personal_file_treeView.Nodes.Add(fileDto.getFileName());
                        rootTreeNode.Tag = fileDto;

                        // 判断是否有子节点
                        if (null != fileDto.getChildFileList() && fileDto.getChildFileList().Count > 0)
                        {
                            List<FileDto> list = fileDto.getChildFileList();
                            // 创建子节点
                            for (int i = 0; i < list.Count; i++)
                            {
                                TreeNode childTreeNode = rootTreeNode.Nodes.Add(list[i].getFileName());
                                childTreeNode.Tag = list[i];
                            }
                        }
                    }
                    else
                    {
                        // 处理子文件夹还是处理包含的文件
                        if (fileDto.getHasChild() == "Y")
                        {
                            // 含有子文件夹则显示子文件夹  
                            appendFileNode(fileDto);
                        }
                        else
                        {
                            // 处理子文件集合
                            fileListHandle(fileDto);
                        }

                    }
                    Main_Form.main_Form.personal_file_treeView.ExpandAll();
                }));
            }
            else if (frameType == (byte)7) // 7: 个人网盘文件夹创建
            {
                FileDto fileDto = (FileDto)netResponse.getCommonRes();

                if (fileDto.getRepeatCreate() == "Y")
                {
                    File_Create_Form.file_Create_Form.create_description_label.Invoke(new MethodInvoker(delegate ()
                    {
                        File_Create_Form.file_Create_Form.create_description_label.Visible = true;
                        File_Create_Form.file_Create_Form.create_description_label.Text = "创建的文件夹已存在";
                    }));
                }
                else if (fileDto.getFileCount() > 0)
                {
                    File_Create_Form.file_Create_Form.create_description_label.Invoke(new MethodInvoker(delegate ()
                    {
                        File_Create_Form.file_Create_Form.create_description_label.Visible = true;
                        File_Create_Form.file_Create_Form.create_description_label.Text = "当前文件夹下已含有文件，无法创建文件夹";
                    }));
                }
                else
                {
                    File_Create_Form.file_Create_Form.create_description_label.Invoke(new MethodInvoker(delegate ()
                    {
                        File_Create_Form.file_Create_Form.create_description_label.Visible = false;
                    }));
                    File_Create_Form.file_Create_Form.Invoke(new MethodInvoker(delegate () { File_Create_Form.file_Create_Form.Close(); }));
                    // 创建成功刷新文件夹,从头开始刷新
                    UserModel userModel = new UserModel();
                    userModel.setRefreshFile("true");
                    userModel.setUserName(Main_Form.main_Form.commonRes.getUserName());
                    userModel.setFileName(Main_Form.main_Form.commonRes.getUserName());
                    userModel.setFilePath(Main_Form.main_Form.commonRes.getUserName());
                    userModel.setCurrentPage(1);
                    userModel.setPageSize(10);
                    NetServiceContext.sendMessageNotWaiting(6, JsonConvert.SerializeObject(userModel), obj);
                }
            }
            else if (frameType == (byte)8) // 个人网盘文件夹名称修改
            {
                File_Create_Form.file_Create_Form.create_description_label.Invoke(new MethodInvoker(delegate ()
                {
                    File_Create_Form.file_Create_Form.create_description_label.Visible = false;
                }));
                File_Create_Form.file_Create_Form.Invoke(new MethodInvoker(delegate () { File_Create_Form.file_Create_Form.Close(); }));

                FileDto fileDto = (FileDto)netResponse.getCommonRes();
                Main_Form.main_Form.personal_file_treeView.Invoke(new MethodInvoker(delegate ()
                {
                    // 根据当前节点fileDto判断处于文件夹树中哪一个节点下,追加相应节点
                    appendFileNode(fileDto);
                    Main_Form.main_Form.personal_file_treeView.ExpandAll();
                }));
            }
            else if (frameType == (byte)11)
            {
                List<long> list = (List<long>)netResponse.getCommonRes();
                Main_Form.main_Form.file_list_dataGridView.Invoke(new MethodInvoker(delegate ()
                {
                    int count = Main_Form.main_Form.file_list_dataGridView.Rows.Count;
                    if (count > 0)
                    {
                        // 循环遍历删除行记录
                        for (int i = 0; i < list.Count; i++)
                        {
                            for (int j = (count - 1); j >= 0; j--)
                            {
                                string tag = Main_Form.main_Form.file_list_dataGridView.Rows[j].Cells[9].Value.ToString();
                                if (tag == list[i].ToString())
                                {
                                    Main_Form.main_Form.file_list_dataGridView.Rows.RemoveAt(j);
                                    count = Main_Form.main_Form.file_list_dataGridView.Rows.Count;
                                }
                            }
                        }
                    }

                }));

                // 恢复button的权限
                Main_Form.main_Form.all_select_button.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.all_select_button.Enabled = true;
                }));

                Main_Form.main_Form.all_cancel_select_button.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.all_cancel_select_button.Enabled = true;
                }));

                Main_Form.main_Form.all_select_download_button.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.all_select_download_button.Enabled = true;
                }));

                Main_Form.main_Form.all_select_delete_button.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.all_select_delete_button.Enabled = true;
                }));

                Main_Form.main_Form.all_file_refresh_button.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.all_file_refresh_button.Enabled = true;
                }));
            }
            else if (frameType == (byte)12)
            {
                List<UserModel> list = (List<UserModel>)netResponse.getCommonRes();
                Main_Form.main_Form.user_list_dataGridView.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.user_list_dataGridView.Rows.Clear();
                    for (int i = 0; i < list.Count; i++)
                    {
                        Main_Form.main_Form.user_list_dataGridView.Rows.Add();
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[0].Value = i + 1;
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[1].Value = list[i].getUserName();
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[2].Value = Utils.ToDateTime(list[i].getLastLoginDate());
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[3].Value = Utils.ToDateTime(list[i].getRegisterDate());
                        Main_Form.main_Form.user_list_dataGridView.Rows[i].Cells[4].Value = (list[i].getStatus() == "1" ? "在线" : "未知状态");
                    }
                }));
            }
        }

        // 处理当前文件夹下的子文件夹
        public static void appendFileNode(FileDto fileDto)
        {
            // 遍历当前树中所有节点
            TreeNodeCollection treeNodes = Main_Form.main_Form.personal_file_treeView.Nodes;
            if (treeNodes.Count > 0)
            {
                foreach (TreeNode tn in treeNodes)
                {
                    bool result = recursionFileTreeNode(tn, fileDto);
                    if (result)
                    {
                        return;
                    }
                }
            }
        }

        // 处理当前文件夹下的子文件
        public static void fileListHandle(FileDto fileDto)
        {
            List<FileDto> fileDtoList = fileDto.getChildFileList();
            if (null != fileDtoList && fileDtoList.Count > 0)
            {
                // 设置文件表格
                Main_Form.main_Form.file_list_dataGridView.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.file_list_dataGridView.Rows.Clear();
                    int i = 0;
                    foreach (FileDto filedto in fileDtoList)
                    {
                        Main_Form.main_Form.file_list_dataGridView.Rows.Add();
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[1].Value = i + 1;
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[2].Value = filedto.getFileName();
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[3].Value = filedto.getFilePath();
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[4].Value = filedto.getFileSize();
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[5].Value = Utils.ToDateTime(filedto.getGmtCreate());
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[6].Value = "已上传";
                        Main_Form.main_Form.file_list_dataGridView.Rows[i].Cells[9].Value = filedto.getId().ToString();
                        i++;
                    }
                }));
            }
            else
            {
                Main_Form.currentPage = Main_Form.currentPage - 1;
                Main_Form.main_Form.file_list_dataGridView.Rows.Clear();
            }
        }

        public static bool recursionFileTreeNode(TreeNode treeNode, FileDto fileDto)
        {
            int currentNodeChildNodeCount = treeNode.Nodes.Count; // 当前节点下的子节点数量
            if (currentNodeChildNodeCount == 0) // == 0：表示当前节点下子节点为空，则直接追加节点
            {
                FileDto dto = (FileDto) treeNode.Tag;
                if (dto.getId() == fileDto.getId())
                {
                    // 如果当前节点子节点不为空，那就扩展字节点
                    if (null != fileDto.getChildFileList() && fileDto.getChildFileList().Count > 0)
                    {
                        List<FileDto> list = fileDto.getChildFileList();
                        // 创建子节点
                        for (int i = 0; i < list.Count; i++)
                        {
                            TreeNode childTreeNode = treeNode.Nodes.Add(list[i].getFileName());
                            childTreeNode.Tag = list[i];
                        }

                        treeNode.ExpandAll();
                    }

                    // 如果为修改了文件夹名称
                    if (dto.getFileName() != fileDto.getFileName())
                    {
                        dto.setFileName(fileDto.getFileName());
                        treeNode.ExpandAll();
                    }

                    return true;
                }

                return false;
            }
            else // > 0：表示当前节点下不为空，则直接追加节点
            {
                // 当前节点下的子节点不为空，尝试如果当前节点刚好等于所请求的节点信息，则无需遍历当前节点子节点
                FileDto dto = (FileDto)treeNode.Tag;
                if (dto.getId() == fileDto.getId())
                {
                    treeNode.Nodes.Clear();

                    if (null != fileDto.getChildFileList() && fileDto.getChildFileList().Count > 0)
                    {
                        List<FileDto> list = fileDto.getChildFileList();
                        // 创建子节点
                        for (int i = 0; i < list.Count; i++)
                        {
                            TreeNode childTreeNode = treeNode.Nodes.Add(list[i].getFileName());
                            childTreeNode.Tag = list[i];
                        }

                        treeNode.ExpandAll();
                    }

                    return true;
                }

                // 遍历子节点
                TreeNodeCollection treeNodes = treeNode.Nodes;
                foreach (TreeNode tn in treeNodes)
                {
                    bool result = recursionFileTreeNode(tn, fileDto);
                    if (result)
                    {
                        return result;
                    }
                }

                return false;
            }
        }

        public static void resetFrameModelBasic()
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

        public static void resetFrameModelAll()
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

        // 判断socket连接状态(发送0字节判断socket连接状态)
        public static bool isSocketConnected(Socket socket)
        {
            try
            {
                if (socket == null)
                {
                    return false;
                }
                byte[] tmp = new byte[1];
                if (null != socket)
                {
                    socket.Send(tmp, 0, 0); // 如果该段代码不会抛出异常，则为连接状态，否则不处于连接状态
                }
            }
            catch (SocketException e)
            {
                // 代码 10035也保证socket连接状态正常
                if (!e.NativeErrorCode.Equals(10035))
                {
                    return false;
                }
            }
            catch (System.ObjectDisposedException e)
            {
                return false;
            }

            return true;
        }


        // 关闭当前Socket
        public static void close()
        {
            if(socket == null)
            {
                return;
            }

            if (NetServiceContext.isSocketConnected(socket))
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }
}
