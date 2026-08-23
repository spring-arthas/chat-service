using System;
using System.Collections.Generic;
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
    /// Socket 连接状态。
    /// </summary>
    public enum SocketConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Socket 管理器（单例）。
    /// 与 chat-storage 的 SocketManager 对应，负责：
    /// 1. 维护到主控服务端（默认 10086）的 TCP 长连接；
    /// 2. 帧的同步发送 / 接收；
    /// 3. 请求-响应映射（sendFrameAndWait，通过 Continuation 机制）；
    /// 4. 流式处理器（针对长流数据，如文件下载 dataFrame）。
    ///
    /// 说明：C# 客户端为 WinForms 原生实现，内部采用独立接收线程 + ManualResetEventSlim 完成
    /// 请求-响应等待，等价于 Swift 侧的 async/await Continuation 模型。
    /// </summary>
    public class SocketManager
    {
        private static readonly SocketManager _instance = new SocketManager();
        public static SocketManager Shared { get { return _instance; } }

        public SocketConnectionState ConnectionState { get; private set; }

        private string host = "";
        private int port = 0;

        private Socket socket;
        private Thread receiveThread;
        private volatile bool isReceiving = false;

        // 接收缓冲
        private byte[] receiveBuffer = new byte[64 * 1024];
        private List<byte> pendingBytes = new List<byte>();

        // 发送队列锁
        private readonly object sendLock = new object();

        // 请求-响应映射
        private readonly object waitersLock = new object();
        private readonly Dictionary<long, WaitingRequest> waiters = new Dictionary<long, WaitingRequest>();

        // 流式处理器：帧类型 -> 处理器闭包
        private readonly object streamLock = new object();
        private readonly Dictionary<FrameTypeEnum, List<Func<Frame, bool>>> streamHandlers =
            new Dictionary<FrameTypeEnum, List<Func<Frame, bool>>>();

        // 当前登录用户（用于文件传输 token）
        public UserDO CurrentUser { get; set; }
        public long CurrentUserId { get; set; }

        private long requestIdCounter = 0;

        // 连接/断开操作锁，避免并发 Connect 造成 socket 竞争
        private readonly object connectionLock = new object();

        private SocketManager() { }

        /// <summary>
        /// 连接主控服务端。
        /// </summary>
        public void Connect(string host, int port)
        {
            lock (connectionLock)
            {
                if (this.host == host && this.port == port && IsConnected)
                {
                    return; // 已连接同一目标，无需重复连接
                }

                Disconnect(false);
                this.host = host;
                this.port = port;

                ConnectionState = SocketConnectionState.Connecting;
                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(host, port);
                    socket.NoDelay = true;
                    ConnectionState = SocketConnectionState.Connected;
                    StartReceiveLoop();
                }
                catch (Exception)
                {
                    ConnectionState = SocketConnectionState.Error;
                    throw;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return socket != null && socket.Connected;
            }
        }

        public void Disconnect(bool notify = true)
        {
            lock (connectionLock)
            {
                StopReceiveLoop();
                try
                {
                    if (socket != null)
                    {
                        socket.Close();
                    }
                }
                catch { }
                socket = null;
            }

            // 唤醒所有等待者
            lock (waitersLock)
            {
                foreach (var kv in waiters.Values)
                {
                    try { kv.Signal.Set(); } catch { }
                }
                waiters.Clear();
            }

            if (notify)
            {
                ConnectionState = SocketConnectionState.Disconnected;
            }
        }

        // ============ 接收循环 ============

        private void StartReceiveLoop()
        {
            if (isReceiving) return;
            isReceiving = true;
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void StopReceiveLoop()
        {
            isReceiving = false;
            try
            {
                if (socket != null) socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(200);
            }
            catch { }
            receiveThread = null;
        }

        private void ReceiveLoop()
        {
            while (isReceiving)
            {
                try
                {
                    if (socket == null || !socket.Connected) break;
                    int read = socket.Receive(receiveBuffer);
                    if (read <= 0) break;

                    lock (pendingBytes)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            pendingBytes.Add(receiveBuffer[i]);
                        }
                    }

                    ProcessPendingFrames();
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    break;
                }
            }

            isReceiving = false;
            // 连接断开，唤醒等待者
            lock (waitersLock)
            {
                foreach (var kv in waiters.Values)
                {
                    try { kv.Signal.Set(); } catch { }
                }
                waiters.Clear();
            }
        }

        private void ProcessPendingFrames()
        {
            while (true)
            {
                byte[] buffer;
                int available;
                lock (pendingBytes)
                {
                    buffer = pendingBytes.ToArray();
                    available = buffer.Length;
                }

                var result = FrameParser.ExtractFrame(buffer, 0, available);
                if (result == null) return; // 无完整帧

                if (result.Item1 == null)
                {
                    // 魔数不匹配，丢弃 1 字节重新对齐
                    lock (pendingBytes)
                    {
                        if (pendingBytes.Count > 0) pendingBytes.RemoveAt(0);
                    }
                    continue;
                }

                // 从缓冲区移除已消费的字节
                lock (pendingBytes)
                {
                    if (pendingBytes.Count >= result.Item2)
                    {
                        pendingBytes.RemoveRange(0, result.Item2);
                    }
                    else
                    {
                        pendingBytes.Clear();
                    }
                }

                HandleReceivedFrame(result.Item1);
            }
        }

        private void HandleReceivedFrame(Frame frame)
        {
            // 1. 优先匹配请求-响应等待者
            WaitingRequest matched = null;
            lock (waitersLock)
            {
                foreach (var kv in waiters)
                {
                    if (kv.Value.Matches(frame))
                    {
                        matched = kv.Value;
                        break;
                    }
                }
                if (matched != null)
                {
                    waiters.Remove(matched.RequestId);
                }
            }

            if (matched != null)
            {
                matched.Frame = frame;
                try { matched.Signal.Set(); } catch { }
                return;
            }

            // 2. 流式处理器
            List<Func<Frame, bool>> handlers = null;
            lock (streamLock)
            {
                if (streamHandlers.ContainsKey(frame.Type))
                {
                    handlers = new List<Func<Frame, bool>>(streamHandlers[frame.Type]);
                }
            }
            if (handlers != null)
            {
                List<Func<Frame, bool>> toRemove = new List<Func<Frame, bool>>();
                foreach (var h in handlers)
                {
                    bool keep = false;
                    try { keep = h(frame); } catch { }
                    if (!keep) toRemove.Add(h);
                }
                if (toRemove.Count > 0)
                {
                    lock (streamLock)
                    {
                        if (streamHandlers.ContainsKey(frame.Type))
                        {
                            foreach (var h in toRemove)
                            {
                                streamHandlers[frame.Type].Remove(h);
                            }
                        }
                    }
                }
                return;
            }

            // 3. 未匹配帧（忽略，仅记录）
        }

        // ============ 发送 ============

        /// <summary>同步发送帧（基于锁保证发送完整性）。</summary>
        public void SendFrame(Frame frame)
        {
            if (!IsConnected) throw new InvalidOperationException("Socket 未连接");

            byte[] data = frame.ToBytes();
            lock (sendLock)
            {
                int totalSent = 0;
                while (totalSent < data.Length)
                {
                    int sent = socket.Send(data, totalSent, data.Length - totalSent, SocketFlags.None);
                    if (sent <= 0) throw new SocketException(10054); // connection reset
                    totalSent += sent;
                }
            }
        }

        /// <summary>
        /// 发送帧并等待响应（阻塞式请求-响应）。
        /// </summary>
        /// <param name="frame">请求帧</param>
        /// <param name="expectingTypes">期望的响应类型集合</param>
        /// <param name="timeoutMs">超时毫秒数</param>
        /// <param name="matcher">可选的匹配器（如按 taskId 匹配），null 表示仅按类型匹配</param>
        public Frame SendFrameAndWait(Frame frame, IEnumerable<FrameTypeEnum> expectingTypes, int timeoutMs = 10000, Func<Frame, bool> matcher = null)
        {
            var types = new HashSet<FrameTypeEnum>(expectingTypes);

            // 生成请求 ID 并注册等待者
            long id;
            WaitingRequest waiting;
            lock (waitersLock)
            {
                id = ++requestIdCounter;
                waiting = new WaitingRequest(id, types, matcher);
                waiters[id] = waiting;
            }

            try
            {
                // 发送帧
                SendFrame(frame);

                // 等待信号
                if (!waiting.Signal.Wait(timeoutMs))
                {
                    lock (waitersLock)
                    {
                        waiters.Remove(id);
                    }
                    throw new TimeoutException(string.Format("等待响应超时: {0}", frame.Type.Describe()));
                }

                if (waiting.Frame == null)
                {
                    throw new SocketException(10054); // 连接已关闭
                }
                return waiting.Frame;
            }
            catch
            {
                lock (waitersLock)
                {
                    waiters.Remove(id);
                }
                throw;
            }
        }

        public Frame SendFrameAndWait(Frame frame, FrameTypeEnum expectingType, int timeoutMs = 10000, Func<Frame, bool> matcher = null)
        {
            return SendFrameAndWait(frame, new[] { expectingType }, timeoutMs, matcher);
        }

        // ============ 流式处理器 ============

        /// <summary>
        /// 注册流式处理器，返回取消标记（本实现返回处理器引用）。
        /// </summary>
        public Func<Frame, bool> RegisterStreamHandler(FrameTypeEnum type, Func<Frame, bool> handler)
        {
            lock (streamLock)
            {
                if (!streamHandlers.ContainsKey(type))
                {
                    streamHandlers[type] = new List<Func<Frame, bool>>();
                }
                streamHandlers[type].Add(handler);
            }
            return handler;
        }

        public void RegisterStreamHandler(IEnumerable<FrameTypeEnum> types, Func<Frame, bool> handler)
        {
            lock (streamLock)
            {
                foreach (var t in types)
                {
                    if (!streamHandlers.ContainsKey(t))
                    {
                        streamHandlers[t] = new List<Func<Frame, bool>>();
                    }
                    streamHandlers[t].Add(handler);
                }
            }
        }

        public void UnregisterStreamHandler(Func<Frame, bool> handler)
        {
            lock (streamLock)
            {
                foreach (var kv in streamHandlers)
                {
                    kv.Value.Remove(handler);
                }
            }
        }

        // ============ 附带便捷解析方法 ============

        /// <summary>解析标准响应（code==200 视为成功）。</summary>
        public static bool ParseStandardResponse(Frame frame)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(frame.GetDataAsString());
                if (dict != null)
                {
                    if (dict.ContainsKey("code"))
                    {
                        object codeObj = dict["code"];
                        int code = Convert.ToInt32(codeObj);
                        return code == 200;
                    }
                    if (dict.ContainsKey("success"))
                    {
                        return Convert.ToBoolean(dict["success"]);
                    }
                }
                return true; // 无 code 字段时视为成功
            }
            catch
            {
                return false;
            }
        }

        /// <summary>解析为 ResponseWrapper。</summary>
        public static ResponseWrapper ParseResponseWrapper(Frame frame)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(frame.GetDataAsString());
            var wrapper = new ResponseWrapper();
            if (dict == null) return wrapper;

            if (dict.ContainsKey("success")) wrapper.Success = Convert.ToBoolean(dict["success"]);
            if (dict.ContainsKey("code")) wrapper.Code = Convert.ToInt32(dict["code"]);
            if (dict.ContainsKey("msg")) wrapper.Message = Convert.ToString(dict["msg"]);
            else if (dict.ContainsKey("message")) wrapper.Message = Convert.ToString(dict["message"]);
            if (dict.ContainsKey("data")) wrapper.Data = dict["data"];

            return wrapper;
        }

        /// <summary>从响应包装器中抽取指定类型的 Data。</summary>
        public static T ParseData<T>(Frame frame)
        {
            var wrapper = ParseResponseWrapper(frame);
            if (wrapper.Data != null)
            {
                string dataJson = JsonConvert.SerializeObject(wrapper.Data);
                return JsonConvert.DeserializeObject<T>(dataJson);
            }
            // 兼容直接返回数据而非包装的情况
            return JsonConvert.DeserializeObject<T>(frame.GetDataAsString());
        }

        // ============ 内部等待请求类 ============

        private class WaitingRequest
        {
            public long RequestId { get; private set; }
            public HashSet<FrameTypeEnum> ExpectingTypes { get; private set; }
            public Func<Frame, bool> Matcher { get; private set; }
            public ManualResetEventSlim Signal { get; private set; }
            public Frame Frame { get; set; }

            public WaitingRequest(long id, HashSet<FrameTypeEnum> types, Func<Frame, bool> matcher)
            {
                RequestId = id;
                ExpectingTypes = types;
                Matcher = matcher;
                Signal = new ManualResetEventSlim(false);
            }

            public bool Matches(Frame frame)
            {
                if (!ExpectingTypes.Contains(frame.Type)) return false;
                if (Matcher != null)
                {
                    try { return Matcher(frame); }
                    catch { return false; }
                }
                return true;
            }
        }
    }
}
