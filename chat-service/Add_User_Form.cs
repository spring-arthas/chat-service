using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_service
{
    public partial class Add_User_Form : Form
    {
        public Add_User_Form()
        {
            InitializeComponent();

            // 获取所有用户
            queryAllUser();

        }

        private void queryAllUser()
        {
            // 获取所有用户
            //Dictionary<string, object> sendDictionary = new Dictionary<string, object>();
            //sendDictionary.Add("queryUser", queryUser);
            //NetServiceContext.sendMessageNotWaiting(12, JsonConvert.SerializeObject(sendDictionary), this);
        }
        
        // 搜索好友
        private void query_user_button_Click(object sender, EventArgs e)
        {

        }
    }
}
