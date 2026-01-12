using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace chat_service
{
    public partial class Test : Form
    {
        public Test()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Socket socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Parse("30.7.80.92");
            socketSend.Connect(ip, 10086);

            int index = 0;
            while (true)
            {
                ++index;
                string message = "这是客户端第 [ " + index + " ] 次发送消息\r\n";
                byte[] buffer = new byte[1024];
                buffer = Encoding.Default.GetBytes(message);
                socketSend.Send(buffer);
                richTextBox1.AppendText(message);
                Thread.Sleep(1000);

                if (index == 5)
                {
                    socketSend.Shutdown(SocketShutdown.Both);
                    //  socketSend.Close();
                    richTextBox1.AppendText("客户端关闭了连接");
                    break;
                }
            }

            Thread.Sleep(20000);
        }
    }
}
