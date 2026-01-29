using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
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

using Newtonsoft.Json.Linq;

namespace chat_service.util
{
    public class NetServiceContext
    {
        private static readonly byte[] MAGIC = { 0xFA, 0xCE };
        private const int HEADER_LENGTH = 8;
        // 用户帧类型
        public const byte USER_REGISTER_REQ = 0x30;
        public const byte USER_LOGIN_REQ = 0x31;
        public const byte USER_CHANGE_PWD_REQ = 0x32;
        public const byte USER_LOGOUT_REQ = 0x33;
        public const byte USER_RESPONSE = 0x34;
        // 文件帧类型
        public const byte DIR_CREATE_REQ = 0x10;
        public const byte DIR_DELETE_REQ = 0x11;
        public const byte DIR_UPDATE_REQ = 0x12;
        public const byte DIR_MOVE_REQ = 0x13;
        public const byte DIR_RESPONSE = 0x14;
        // 文件操作帧类型
        public const byte FILE_LIST_REQ = 0x40;
        public const byte FILE_DETAIL_REQ = 0x41;
        public const byte FILE_DELETE_REQ = 0x42;
        public const byte FILE_RESPONSE = 0x43;
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
        // socket建立事件
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        // socket接收事件
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        // socket接收事件
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // ==================== 同步请求上下文 ====================
        private class RequestWaitState
        {
            public ManualResetEvent WaitEvent { get; } = new ManualResetEvent(false);
            public NetResponse Response { get; set; }
        }

        // Key: Expected Response Frame Type, Value: Wait State
        private static ConcurrentDictionary<byte, RequestWaitState> _pendingRequests = new ConcurrentDictionary<byte, RequestWaitState>();

