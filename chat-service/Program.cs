using chat_service.net;
using chat_service.service.file;
using chat_service.util;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace chat_service
{
    static class Program
    {

        // 定义个委托,委托开启远程连接
        public delegate NetResponse RemoteConntect();
        // 执行远程调用
        private static RemoteConntect rc = null;
        private static Login_Register_Form login_Register_Form = null;
        // 接收数据线程
        public static Thread receiveThread = null;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 1. 加载配置文件
            loadNetClient();

            // 2. 实例化登录窗口, 即应用启动的主窗口
            login_Register_Form = new Login_Register_Form();

            // 委托建立连接
            delegateCreateConnection();

            // 3. 开启登录窗口
            Application.Run(login_Register_Form);

            //Application.Run(new Test());

        }
        /// <summary>
        /// 加载配置文件
        /// </summary>
        private static void loadNetClient()
        {
            // 加载配置文件
            XmlConfigUtils.InitDoc();
            // 设置远程聊天服务地址
            NetServiceContext.remoteServiceAddress = XmlConfigUtils.GetValue("remoteServiceAddress");
            // 设置远程文件上传服务地址
            NetServiceContext.remoteFileServiceAddress = XmlConfigUtils.GetValue("remoteFileServiceAddress");
            // 设置远程文件下载服务地址
            NetServiceContext.remoteFileDownloadServiceAddress = XmlConfigUtils.GetValue("remoteFileDownloadServiceAddress");
            // 设置socket发送缓冲区大小
            NetServiceContext.socketSendBufferSize = Convert.ToInt32(XmlConfigUtils.GetValue("socketSendBufferSize"));
            // 设置socket接收缓冲区大小
            NetServiceContext.socketReceiveBufferSize = Convert.ToInt32(XmlConfigUtils.GetValue("socketReceiveBufferSize"));
            // 设置处理数据的缓冲大小
            NetServiceContext.bufferSize = Convert.ToInt32(XmlConfigUtils.GetValue("bufferSize"));
            // 设置文件读取缓冲大小
            NetServiceContext.fileOperateSize = Convert.ToInt32(XmlConfigUtils.GetValue("fileOperateSize"));
            // 文件默认下载路径
            NetServiceContext.globalDownloadPath = XmlConfigUtils.GetValue("globalDownloadPath");
        }

        /// <summary>
        /// 委托代理执行客户端连接创建，只需要一个构建一个socket连接即可，用于文本传输场景，对于文件类型操作会重新新建连接进行处理
        /// </summary>
        public static void delegateCreateConnection()
        {
            // 构建委托delegete对象，其中入参就是代理需要执行的逻辑
            rc = new RemoteConntect(beginRemoteConnect);
            AsyncCallback callback = new AsyncCallback(AsyncCallbackImpl);
            // rc代理对象执行异步代理逻辑，处理结果通过AsyncCallback异步进行结果回调
            rc.BeginInvoke(callback, null);
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        private static NetResponse beginRemoteConnect()
        {
            return NetServiceContext.chatInitSocketAndConnect();
        }

        /// <summary>
        /// 异步回调结果
        /// </summary>
        private static void AsyncCallbackImpl(IAsyncResult ar)
        {
            // 获取建立连接结果
            NetResponse netResponse = rc.EndInvoke(ar);
            if (null != netResponse)
            {
                
                if (netResponse.getResponse().Equals(NetResponse.Response.CONNECTION_SUCCESS))
                {
                    // 设置用户名用于下次登录默认代入
                    login_Register_Form.userName_textBox.Invoke(new MethodInvoker(delegate () { login_Register_Form.userName_textBox.Text = XmlConfigUtils.GetValue("userName");}));
                    // 设置密码用于下次登录默认代入
                    login_Register_Form.password_textBox.Invoke(new MethodInvoker(delegate () { login_Register_Form.password_textBox.Text = XmlConfigUtils.GetValue("password"); }));
                    // 设置连接信息，即socket连接结果信息
                    login_Register_Form.connect_label.Invoke(new MethodInvoker(delegate () { login_Register_Form.connect_label.Visible = true; login_Register_Form.connect_label.ForeColor = Color.Green; login_Register_Form.connect_label.Text = netResponse.getResult();}));
                    // 异步线程开始监听服务端回传数据，无限循环读取数据
                    receiveThread = new Thread(threadLoop);
                    receiveThread.IsBackground = true; // 设置为后台程序
                    receiveThread.Start();
                }
                else
                {
                    //if (null != Program.receiveThread)
                    //{
                    //    Program.receiveThread.Abort();
                    //    Program.receiveThread = null;
                    //}

                    login_Register_Form.connect_label.Invoke(new MethodInvoker(delegate () { login_Register_Form.connect_label.Visible = true; login_Register_Form.connect_label.ForeColor = Color.Red; login_Register_Form.connect_label.Text = netResponse.getError();}));
                }
            }

            //throw new NotImplementedException();
        }

        // 初始化线程
        private static void threadLoop()
        {
            NetServiceContext.receiveResponse(login_Register_Form);
        }
    }
}
