using System;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;    
using chat_service.frame;
using chat_service.net;
using chat_service.util;
namespace chat_service
{
    public partial class Login_Register_Form : Form
    {
        // 登录结果处理委托
        public delegate Main_Form LoginHandler(NetResponse netResponse);

        // 委托方法
        public Main_Form Login(NetResponse netResponse)
        {
            if (!netResponse.getResponse().Equals(NetResponse.Response.SUCCESS))
            {
                MessageBox.Show(netResponse.getError());
                return null;
            }
            else
            {
                CommonRes commonRes = (CommonRes)netResponse.getCommonRes();
                if (commonRes != null && "200".Equals(commonRes.getCode()))
                {
                    this.Invoke(new MethodInvoker(delegate () { this.Hide(); }));
                    // 打开主窗口
                    return new Main_Form(netResponse.getCommonRes());
                }
                else
                {
                    if (commonRes != null)
                    {
                        MessageBox.Show(commonRes.getMessage());
                    }
                    return null;
                }
            }
        }
        /// <summary>
        /// 登录注册窗体
        /// </summary>
        public Login_Register_Form()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 登录系统
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void login_button_Click(object sender, EventArgs e)
        {
            this.executeLogin();
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void register_button_Click(object sender, EventArgs e)
        {
            Register_Form regiterFrom = new Register_Form();
            regiterFrom.ShowDialog();
        }

        /// <summary>
        /// 设置IP地址，用于切换新的服务端地址进行连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setting_label_Click(object sender, EventArgs e)
        {
            Setting_Form setting_Form = new Setting_Form();
            setting_Form.GetForm(this);
            setting_Form.ShowDialog();
        }

        // --> *********************************************** 委托调用 ********************************************

        // 登录代理调用
        public static void loginDelegateHandler(object obj, NetResponse netResponse)
        {
            Login_Register_Form login_Register_Form = (Login_Register_Form)obj;
            LoginHandler loginHandler = new LoginHandler(login_Register_Form.Login);
            loginHandler.BeginInvoke(netResponse, new AsyncCallback(loginAsyncHandler), null);
        }
        /// <summary>
        /// 登录成功后回调
        /// </summary>
        /// <param name="result"></param>
        public static void loginAsyncHandler(IAsyncResult result)
        {
            // 打开主窗口
            LoginHandler loginHandler = (LoginHandler)((AsyncResult)result).AsyncDelegate;
            Main_Form main_Form = loginHandler.EndInvoke(result);
            if (main_Form != null)
            {
                main_Form.ShowDialog();
            }
        }
        /// <summary>
        /// 用户名textBox触发回车
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void userName_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)//如果输入的是回车键  
            {
                this.executeLogin();
            }
        }
        /// <summary>
        /// 密码textBox触发回车
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void password_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)//如果输入的是回车键  
            {
                this.executeLogin();
            }
        }
        /// <summary>
        /// 执行登录逻辑
        /// </summary>
        private void executeLogin()
        {
            if (this.userName_textBox.Text == "")
            {
                MessageBox.Show("用户名不能为空");
                return;
            }
            if (this.password_textBox.Text == "")
            {
                MessageBox.Show("密码不能为空");
                return;
            }

            // 1、用户名密码写入配置文件，方便下次进行登录
            XmlConfigUtils.UpdateConfig("userName", userName_textBox.Text);
            XmlConfigUtils.UpdateConfig("password", password_textBox.Text);
            // 2、远程连接以及注册用户 (同步等待响应)
            // 注意：此处会阻塞UI线程直到超时或响应返回
            NetResponse response = NetServiceContext.login(userName_textBox.Text, password_textBox.Text);
            // 3、直接处理登录结果
            Main_Form mainForm = this.Login(response);
            if (mainForm != null)
            {
                mainForm.ShowDialog();
            }
        }
        // 登录窗体关闭，发送关闭socket消息
        private void Login_Register_Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 关闭所有socket连接
            NetServiceContext.close();
        }
    }
}
