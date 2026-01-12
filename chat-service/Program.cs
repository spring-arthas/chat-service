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
            
            // 加载配置文件
            loadNetClient();

            // 实例化登录窗口
            login_Register_Form = new Login_Register_Form();

            // 委托建立连接
            delegateCreateConnection();

            // 开启登录窗口
            Application.Run(login_Register_Form);

            //Application.Run(new Test());

        }

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

        public static void delegateCreateConnection()
        {
            rc = new RemoteConntect(beginRemoteConnect);
            AsyncCallback callback = new AsyncCallback(AsyncCallbackImpl);
            rc.BeginInvoke(callback, null);
        }

        // 建立连接(只建立聊天连接)
        private static NetResponse beginRemoteConnect()
        {
            return NetServiceContext.chatInitSocketAndConnect();
        }

        // 异步回调结果
        private static void AsyncCallbackImpl(IAsyncResult ar)
        {
            // 获取建立连接结果
            NetResponse netResponse = rc.EndInvoke(ar);
            if (null != netResponse)
            {
                
                if (netResponse.getResponse().Equals(NetResponse.Response.CONNECTION_SUCCESS))
                {

                    // 设置用户名
                    login_Register_Form.userName_textBox.Invoke(new MethodInvoker(delegate () { login_Register_Form.userName_textBox.Text = XmlConfigUtils.GetValue("userName");}));
                    
                    // 设置密码
                    login_Register_Form.password_textBox.Invoke(new MethodInvoker(delegate () { login_Register_Form.password_textBox.Text = XmlConfigUtils.GetValue("password");}));

                    // 设置连接信息
                    login_Register_Form.connect_label.Invoke(new MethodInvoker(delegate () { login_Register_Form.connect_label.Visible = true; login_Register_Form.connect_label.ForeColor = Color.Green; login_Register_Form.connect_label.Text = netResponse.getResult();}));

                    // 异步线程开始监听数据
                    receiveThread = new Thread(threadLoop);
                    receiveThread.IsBackground = true;
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
            NetServiceContext.loopReceiveServiceData(login_Register_Form);
        }
    }
}