        // 发送并等待响应
        private static NetResponse sendFrameWait(byte requestFrameType, string jsonData, byte expectedResponseType, int timeoutMillis = 10000)
        {
            RequestWaitState waitState = new RequestWaitState();

            // 注册监听
            // 注意：如果多个线程等待同一个响应类型，这里可能会有问题。
            // 假设同一时间只有一个特定类型的请求在进行（通常是一个用户操作对应一个响应），
            // 或者通过更复杂的Key (FrameType) 来区分。
            // 目前简单实现：假设FrameType能唯一对应Pending Request。
            _pendingRequests[expectedResponseType] = waitState;

            try
            {
                // 发送数据
                sendFrame(requestFrameType, jsonData);

                // 等待信号
                if (waitState.WaitEvent.WaitOne(timeoutMillis))
                {
                    // 收到信号
                    return waitState.Response;
                }
                else
                {
                    // 超时
                    return NetResponse.ofFail("请求超时，服务器未响应");
                }
            }
            catch (Exception ex)
            {
                return NetResponse.ofFail("请求异常: " + ex.Message);
            }
            finally
            {
                // 清理
                RequestWaitState removed;
                _pendingRequests.TryRemove(expectedResponseType, out removed);
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
        // 建立聊天连接
        public static NetResponse chatInitSocketAndConnect()
        {
            // 初始化socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 10));
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

        /// <summary>
        /// 客户端登录
        /// </summary>
        /// <summary>
        /// 客户端登录 (同步)
        /// </summary>
        public static NetResponse login(string userName, string password)
        {
            JObject request = new JObject();
            request["userName"] = userName;
            request["password"] = password;
            Console.WriteLine("发送登录请求: " + request.ToString(Formatting.None));
            // 0x31 -> Expect 0x34 (USER_RESPONSE)
            return sendFrameWait(USER_LOGIN_REQ, request.ToString(Formatting.None), USER_RESPONSE);
        }
        /// <summary>
        /// 客户端注册 (同步)
        /// </summary>
        public static NetResponse register(UserModel userModel)
        {
            JObject request = new JObject();
            request["userName"] = userModel.getUserName();
            request["password"] = userModel.getPassword();
            request["phone"] = userModel.getPhone();
            request["mail"] = userModel.getMail();
            Console.WriteLine("发送注册请求: " + request.ToString(Formatting.None));
            // 0x30 -> Expect 0x04 (Assuming Type 4 is Register Response based on handlers) or USER_RESPONSE?
            // Looking at receiveResponse: frameType == 4 is Register Response (Register_Form.registerDelegateHandler)
            return sendFrameWait(USER_REGISTER_REQ, request.ToString(Formatting.None), (byte)4);
        }

        private static void sendFrame(byte frameType, string jsonData)
        {
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            byte[] buffer = new byte[HEADER_LENGTH + data.Length];

            buffer[0] = MAGIC[0];
            buffer[1] = MAGIC[1];
            buffer[2] = frameType;
            i buffer[3] = 0; // flags
            // buffer.putInt(data.length); (Big Endian in Java)
            byte[] lenBytes = BitConverter.GetBytes(data.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lenBytes);
            }
            Array.Copy(lenBytes, 0, buffer, 4, 4);
            Array.Copy(data, 0, buffer, 8, data.Length);
            int sent = 0;
            while (sent < buffer.Length)
            {
                sent += socket.Send(buffer, sent, buffer.Length - sent, SocketFlags.None);
            }
        }

        // ==================== 用户操作方法 ====================

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <summary>
        /// 修改密码
        /// </summary>
        public static NetResponse changePassword(string oldPassword, string newPassword)
        {
            JObject request = new JObject();
            request["oldPassword"] = oldPassword;
            request["newPassword"] = newPassword;
            Console.WriteLine("发送修改密码请求: " + request.ToString(Formatting.None));
            return sendFrameWait(USER_CHANGE_PWD_REQ, request.ToString(Formatting.None), USER_RESPONSE);
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        /// <summary>
        /// 退出登录
        /// </summary>
        public static NetResponse logout()
        {
            JObject request = new JObject(); // 空JSON对象
            Console.WriteLine("发送退出登录请求");
            // 0x33 (Logout) -> Expect 0x01 (Logout Response based on handlers) or USER_RESPONSE?
            // receiveResponse handler: frameType == 1 is Logout Response
            return sendFrameWait(USER_LOGOUT_REQ, request.ToString(Formatting.None), (byte)1);
        }

        // ==================== 目录操作方法 ====================

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <summary>
        /// 创建目录
        /// </summary>
        public static NetResponse createDirectory(long parentId, string dirName)
        {
            JObject request = new JObject();
            request["parentId"] = parentId;
            request["dirName"] = dirName;
            Console.WriteLine("发送创建目录请求: " + request.ToString(Formatting.None));
            // 0x10 (Dir Create) -> Expect 0x07 (Dir Create Resp based on handler) OR DIR_RESPONSE (0x14)?
            // receiveResponse says: frameType == (byte)7 (个人网盘文件夹创建)
            return sendFrameWait(DIR_CREATE_REQ, request.ToString(Formatting.None), (byte)7);
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <summary>
        /// 删除目录
        /// </summary>
        public static NetResponse deleteDirectory(long dirId)
        {
            JObject request = new JObject();
            request["dirId"] = dirId;
            Console.WriteLine("发送删除目录请求: " + request.ToString(Formatting.None));
            // 0x11 (Dir Delete) -> Expect 0x06 (Dir Refresh/Response) or DIR_RESPONSE?
            // Usually delete triggers a refresh list (Frame 6 or DIR_RESPONSE)
            return sendFrameWait(DIR_DELETE_REQ, request.ToString(Formatting.None), DIR_RESPONSE);
        }

        /// <summary>
        /// 更新目录
        /// </summary>
        /// <summary>
        /// 更新目录
        /// </summary>
        public static NetResponse updateDirectory(long dirId, string newName)
        {
            JObject request = new JObject();
            request["dirId"] = dirId;
            request["newName"] = newName;
            Console.WriteLine("发送更新目录请求: " + request.ToString(Formatting.None));
            // 0x12 -> receiveResponse: frameType == 8 (文件夹名称修改)
            return sendFrameWait(DIR_UPDATE_REQ, request.ToString(Formatting.None), (byte)8);
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <summary>
        /// 移动目录
        /// </summary>
        public static NetResponse moveDirectory(long dirId, long targetParentId)
        {
            JObject request = new JObject();
            request["dirId"] = dirId;
            request["targetParentId"] = targetParentId;
            Console.WriteLine("发送移动目录请求: " + request.ToString(Formatting.None));
            // 0x13 -> Expect DIR_RESPONSE (0x14) or Refresh (0x06)?
            // Assuming DIR_RESPONSE or 0x06. Let's assume DIR_RESPONSE for now as structure update.
            return sendFrameWait(DIR_MOVE_REQ, request.ToString(Formatting.None), DIR_RESPONSE);
        }

        // ==================== 文件操作方法 ====================

        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <summary>
        /// 获取文件列表
        /// </summary>
        public static NetResponse getFileList(long dirId, int pageNum = 1, int pageSize = 10)
        {
            JObject request = new JObject();
            request["dirId"] = dirId;
            request["pageNum"] = pageNum;
            request["pageSize"] = pageSize;
            Console.WriteLine("发送获取文件列表请求: " + request.ToString(Formatting.None));
            // 0x40 -> Expect FILE_RESPONSE (0x43) or 0x06?
            // Usually File List returns Frame 6 (FileDto)
            return sendFrameWait(FILE_LIST_REQ, request.ToString(Formatting.None), (byte)6);
        }

        /// <summary>
        /// 获取文件详情
        /// </summary>
        /// <summary>
        /// 获取文件详情
        /// </summary>
        public static NetResponse getFileDetail(long fileId)
        {
            JObject request = new JObject();
            request["fileId"] = fileId;
            Console.WriteLine("发送获取文件详情请求: " + request.ToString(Formatting.None));
            // 0x41 -> Expect FILE_RESPONSE
            return sendFrameWait(FILE_DETAIL_REQ, request.ToString(Formatting.None), FILE_RESPONSE);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <summary>
        /// 删除文件
        /// </summary>
        public static NetResponse deleteFile(long fileId)
        {
            JObject request = new JObject();
            request["fileId"] = fileId;
            Console.WriteLine("发送删除文件请求: " + request.ToString(Formatting.None));
            // 0x42 -> Expect 0x0B (11) ?
            // receiveResponse: frameType == 11 is delete success/list update
            return sendFrameWait(FILE_DELETE_REQ, request.ToString(Formatting.None), (byte)11);
        }


        public class NetFrame
        {
            public byte FrameType { get; set; }
            public byte[] Data { get; set; }
        }

        // Deprecated readNextFrame removed as logic is inlined into receiveResponse

        // Main receiving loop
        public static void receiveResponse(object obj)
        {
            while (true)
            {
                // 1. Connection Check & Reconnect
                if (socket == null || !socket.Connected)
                {
                    try
                    {
                        // Update UI: Reconnecting
                        updateConnectionStatus(obj, "正在尝试重连服务器...", Color.Red);

                        if (socket != null)
                        {
                            try { socket.Close(); } catch { }
                            socket = null;
                        }

                        // Parse address again or use cached
                        string[] address = remoteServiceAddress.Split(':');
                        IPAddress ip = IPAddress.Parse(address[0]);
                        int port = Convert.ToInt32(address[1]);
                        IPEndPoint remoteEP = new IPEndPoint(ip, port);

                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 10));

                        // Synchronous connect
                        socket.Connect(remoteEP);

                        if (socket.Connected)
                        {
                            updateConnectionStatus(obj, "网络连接正常......", Color.Green);
                        }
                    }
                    catch (Exception)
                    {
                        // Connect failed, wait and retry
                        Thread.Sleep(3000);
                        continue;
                    }
                }

                try
                {
                    // 2. Read Header (8 bytes)
                    byte[] header = new byte[HEADER_LENGTH];
                    int totalHeaderReceived = 0;
                    while (totalHeaderReceived < HEADER_LENGTH)
                    {
                        int received = socket.Receive(header, totalHeaderReceived, HEADER_LENGTH - totalHeaderReceived, SocketFlags.None);
                        if (received == 0)
                        {
                            throw new SocketException((int)SocketError.ConnectionReset);
                        }
                        totalHeaderReceived += received;
                    }

                    // 3. Validate Magic
                    if (header[0] != MAGIC[0] || header[1] != MAGIC[1])
                    {
                        throw new Exception("无效的协议头 (Magic Mismatch)");
                    }

                    // 4. Get Frame Type
                    byte frameType = header[2];

                    // 5. Parse Length
                    byte[] lenBytes = new byte[4];
                    Array.Copy(header, 4, lenBytes, 0, 4);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(lenBytes);
                    }
                    int dataLength = BitConverter.ToInt32(lenBytes, 0);

                    // Sanity check
                    if (dataLength < 0 || dataLength > 50 * 1024 * 1024)
                    {
                        throw new Exception($"非法的数据长度: {dataLength}");
                    }

                    // 6. Read Data
                    byte[] data = new byte[dataLength];
                    int totalDataReceived = 0;
                    while (totalDataReceived < dataLength)
                    {
                        int received = socket.Receive(data, totalDataReceived, dataLength - totalDataReceived, SocketFlags.None);
                        if (received == 0)
                        {
                            throw new SocketException((int)SocketError.ConnectionReset);
                        }
                        totalDataReceived += received;
                    }

                    // 7. Dispatch
                    string json = Encoding.UTF8.GetString(data);

                    // --- 同步响应拦截处理 ---
                    if (_pendingRequests.ContainsKey(frameType))
                    {
                        RequestWaitState waitState;
                        if (_pendingRequests.TryGetValue(frameType, out waitState))
                        {
                            // 构造反序列化对象，复用下方的逻辑思路，但直接在这里处理
                            try
                            {
                                NetResponse response = null;
                                if (frameType == USER_RESPONSE)
                                {
                                    CommonRes commonRes = JsonConvert.DeserializeObject<CommonRes>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), "");
                                }
                                else if (frameType == DIR_RESPONSE)
                                {
                                    FileDto fileDto = JsonConvert.DeserializeObject<FileDto>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, fileDto, "Success", "");
                                }
                                else if (frameType == (byte)3 || frameType == (byte)12) // User List
                                {
                                    List<UserModel> userModels = JsonConvert.DeserializeObject<List<UserModel>>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, userModels, "Success", "");
                                }
                                else if (frameType == 6 || frameType == 7 || frameType == 8) // File/Dir ops
                                {
                                    FileDto fileDto = JsonConvert.DeserializeObject<FileDto>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, fileDto, "Success", "");
                                }
                                else if (frameType == 11) // File list delete?
                                {
                                    List<long> fileIdList = JsonConvert.DeserializeObject<List<long>>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, fileIdList, "Success", "");
                                }
                                else if (frameType == 4) // Register
                                {
                                    CommonRes commonRes = JsonConvert.DeserializeObject<CommonRes>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), "");
                                }
                                else
                                {
                                    CommonRes commonRes = JsonConvert.DeserializeObject<CommonRes>(json);
                                    response = NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), "");
                                }

