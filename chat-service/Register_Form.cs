using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using chat_service.user;
using Newtonsoft.Json;
using chat_service.util;
using chat_service.net;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Runtime.Remoting.Messaging;
using chat_service.frame;

namespace chat_service
{
    public partial class Register_Form : Form
    {
        private static string userName = "", password = "";

        private static Register_Form register_Form = null;

        // 注册处理委托
        public delegate NetResponse RegisterHandler(NetResponse netResponse);

        // 委托方法
        public static NetResponse Register(NetResponse netResponse)
        {
            if (!netResponse.getResponse().Equals(NetResponse.Response.SUCCESS))
            {
                MessageBox.Show(netResponse.getError());
                return null;
            }
            else
            {
                // 注册成功，回填用户名和密码直接进行登录,并关闭当前注册窗体
                return netResponse;
            }
        }

        public Register_Form()
        {
            InitializeComponent();
            register_Form = this;
        }
        
        // 注册
        private void login_button_Click(object sender, EventArgs e)
        {
            if (userName_textBox.Text == "")
            {
                MessageBox.Show("用户名不能为空");
                return;
            }

            if (password_textBox.Text == "")
            {
                MessageBox.Show("密码不能为空");
                return;
            }

            if (phone_textBox.Text == "")
            {
                MessageBox.Show("联系方式不能为空");
                return;
            }

            if (mail_textBox.Text == "")
            {
                MessageBox.Show("邮箱不能为空");
                return;
            }

            // 远程连接以及注册用户
            UserModel userModel = new UserModel();
            userModel.setUserName(userName_textBox.Text);
            userName = userName_textBox.Text;
            userModel.setPassword(password_textBox.Text);
            password = password_textBox.Text;
            userModel.setPhone(phone_textBox.Text);
            userModel.setMail(mail_textBox.Text);
            NetServiceContext.register(JsonConvert.SerializeObject(userModel));
        }

        // --> *********************************************** 委托调用 ********************************************

        // 注册代理调用
        public static void registerDelegateHandler(object obj, NetResponse netResponse)
        {
            Login_Register_Form login_Register_Form = (Login_Register_Form)obj;
            RegisterHandler registerHandler = new RegisterHandler(Register);
            registerHandler.BeginInvoke(netResponse, new AsyncCallback(registerAsyncHandler), obj);
        }

        // 注册成功后回调
        public static void registerAsyncHandler(IAsyncResult result)
        {
            RegisterHandler registerHandler = (RegisterHandler)((AsyncResult)result).AsyncDelegate;
            NetResponse netResponse = registerHandler.EndInvoke(result);
            if (null != netResponse)
            {
                CommonRes commonRes = (CommonRes)netResponse.getCommonRes();
                if (null != commonRes.getMessageType() && commonRes.getMessageType().Equals("REPEAT"))
                {
                    // 弹窗提示注册成功
                    MessageBox.Show(netResponse.getResult());
                    return;
                }

                Login_Register_Form login_Register_Form = (Login_Register_Form) result.AsyncState;

                // 回填成功注册的用户名
                login_Register_Form.userName_textBox.Invoke(new MethodInvoker(delegate ()
                {
                    login_Register_Form.userName_textBox.Text = userName;
                }));

                // 回填成功注册的密码
                login_Register_Form.password_textBox.Invoke(new MethodInvoker(delegate ()
                {
                    login_Register_Form.password_textBox.Text = password;
                }));

                // 弹窗提示注册成功
                MessageBox.Show("用户名：[ " + userName + " ] " + netResponse.getResult());

                // 关闭注册窗口
                register_Form.Invoke(new MethodInvoker(delegate ()
                {
                    register_Form.Close();
                }));
            }
        }
    }
}
