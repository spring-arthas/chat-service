using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using chat_service.util;
using chat_service.net;
using System.Threading;
using System.Net.Sockets;

namespace chat_service
{
    public partial class Setting_Form : Form
    {
        public Login_Register_Form Login_Register_Form_Temp = null;

        public static string remoteServiceAddress = "", remoteFileServiceAddress = "";

        public Setting_Form()
        {
            InitializeComponent();

            if (remoteServiceAddress == "")
            {
                // 从配置文件中读取
                remoteServiceAddress = XmlConfigUtils.GetValue("remoteServiceAddress");
                // 设置到当前的服务地址文本框
                Remote_Server_textBox.Text = remoteServiceAddress;
            }
            else
            {
                // 设置到当前的服务地址文本框
                Remote_Server_textBox.Text = remoteServiceAddress;
            }
        }

        // 重新配置服务地址后委托开启连接，如果在正常连接的情况下，修改了新的服务地址，则先关闭当前处于连接的socket后重新发起连接
        private void login_button_Click(object sender, EventArgs e)
        {
            // 1、如果重新修改了聊天服务地址，则更新到配置文件
            if (remoteServiceAddress != Remote_Server_textBox.Text)
            {
                XmlConfigUtils.UpdateConfig("remoteServiceAddress", Remote_Server_textBox.Text);
                remoteServiceAddress = Remote_Server_textBox.Text;
            }
            else
            {
                // 如果等于不做任何操作
                remoteServiceAddress = Remote_Server_textBox.Text;
                MessageBox.Show("当前聊天服务连接已建立或连接建立失败，请重新配置.....");
                return;
            }

            // 2、更新应用内存内用于socket连接的服务地址
            NetServiceContext.remoteServiceAddress = Remote_Server_textBox.Text;

            // 3、断开当前新协议的 socket 连接
            chat_service.protocol.SocketManager.Shared.Disconnect();

            // 4、重启建立新的连接
            Program.delegateCreateConnection();
            this.Close();
        }

        // 父窗体的引用
        public void GetForm(Login_Register_Form login_Register_Form)
        {
            Login_Register_Form_Temp = login_Register_Form;
        }
    }
}