                                waitState.Response = response;
                                waitState.WaitEvent.Set();
                                // 拦截成功，跳过后续异步Handler
                                continue;
                            }
                            catch (Exception ex)
                            {
                                waitState.Response = NetResponse.ofFail("解析响应异常: " + ex.Message);
                                waitState.WaitEvent.Set();
                                continue;
                            }
                        }
                    }
                    // -----------------------

                    try
                    {
                        // Use Invoke if needed for simple handlers, but dataHandler usually handles Invoke internaly
                        if (frameType == USER_RESPONSE)
                        {
                            CommonRes commonRes = JsonConvert.DeserializeObject<CommonRes>(json);
                            dataHandler(frameType, NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), ""), obj);
                        }
                        else if (frameType == DIR_RESPONSE)
                        {
                            FileDto fileDto = JsonConvert.DeserializeObject<FileDto>(json);
                            dataHandler(frameType, NetResponse.of(NetResponse.Response.SUCCESS, fileDto, "Success", ""), obj);
                        }
                        else if (frameType == (byte)3 || frameType == (byte)12)
                        {
                            List<UserModel> userModels = JsonConvert.DeserializeObject<List<UserModel>>(json);
                            dataHandler(frameType, NetResponse.of(NetResponse.Response.SUCCESS, userModels, "Success", ""), obj);
                        }
                        else if (frameType == 6 || frameType == 7 || frameType == 8)
                        {
                            FileDto fileDto = JsonConvert.DeserializeObject<FileDto>(json);
                            dataHandler(frameType, NetResponse.of(NetResponse.Response.SUCCESS, fileDto, "Success", ""), obj);
                        }
                        else if (frameType == 11)
                        {
                            List<long> fileIdList = JsonConvert.DeserializeObject<List<long>>(json);
                            dataHandler(frameType, NetResponse.of(NetResponse.Response.SUCCESS, fileIdList, "Success", ""), obj);
                        }
                        else
                        {
                            CommonRes commonRes = JsonConvert.DeserializeObject<CommonRes>(json);
                            dataHandler(frameType, NetResponse.of(NetResponse.Response.SUCCESS, commonRes, commonRes.getMessage(), ""), obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Data Handler Logic Error: " + ex.Message);
                    }

                }
                catch (Exception)
                {
                    // Socket error or logic error, close socket to trigger reconnect
                    try { if (socket != null) socket.Close(); } catch { }
                    // Update UI immediately
                    updateConnectionStatus(obj, "连接异常，正在重连...", Color.Red);
                }
            }
        }

        private static void updateConnectionStatus(object obj, string msg, Color color)
        {
            if (Main_Form.main_Form != null && !Main_Form.main_Form.IsDisposed)
            {
                Main_Form.main_Form.result_label.Invoke(new MethodInvoker(delegate ()
                {
                    Main_Form.main_Form.result_label.Text = msg;
                    Main_Form.main_Form.result_label.ForeColor = color;
                }));
            }
            else if (obj is Login_Register_Form loginForm && !loginForm.IsDisposed)
            {
                loginForm.connect_label.Invoke(new MethodInvoker(delegate ()
                {
                    loginForm.connect_label.Text = msg;
                    loginForm.connect_label.ForeColor = color;
                }));
            }
        }

        /// <summary>
        /// 业务数据处理
        /// </summary>
        /// <param name="frameType"></param>
        /// <param name="netResponse"></param>
        /// <param name="obj"></param>
        public static void dataHandler(byte frameType, NetResponse netResponse, object obj)
        {
            if (frameType == USER_RESPONSE)) // 用户响应帧处理
            {
                // 登录
                // 注册
                // 修改密码
                // 退出登录
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
            else if (frameType == USER_RESPONSE) // 0x34
            {
                // Fallback attempt: check content or just treat as logic success.
                // Assuming it routes to Login or generic handler.
                // If it's Login Response:
                Login_Register_Form.loginDelegateHandler(obj, netResponse);
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
            else if (frameType == (byte)6 || frameType == DIR_RESPONSE) // 6: 个人网盘文件夹刷新 OR DIR_RESPONSE (if they share structure)
            {
                FileDto fileDto = (FileDto)netResponse.getCommonRes();
                // 设置当前文件夹下的文件数量
                Main_Form.main_Form.file_sum_count_label.Invoke(new MethodInvoker(delegate ()
                {
                    if (fileDto != null)
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


















































        // 实例化文件发送socket
        public static Socket getSendFileSocket()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 10));
            return socket;
        }

        // 实例化文件接收socket
        public static Socket getReceiveFileSocket()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 10));
            return socket;
        }







        /// <summary>
        /// 初始化文件发送socket并连接到服务器
        /// </summary>
        /// <param name="fileSendSocket"></param>
        /// <param name="taskType"></param>
        /// <returns></returns>
        public static NetResponse initSocketAndConnect(Socket fileSendSocket, string taskType)
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
                FileDto dto = (FileDto)treeNode.Tag;
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
            if (socket == null)
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
