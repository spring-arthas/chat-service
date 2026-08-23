using chat_service.frame;
using chat_service.net;
using chat_service.protocol;
using chat_service.user;
using chat_service.util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                if("201".Equals(commonRes.getCode()))
                {
                    MessageBox.Show(commonRes.getMessage());
                    return null;
                }

                if ("202".Equals(commonRes.getCode()))
                {
                    MessageBox.Show(commonRes.getMessage());
                    return null;
                }

                this.Invoke(new MethodInvoker(delegate () { this.Hide(); }));

                // 打开聊天主解面
                return new Main_Form(netResponse.getCommonRes());
            }
        }

        public Login_Register_Form()
        {
            InitializeComponent();
            BuildModernLoginLayout();
        }
        
        // 登录系统
        private void login_button_Click(object sender, EventArgs e)
        {
            this.executeLogin();
        }

        // 用户注册
        private void register_button_Click(object sender, EventArgs e)
        {
            Register_Form regiterFrom = new Register_Form();
            regiterFrom.ShowDialog();
        }

        // 设置IP地址
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

        // 登录成功后回调
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

        // 用户名textBox触发回车
        private void userName_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)//如果输入的是回车键  
            {
                this.executeLogin();
            }
        }

        // 密码textBox触发回车
        private void password_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)//如果输入的是回车键  
            {
                this.executeLogin();
            }
        }

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

            // 2、使用新协议（SocketManager + AuthenticationService）进行登录
            string userName = userName_textBox.Text.Trim();
            string password = password_textBox.Text;

            // 禁用登录按钮，避免重复提交
            this.login_button.Enabled = false;

            Thread loginThread = new Thread(() =>
            {
                try
                {
                    DoLogin(userName, password);
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new MethodInvoker(delegate ()
                    {
                        this.login_button.Enabled = true;
                        MessageBox.Show("登录失败: " + ex.Message);
                    }));
                }
            });
            loginThread.IsBackground = true;
            loginThread.Start();
        }

        // 后台线程执行登录
        private void DoLogin(string userName, string password)
        {
            // 确保主控连接已建立
            if (!SocketManager.Shared.IsConnected)
            {
                string[] address = NetServiceContext.remoteServiceAddress.Split(':');
                SocketManager.Shared.Connect(address[0], Convert.ToInt32(address[1]));
            }

            UserDO user = AuthenticationService.Shared.Login(userName, password);

            // 登录成功：切换到主界面
            this.BeginInvoke(new MethodInvoker(delegate ()
            {
                this.login_button.Enabled = true;
                this.Hide();
                Main_Form mainForm = new Main_Form(user);
                mainForm.ShowDialog();
            }));
        }

        // 登录窗体关闭，发送关闭socket消息
        private void Login_Register_Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 关闭新协议 socket 连接
            chat_service.protocol.SocketManager.Shared.Disconnect();
        }
    }
}
