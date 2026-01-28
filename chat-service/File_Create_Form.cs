using chat_service.file;
using chat_service.util;
using Newtonsoft.Json;
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
    public partial class File_Create_Form : Form
    {
        private TreeNode selectTreeNode;

        private string userName = "", operate = "";

        public static File_Create_Form file_Create_Form;

        public File_Create_Form()
        {
            InitializeComponent();
        }

        public File_Create_Form(TreeNode selectTreeNode, string userName, string operate)
        {
            InitializeComponent();
            this.selectTreeNode = selectTreeNode;
            this.userName = userName;
            this.operate = operate;
            file_Create_Form = this;

            this.parent_file_node_label.Text = "上级文件夹: /" + selectTreeNode.Text.ToString();
        }

        private void file_create_button_Click(object sender, EventArgs e)
        {
            if (this.new_file_textBox.Text == "")
            {
                MessageBox.Show("文件夹名称不能为空");
                return;
            }

            if (this.operate == "CREATE")
            {
                long parentId = ((FileDto)this.selectTreeNode.Tag).getId();
                string dirName = this.new_file_textBox.Text.Trim();
                NetServiceContext.createDirectory(parentId, dirName);
            }
            else if (this.operate == "UPDATE")
            {
                long dirId = ((FileDto)this.selectTreeNode.Tag).getId();
                string newName = this.new_file_textBox.Text.Trim();
                NetServiceContext.updateDirectory(dirId, newName);
            }
        }
    }
}
