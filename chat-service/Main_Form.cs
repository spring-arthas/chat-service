using chat_service.file;
using chat_service.frame;
using chat_service.net;
using chat_service.service.file;
using chat_service.user;
using chat_service.util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_service
{
    public partial class Main_Form : Form
    {
        // 当前登录用户数据
        public CommonRes commonRes = null;

        // 当前窗体类实例
        public static Main_Form main_Form = null;

        // 文件选择框
        private OpenFileDialog openFileDialog = null;

        // 当前选择的聊天用户
        private static string currentSelectUser = "";

        // 待处理任务是否以全部开始的方式进行下载
        private static bool isBeginByAll = false;

        // 全局判断是否正在执行文件在线传输
        private static bool isOnlineUpload = false;

        // 全局网盘树中节点右键获取的节点名称
        private static TreeNode currentSelectedNode = null;

        // 个人网盘文件上传集合
        public static List<Dictionary<string, object>> personalFileUploadList = new List<Dictionary<string, object>>();

        // 个人网盘文件下载集合
        public static List<Dictionary<string, object>> personalFileDownloadList = new List<Dictionary<string, object>>();

        // 个人网盘文件删除集合
        public static List<Dictionary<string, object>> personalFileDeleteList = new List<Dictionary<string, object>>();

        // 异步上传任务集
        public static List<AsyncPersonalFileUploadHelper> uploadHelper = new List<AsyncPersonalFileUploadHelper>();

        // 异步下载任务集
        public static List<AsyncPersonalFileDownloadHelper> downloadHelper = new List<AsyncPersonalFileDownloadHelper>();

        // 上一页、下一页、总页数
        public static int currentPage = 1, pageSize = 13, sumPageCount = 0;

        // 网卡数据相关
        private PerformanceCounter networkR = null, networkS = null;

        private string netActiveName = "";

        private string[] networkNames = null;


        public Main_Form()
        {
            InitializeComponent();
        }

        public Main_Form(object obj)
        {
            InitializeComponent();

            // 登陆成功后持有的用户信息
            this.commonRes = (CommonRes)obj;

            // 当前对象
            main_Form = this;

            // 初始化解面数据
            this.initData();
        }

        // 初始化展示数据
        private void initData()
        {

            // 远程服务地址
            remote_address_textBox.Text = NetServiceContext.remoteServiceAddress;

            // 欢迎术语
            user_label.Text = "欢迎，" + commonRes.getUserName() + "使用，登录时间: " + commonRes.getTime();

            // 与服务器连接结果
            result_label.Visible = true;
            result_label.ForeColor = Color.Green;
            result_label.Text = "网络连接正常......";

            // 定时刷新时间
            this.timer1.Interval = 1000;//设置定时器触发间隔
            this.timer1.Start();    //启动定时器

            // 定时网络判断
            this.timer2.Interval = 120000;//设置定时器触发间隔
            this.timer2.Start();    //启动定时器

            // 个人网盘
            this.personal_file_treeView.ExpandAll();
            //FileService.createFileRootTree(this.personal_file_treeView, commonRes.getUserName().Trim());

            // 下载路径
            global_download_path_label.Text = "当前下载路径: " + NetServiceContext.globalDownloadPath;

            // 创建下载文件路径对应文件夹
            this.createDownloadFolder();
        }

        // 创建下载文件路径对应文件夹
        private void createDownloadFolder()
        {
            if (NetServiceContext.globalDownloadPath != "")
            {
                if (System.IO.Directory.Exists(NetServiceContext.globalDownloadPath) == false)//如果不存在就创建file文件夹
                {
                    System.IO.Directory.CreateDirectory(NetServiceContext.globalDownloadPath);
                }
            }
        }

        // 定时更新当前时间
        private void timer1_Tick(object sender, EventArgs e)
        {
            date_label.ForeColor = Color.Green;
            date_label.Text = "当前时间: " + DateTime.Now.ToLocalTime().ToString();
        }

        //刷新网络速率
        private void timer3_Tick(object sender, EventArgs e)
        {
            // 获取活跃的网卡
            if (netActiveName == "")
            {
                NetworkInterface[] fNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in fNetworkInterfaces)
                {
                    if (network.Description.Contains("Loopback"))
                    {
                        continue;
                    }

                    if (network.OperationalStatus == OperationalStatus.Up)
                    {
                        netActiveName = network.Description;
                    }
                }
            }

            // 统计网卡
            if (networkNames == null)
            {
                // 获取网卡数据
                networkNames = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
                foreach (string name in networkNames)
                {
                    if ((name.Contains("Wi-Fi") && netActiveName.Contains("Wi-Fi")) || (name.Contains("Ethernet") && netActiveName.Contains("Ethernet")))
                    {
                        if (networkR == null)
                        {
                            networkR = new PerformanceCounter("Network Interface", "Bytes Received/sec", name);//获取网络接收速度
                        }

                        if (networkS == null)
                        {
                            networkS = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name);
                        }

                        net_rate_label.Text = "网卡名称: [" + name + "], 下载网速: [" + (networkR.NextValue() / 1024 / 1024).ToString("0.00") + "mb/s]  上传网速: [" + (networkS.NextValue() / 1024 / 1024).ToString("0.00") + "mb/s]  ";
                        return;
                    }
                }
            }

            net_rate_label.Text = "网卡名称: [" + netActiveName + "], 下载网速: [" + (networkR.NextValue() / 1024 / 1024).ToString("0.00") + "mb/s]  上传网速: [" + (networkS.NextValue() / 1024 / 1024).ToString("0.00") + "mb/s]  ";
        }

        // 定时判断网络心跳
        private void timer2_Tick_1(object sender, EventArgs e)
        {
            // 执行退出操作，弹出登录框，重新选择用户登录
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            dictionary.Add("userName", commonRes.getUserName());
            dictionary.Add("heartInterval", this.timer2.Interval.ToString());
            dictionary.Add("data", "HAERT_REQUEST");
            NetServiceContext.sendMessageNotWaiting(5, JsonConvert.SerializeObject(dictionary), this);
        }

        // 退出登录
        private void exist_button_Click(object sender, EventArgs e)
        {
            // 先执行一次上传和下载任务的清空
            fileUploadClear();
            fileDownloadClear();

            if (uploadHelper.Count > 0 || downloadHelper.Count > 0)
            {
                if (MessageBox.Show("当前存在未完成的传输任务,是否强制执行取消", "系统提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    // 添加任务
                    Task<bool>[] cancleUploadTasks = new Task<bool>[uploadHelper.Count];
                    Task<bool>[] cancleDownloadTasks = new Task<bool>[downloadHelper.Count];
                    if (uploadHelper.Count > 0)
                    {
                        for (int i = 0; i < uploadHelper.Count; i++)
                        {
                            AsyncPersonalFileUploadHelper helper = uploadHelper[i];
                            cancleUploadTasks[i] = new Task<bool>(() => closeUploadTask(helper));
                        }
                    }

                    if (downloadHelper.Count > 0)
                    {
                        for (int i = 0; i < downloadHelper.Count; i++)
                        {
                            AsyncPersonalFileDownloadHelper helper = downloadHelper[i];
                            cancleDownloadTasks[i] = new Task<bool>(() => closeDownloadTask(helper));
                        }
                    }

                    // 执行任务
                    //int result = 0;
                    for (int i = 0; i < cancleUploadTasks.Length; i++)
                    {
                        Task<bool> task = cancleUploadTasks[i];
                        task.Start();
                        //task.GetAwaiter().OnCompleted(() =>
                        //{
                        //    result = task.Result ? result++ : result;
                        //});
                    }
                    for (int i = 0; i < cancleDownloadTasks.Length; i++)
                    {
                        Task<bool> task = cancleDownloadTasks[i];
                        task.Start();
                        //task.GetAwaiter().OnCompleted(() =>
                        //{
                        //    result = task.Result ? result++ : result;
                        //});
                    }
                    Task.WaitAll(cancleUploadTasks);
                    Task.WaitAll(cancleDownloadTasks);

                    // 释放静态资源
                    uploadHelper.Clear();
                    downloadHelper.Clear();
                    releaseTaskResource();
                    this.Close();
                }
            }
            else
            {
                releaseTaskResource();
                this.Close();
            }
        }

        private void Main_Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 先执行一次上传和下载任务的清空
            fileUploadClear();
            fileDownloadClear();

            if (uploadHelper.Count > 0 || downloadHelper.Count > 0)
            {
                if (MessageBox.Show("当前存在未完成的传输任务,是否强制执行取消", "系统提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    // 添加任务
                    Task<bool>[] cancleUploadTasks = new Task<bool>[uploadHelper.Count];
                    Task<bool>[] cancleDownloadTasks = new Task<bool>[downloadHelper.Count];
                    if (uploadHelper.Count > 0)
                    {
                        for (int i = 0; i < uploadHelper.Count; i++)
                        {
                            AsyncPersonalFileUploadHelper helper = uploadHelper[i];
                            cancleUploadTasks[i] = new Task<bool>(() => closeUploadTask(helper));
                        }
                    }

                    if (downloadHelper.Count > 0)
                    {
                        for (int i = 0; i < downloadHelper.Count; i++)
                        {
                            AsyncPersonalFileDownloadHelper helper = downloadHelper[i];
                            cancleDownloadTasks[i] = new Task<bool>(() => closeDownloadTask(helper));
                        }
                    }

                    // 执行任务
                    //int result = 0;
                    for (int i = 0; i < cancleUploadTasks.Length; i++)
                    {
                        Task<bool> task = cancleUploadTasks[i];
                        task.Start();
                    }
                    for (int i = 0; i < cancleDownloadTasks.Length; i++)
                    {
                        Task<bool> task = cancleDownloadTasks[i];
                        task.Start();
                    }
                    Task.WaitAll(cancleUploadTasks);
                    Task.WaitAll(cancleDownloadTasks);

                    // 释放静态资源
                    uploadHelper.Clear();
                    downloadHelper.Clear();

                    // 执行退出操作，弹出登录框，重新选择用户登录
                    NetServiceContext.logout();

                    releaseTaskResource();

                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                // 执行退出操作，弹出登录框，重新选择用户登录
                NetServiceContext.logout();

                releaseTaskResource();
            }
        }

        private bool closeUploadTask(AsyncPersonalFileUploadHelper helper)
        {
            if (helper.Bg_Worker.IsBusy)
            {
                // 执行异步取消
                helper.Bg_Worker.CancelAsync();
                // 等待结果
                while (true)
                {
                    if (helper.doWorkEventArgs.Cancel)
                    {
                        return true;
                    }
                    Thread.Sleep(50);
                }
            }

            return false;
        }

        private bool closeDownloadTask(AsyncPersonalFileDownloadHelper helper)
        {
            if (helper.Bg_Worker.IsBusy)
            {
                // 执行异步取消
                helper.Bg_Worker.CancelAsync();
                // 等待结果
                while (true)
                {
                    if (helper.doWorkEventArgs.Cancel)
                    {
                        return true;
                    }
                    Thread.Sleep(50);
                }
            }

            return false;
        }

        // 释放传输任务中的资源
        private void releaseTaskResource()
        {
            timer1.Stop();
            timer2.Stop();
            AsyncPersonalFileUploadHelper.taskCount = 0;
            AsyncPersonalFileDownloadHelper.taskCount = 0;
            main_Form = null;
        }

        // 发送消息
        private void send_button_Click(object sender, EventArgs e)
        {
            this.sendMessage();
        }

        // 发送消息
        private void sendMessage()
        {
            if ("".Equals(currentSelectUser))
            {
                MessageBox.Show("请选择需要聊天的用户");
                return;
            }

            this.send_message_richTextBox.Text.Replace("\r", "").Trim();
            this.send_message_richTextBox.Text.Replace("\n", "").Trim();
            this.send_message_richTextBox.Text.Replace("\r\n", "").Trim();
            if ("".Equals(this.send_message_richTextBox.Text.Trim()))
            {
                return;
            }

            // 如果遇到列表的刷新，判断刷新后的列表数据中是否还包含上次聊天的用户，不包含则终止发送
            IEnumerable<DataGridViewRow> enumerableList = this.user_list_dataGridView.Rows.Cast<DataGridViewRow>();
            List<DataGridViewRow> list = (from item in enumerableList where item.Cells[1].Value.ToString() == currentSelectUser select item).ToList();
            if (null == list || list.Count == 0)
            {
                MessageBox.Show(" 用户 [ " + currentSelectUser + " ] 已下线");
                return;
            }

            // 发送聊天数据
            Dictionary<string, object> sendDictionary = new Dictionary<string, object>();
            sendDictionary.Add("currentUserName", this.commonRes.getUserName());
            sendDictionary.Add("remoteUserName", currentSelectUser);
            sendDictionary.Add("content", this.send_message_richTextBox.Text.Replace(@"\r\n", "").Trim());
            NetServiceContext.sendMessageNotWaiting(2, JsonConvert.SerializeObject(sendDictionary), this);

            // 记录发送日志
            message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 向 [ " + currentSelectUser + " ] 发送: " + send_message_richTextBox.Text.ToString() + "\r\n");

            // 清空发送列表
            send_message_richTextBox.Clear();
        }

        // 刷新在线用户, 此处需要异步进行刷新
        private void refresh_button_Click(object sender, EventArgs e)
        {
            // 执行退出操作，弹出登录框，重新选择用户登录
            UserModel userModel = new UserModel();
            userModel.setRefresh("true");
            userModel.setUserName(commonRes.getUserName());
            NetServiceContext.sendMessageNotWaiting(3, JsonConvert.SerializeObject(userModel), this);
        }

        // 好友列表取消默认行选中
        private void user_list_dataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            user_list_dataGridView.ClearSelection();
        }

        // 好友列表选中某行触发
        private void user_list_dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                // 或
                currentSelectUser = this.user_list_dataGridView.Rows[e.RowIndex].Cells[1].Value.ToString();
                this.chat_with_user_label.Text = "正在与 [ " + currentSelectUser + " ] 进行聊天";
            }
        }

        // 聊天输入框回车键触发发送消息
        private void send_message_richTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.Handled == Keys.Enter)//如果输入的是回车键  
            //{
            //    this.sendMessage();
            //}

            if (e.KeyChar == '\r')//判断是否是回车。
            {
                this.sendMessage();
            }
        }

        // 点击tab页触发
        private void main_tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (main_tabControl.SelectedTab.Name == "file_tabPage")
            {
                person_folder_label.Text = commonRes.getUserName() + "网盘";
                this.refreshFileRefreshTree(commonRes.getUserName(), commonRes.getUserName(), commonRes.getUserName());
            }
        }

        // 查询好友
        private void query_user_button_Click_1(object sender, EventArgs e)
        {
            if (query_user_textBox.Text.Trim() == "")
            {
                return;
            }

            queryAddUser(query_user_textBox.Text.Trim());
        }

        // 添加好友
        private void add_user_button_Click(object sender, EventArgs e)
        {
            Add_User_Form add_User_Form = new Add_User_Form();
            add_User_Form.ShowDialog();
        }

        private void queryAddUser(string queryUser)
        {
            // 搜索好友
            Dictionary<string, object> sendDictionary = new Dictionary<string, object>();
            sendDictionary.Add("queryUser", queryUser);
            NetServiceContext.sendMessageNotWaiting(12, JsonConvert.SerializeObject(sendDictionary), this);
        }

        // ********************************************* 在线传送文件开始 *********************************************//

        // 待处理任务列表点击开始下载列触发
        private void task_list_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!isOnlineUpload) // 首次上传，直接更新传输任务状态
            {
                isOnlineUpload = true;
            }
            else
            {

            }

            string taskStatus = this.task_list_dataGridView.Rows[e.RowIndex].Cells[4].Value.ToString();

            int CIndex = e.ColumnIndex;
            if (CIndex == 5) // 下载
            {
                // 判断当前行的文件接收
                if (taskStatus == "接收中")
                {
                    MessageBox.Show("当前文件正在接收,请勿重复接收 ! ! !", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                // 每一行建立一个文件服务类进行处理文件接收
                this.task_list_dataGridView.Rows[e.RowIndex].Cells[4].Value = "接收中";
                this.task_list_dataGridView.Rows[e.RowIndex].Cells[9].Value = "true";
                taskStatus = "接收中";
                this.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 开始接收来自用户 [ "
                    + this.task_list_dataGridView.Rows[e.RowIndex].Cells[12].Value + " ] 发送的 [ "
                    + this.task_list_dataGridView.Rows[e.RowIndex].Cells[10].Value + " ] 文件");

                this.beginFileTransportTask(this.task_list_dataGridView.Rows[e.RowIndex]);
            }

            if (CIndex == 6) // 暂停 关闭当前接收任务通道
            {

            }

            if (CIndex == 7) // 取消
            {

            }

            if (CIndex == 8) // 拒绝
            {

            }
        }

        // 待处理任务全部停止传送
        private void all_task_stop_button_Click(object sender, EventArgs e)
        {

        }

        // 待处理任务全部开始
        private void all_task_begin_button_Click(object sender, EventArgs e)
        {
            //if (FileService.fileTaskDictionary.IsEmpty)
            //{
            //    MessageBox.Show("暂时没有待处理的文件任务哦 ! ! !", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            //    return;
            //}

            //// 1、设置任务以全部方式下载
            //isBeginByAll = true;

            //// 2、当前帧需要执行文件在线传输操作，令开辟线程执行文件传输操作，初始化后台异步在线接收委托
            //backGroundWorkerReceiveOnlineTransport.RunWorkerAsync(); //开启异步执行
        }

        // 处理在线文件接收
        private void beginFileTransportTask(DataGridViewRow dataGridViewRow)
        {
            try
            {
                // 判断是否是以全部下载方式下载，如果是,则不用管待处理任务列表是有记录，直接按照FilService类处理
                if (isBeginByAll)
                {
                    // 判断下载任务是否进行中,isReceiveBusy只用于判断以全部开始下载的方式进行文件的传输
                }
                else
                {
                    // 初始化控件显示
                    this.upload_path_textBox.Text = dataGridViewRow.Cells[10].Value.ToString();
                    this.upload_size_textBox.Text = dataGridViewRow.Cells[11].Value.ToString();
                    this.upload_waiting_label.Text = "[ " + dataGridViewRow.Cells[10].Value.ToString() + " ]";
                    this.upload_progressBar.Minimum = 0;
                    this.upload_progressBar.Maximum = Convert.ToInt32(dataGridViewRow.Cells[11].Value.ToString());
                    this.upload_path_label.Visible = true;
                    this.upload_path_textBox.Visible = true;
                    this.upload_size_label.Visible = true;
                    this.upload_size_textBox.Visible = true;
                    this.upload_waiting_label.Visible = true;
                    this.upload_progress_label.Visible = true;
                    this.upload_progressBar.Visible = true;

                    // 开始下载任务,此处只需遍历当前用户的待处理的任务集
                    FileService fileService = new FileService(dataGridViewRow);
                    fileService.receiveOnlineTransportHandler();
                }
            }
            finally
            {
            }
        }

        // 处理在线文件取消
        private void cancelFileTransportTask(DataGridViewRow dataGridViewRow)
        {
            try
            {
                // 判断是否是以全部下载方式下载，如果是,则不用管待处理任务列表是有记录，直接按照FilService类处理
                if (isBeginByAll)
                {
                    // 判断下载任务是否进行中,isReceiveBusy只用于判断以全部开始下载的方式进行文件的传输
                }
                else
                {
                    // 初始化控件显示
                    this.upload_path_textBox.Text = dataGridViewRow.Cells[10].Value.ToString();
                    this.upload_size_textBox.Text = dataGridViewRow.Cells[11].Value.ToString();
                    this.upload_waiting_label.Text = "[ " + dataGridViewRow.Cells[10].Value.ToString() + " ]";
                    this.upload_progressBar.Minimum = 0;
                    this.upload_progressBar.Maximum = Convert.ToInt32(dataGridViewRow.Cells[11].Value.ToString());
                    this.upload_path_label.Visible = true;
                    this.upload_path_textBox.Visible = true;
                    this.upload_size_label.Visible = true;
                    this.upload_size_textBox.Visible = true;
                    this.upload_waiting_label.Visible = true;
                    this.upload_progress_label.Visible = true;
                    this.upload_progressBar.Visible = true;

                    // 开始下载任务,此处只需遍历当前用户的待处理的任务集
                    FileService fileService = new FileService(dataGridViewRow);
                    fileService.receiveOnlineTransportHandler();
                }
            }
            finally
            {
            }
        }

        // 用户列表中点击文件传送列button
        private void user_list_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int CIndex = e.ColumnIndex;
            if (CIndex == 5)
            {

                this.initOpenFileDialog();
                this.initProgressBar();
                this.sendOnlineTransportHandler(e.RowIndex, this.user_list_dataGridView.Rows[e.RowIndex]);
            }
        }

        // 初始化文件选择框
        private void initOpenFileDialog()
        {
            if (null == this.openFileDialog)
            {
                this.openFileDialog = new OpenFileDialog();
                this.openFileDialog.InitialDirectory = @"D:\";//设置文件打开初始目录为E盘
                this.openFileDialog.Title = "选择文件";//设置打开文件对话框标题
                this.openFileDialog.Multiselect = true; // 多文件上传
                this.openFileDialog.Filter = "All Files(*.*)|*.*";//设置文件过滤类型
                //this.openFileDialog.Filter = "All Files(*.*)|*.*|txt Files(*.txt)|*.txt";//设置文件过滤类型
                //this.openFileDialog.FilterIndex = 2;//根据文件类型索引设置文件过滤类型
                this.openFileDialog.RestoreDirectory = true;//设置对话框是否记忆之前打开的目录
            }
        }

        // 后台进度条初始化以及backGroundWorker初始化
        private void initProgressBar()
        {
            this.upload_progressBar.Minimum = 0;
        }

        // 用户列表点击上传开始文件传送处理
        private void sendOnlineTransportHandler(int currentRow, DataGridViewRow dataGridViewRow)
        {
            string receiveUserName = this.user_list_dataGridView.Rows[currentRow].Cells[1].Value.ToString();
            // 打开文件选择弹出框
            DialogResult result = DialogResult.Cancel;
            Thread openFileDialogThread = new Thread((ThreadStart)(() =>
            {
                result = this.openFileDialog.ShowDialog();
            }));
            openFileDialogThread.SetApartmentState(ApartmentState.STA);
            openFileDialogThread.Start();
            openFileDialogThread.Join();

            // 获取选择结果
            if (result == DialogResult.OK)
            {
                // 获取文件名称展示
                string[] safeFileNames = this.openFileDialog.SafeFileNames;
                string fileNames = "";
                if (safeFileNames.Length > 0)
                {
                    for (int i = 0; i < safeFileNames.Length; i++)
                    {
                        fileNames += safeFileNames[i] + ";";
                    }
                }

                if (fileNames.EndsWith(";"))
                {
                    fileNames.Substring(0, fileNames.LastIndexOf(";"));
                }
                this.upload_path_textBox.Text = fileNames;//获取选择文件的完整路径名（含文件名称）

                // 创建在线传输文件Map对象
                List<Dictionary<string, object>> fileDicList = new List<Dictionary<string, object>>();
                long fileSize = 0L;
                string[] files = new string[safeFileNames.Length];
                if ((files = this.openFileDialog.FileNames).Length > 0)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        FileStream fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                        dictionary.Add("fileStream", fileStream);
                        dictionary.Add("fileSize", fileStream.Length);
                        dictionary.Add("fileName", safeFileNames[i]);
                        dictionary.Add("launchUserName", commonRes.getUserName());
                        dictionary.Add("receiveUserName", receiveUserName);
                        dictionary.Add("filePath", files[i]);
                        dictionary.Add("currentRow", currentRow);

                        fileSize += fileStream.Length;
                        fileDicList.Add(dictionary);
                    }
                }

                // 设置文件大小
                this.upload_size_textBox.Text = fileSize.ToString() + "字节";

                // 聊天日志框追加操作日志
                this.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 向 [ " + receiveUserName + " ] 开始上传: [ " + this.upload_path_textBox.Text + " ], 总大小: [ " + this.upload_size_textBox.Text + " ]\r\n");

                // 设置传送总大小到progressBar
                this.upload_progressBar.Maximum = Convert.ToInt32(fileSize);

                // 文件上传,每一用户行都新建文件服务类FileService进行在线文件传输
                FileService fileService = new FileService(commonRes.getUserName(), receiveUserName, fileDicList, fileSize, dataGridViewRow);
                fileService.sendOnlineTransportHandler();

                // 控件显示
                this.upload_path_label.Visible = true;
                this.upload_path_textBox.Visible = true;
                this.upload_size_label.Visible = true;
                this.upload_size_textBox.Visible = true;
                this.upload_waiting_label.Visible = true;
                this.upload_progress_label.Visible = true;
                this.upload_progressBar.Visible = true;
            }
        }

        // 文件在线传输发起端更新发送进度条
        public void backGroundWorkerSendOnlineTransport_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.upload_progressBar.Value = e.ProgressPercentage;
            this.upload_progress_label.Text = "进度:" + (e.ProgressPercentage * 100 / this.upload_progressBar.Maximum).ToString() + "%";
        }

        // 文件在线传输任务完成
        public void backGroundWorkerSendOnlineTransport_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.upload_waiting_label.Text = "文件全部传送成功";
            //MessageBox.Show("文件全部传送成功！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            this.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件全部传送成功！\r\n");
        }

        // 文件在线传输接收端更新发送进度条
        public void backGroundWorkerReceiveOnlineTransport_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.upload_progressBar.Value = e.ProgressPercentage;
            this.upload_progress_label.Text = "进度:" + (e.ProgressPercentage * 100 / this.upload_progressBar.Maximum).ToString() + "%";
        }

        // 文件在线传输任务完成
        public void backGroundWorkerReceiveOnlineTransport_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.upload_waiting_label.Text = "文件全部接收成功";
            //MessageBox.Show("文件全部接收成功！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            this.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件全部接收成功！\r\n");
        }


        // ********************************************* 在线传送文件结束 *********************************************//









        // ********************************************* 个人网盘 *********************************************//

        // 网盘文件夹树刷新
        private void file_refresh_button_Click(object sender, EventArgs e)
        {
            this.refreshFileRefreshTree(commonRes.getUserName(), commonRes.getUserName(), commonRes.getUserName());
        }

        // 鼠标右键点击树中的节点下拉显示节点操作
        private void personal_file_treeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)//判断你点的是不是右键
            {
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode CurrentNode = this.personal_file_treeView.GetNodeAt(ClickPoint);
                if (CurrentNode != null)//判断你点的是不是一个节点
                {
                    CurrentNode.ContextMenuStrip = this.file_context_menu_trip;
                    currentSelectedNode = CurrentNode;

                    FileDto fileDto = (FileDto)currentSelectedNode.Tag;
                    this.folder_create_time_label.Visible = true;
                    this.folder_create_time_label.Text = Utils.ToDateTime(fileDto.getGmtCreate()).ToString();
                    this.folder_create_path_label.Visible = true;
                    this.folder_create_path_label.Text = fileDto.getFilePath();
                    this.file_sum_count_label.Visible = true;
                    this.file_sum_count_label.Text = "0";
                }
            }

            // 刷新所选中节点文件下的个人网盘列表，
            if (e.Button == MouseButtons.Left)
            {
                // 动态跳转到上传列表tab页
                this.personal_file_tabPage.SelectedTab = this.personal_file_tabPage.TabPages["tabPage1"];
                // 每次点击新的文件夹节点，重置当前分页为第一页
                currentPage = 1;
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode CurrentNode = this.personal_file_treeView.GetNodeAt(ClickPoint);
                if (null != CurrentNode)
                {
                    currentSelectedNode = CurrentNode;
                    FileDto fileDto = (FileDto)CurrentNode.Tag;
                    this.folder_create_time_label.Visible = true;
                    this.folder_create_time_label.Text = Utils.ToDateTime(fileDto.getGmtCreate()).ToString();
                    this.folder_create_path_label.Visible = true;
                    this.folder_create_path_label.Text = fileDto.getFilePath();
                    this.file_sum_count_label.Visible = true;
                    this.file_sum_count_label.Text = "0";

                    NetServiceContext.getFileList(((FileDto)CurrentNode.Tag).getId(), currentPage, pageSize);
                }
            }

        }

        // 添加文件夹
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                // 弹框创建文件夹
                File_Create_Form file_Create_Form = new File_Create_Form(currentSelectedNode, commonRes.getUserName(), "CREATE");
                file_Create_Form.ShowDialog();
            }
        }

        // 删除文件夹
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                if (MessageBox.Show("确定删除该文件夹吗？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    long dirId = ((FileDto)currentSelectedNode.Tag).getId();
                    NetServiceContext.deleteDirectory(dirId);
                }
            }
        }

        // 修改文件夹
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                // 弹框创建文件夹
                File_Create_Form file_Create_Form = new File_Create_Form(currentSelectedNode, commonRes.getUserName(), "UPDATE");
                file_Create_Form.ShowDialog();
            }
        }

        // 上传文件
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                this.initOpenFileDialog();
                this.initProgressBar();
                FileDto fileDto = (FileDto)currentSelectedNode.Tag;
                if (fileDto.getHasChild() == "Y")
                {
                    MessageBox.Show("当前目录 [ " + fileDto.getFilePath() + " ] 存在二级目录无法上传文件，请选择最子目录进行！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                this.sendUploadFileHandler();
            }
        }

        // 文件列表点击checkBox列
        private void file_list_dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int cIndex = e.ColumnIndex;
            if (cIndex == 0)
            {
                //checkbox 勾上
                if ((bool)this.file_list_dataGridView.Rows[e.RowIndex].Cells[0].EditedFormattedValue == true)
                {
                    // 取消选中
                    this.file_list_dataGridView.Rows[e.RowIndex].Cells[0].Value = false;
                }
                else
                {
                    // 选中(同时增加personalFileDeleteList和personFileDownloadList)
                    this.file_list_dataGridView.Rows[e.RowIndex].Cells[0].Value = true;
                }
            }

            this.file_name_label.Text = this.file_list_dataGridView.Rows[e.RowIndex].Cells[2].Value.ToString();
            this.file_name_label.Visible = true;
            this.file_path_label.Text = this.file_list_dataGridView.Rows[e.RowIndex].Cells[3].Value.ToString();
            this.file_path_label.Visible = true;
            this.file_size_label.Text = this.file_list_dataGridView.Rows[e.RowIndex].Cells[4].Value.ToString();
            this.file_size_label.Visible = true;
            this.file_upload_time_label.Text = this.file_list_dataGridView.Rows[e.RowIndex].Cells[5].Value.ToString();
            this.file_upload_time_label.Visible = true;
            this.file_status_label.Text = this.file_list_dataGridView.Rows[e.RowIndex].Cells[6].Value.ToString();
            this.file_status_label.Visible = true;
        }

        // 文件列表中点击下载、删除
        private void file_list_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int CIndex = e.ColumnIndex;
            string tag = this.file_list_dataGridView.CurrentRow.Cells[9].Value.ToString();
            string fileName = this.file_list_dataGridView.CurrentRow.Cells[2].Value.ToString();
            string taskStatus = this.file_list_dataGridView.CurrentRow.Cells[6].Value.ToString();

            // 下载(需要判断当前行的文件是否处于下载列表中，处于则不能重复下载)
            if (CIndex == 7)
            {
                if (taskStatus == "上传中" || taskStatus == "上传成功")
                {
                    MessageBox.Show("文件 [ " + fileName + " ] " + taskStatus + "，请勿重复上传！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                // 将当前待下载的文件添加到现在列表中
                int downloadCount = this.file_download_list_dataGridView.Rows.Count;
                if (downloadCount > 0)
                {
                    // 判断是否重复添加待下载文件,只要有记录就不能添加，无需关注是否处于下载中还是未下载状态
                    for (int i = 0; i < this.file_download_list_dataGridView.Rows.Count; i++)
                    {
                        if (fileName == this.file_download_list_dataGridView.Rows[i].Cells[1].Value.ToString())
                        {
                            MessageBox.Show("文件 [ " + fileName + " ] 已添加至下载列表请勿重复添加！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            return;
                        }
                    }
                }
                this.file_download_list_dataGridView.Rows.Add();
                this.file_download_list_dataGridView.Rows[downloadCount].Cells[1].Value = fileName;
                this.file_download_list_dataGridView.Rows[downloadCount].Cells[2].Value = getFileSize(long.Parse(this.file_list_dataGridView.CurrentRow.Cells[4].Value.ToString()));
                this.file_download_list_dataGridView.Rows[downloadCount].Cells[3].Value = "待下载";
                this.file_download_list_dataGridView.Rows[downloadCount].Cells[8].Value = tag;
                this.file_download_list_dataGridView.Rows[downloadCount].Cells[12].Value = NetServiceContext.globalDownloadPath + fileName;
                this.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ]  [ " + fileName + " ] 成功添加至下载列表\r\n");
                this.file_download_list_dataGridView.Rows[downloadCount].Cells[13].Value = long.Parse(this.file_list_dataGridView.CurrentRow.Cells[4].Value.ToString());

                this.personal_file_tabPage.SelectedTab = this.personal_file_tabPage.TabPages["tabPage3"];
            }

            // 删除（删除真实文件，但是数据库记录不删除，只是将文件记录的del状态由N变为Y）
            if (CIndex == 8)
            {
                if (taskStatus == "删除成功")
                {
                    MessageBox.Show("文件 [ " + fileName + " ] 删除成功，无需再次删除！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                if (currentSelectedNode != null)
                {
                    bool isDownload = false;
                    for (int j = 0; j < this.file_download_list_dataGridView.Rows.Count; j++)
                    {
                        // 待下载文件名称
                        string downloadFileName = this.file_download_list_dataGridView.Rows[j].Cells[1].Value.ToString();
                        string status = this.file_download_list_dataGridView.Rows[j].Cells[3].Value.ToString();
                        if (fileName == downloadFileName && status == "下载中...")
                        {
                            // 文件名称已匹配，且文件处于下载中，则不能删除，设置isDownload为true
                            isDownload = true;
                            break;
                        }
                    }

                    if (!isDownload)
                    {
                        // 向远程服务器发送删除文件通知，执行文件delete以及DB文件del状态更新
                        long fileId = long.Parse(this.file_list_dataGridView.CurrentRow.Cells[4].Value.ToString());
                        NetServiceContext.deleteFile(fileId);
                    }
                }
            }
        }

        // 上一页
        private void prePage_button_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                if (currentPage > 1)
                {
                    currentPage = currentPage - 1;
                }

                // 执行查询
                FileDto fileDto = (FileDto)currentSelectedNode.Tag;
                NetServiceContext.getFileList(((FileDto)currentSelectedNode.Tag).getId(), currentPage, pageSize);
            }
        }

        // 下一页
        private void nextPage_button_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                currentPage = currentPage + 1;

                // 执行查询
                FileDto fileDto = (FileDto)currentSelectedNode.Tag;
                NetServiceContext.getFileList(((FileDto)currentSelectedNode.Tag).getId(), currentPage, pageSize);
            }
        }

        // 文件列表全选
        private void all_select_button_Click(object sender, EventArgs e)
        {
            int count = this.file_list_dataGridView.Rows.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bool selected = (bool)this.file_list_dataGridView.Rows[i].Cells[0].EditedFormattedValue;
                    if (!selected)
                    {
                        this.file_list_dataGridView.Rows[i].Cells[0].Value = true;

                        // 追加当前被选中的行数据
                        //Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        //dictionary.Add("fileName", this.file_list_dataGridView.Rows[i].Cells[2].Value.ToString());
                        //dictionary.Add("tag", this.file_list_dataGridView.Rows[i].Cells[9].Value.ToString());
                        //dictionary.Add("rowNumber", i);
                        //personalFileDeleteList.Add(dictionary);
                        //personalFileDownloadList.Add(dictionary);
                    }
                }
            }
        }

        // 文件列表取消全选
        private void all_cancel_select_button_Click(object sender, EventArgs e)
        {
            int count = this.file_list_dataGridView.Rows.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    bool selected = (bool)this.file_list_dataGridView.Rows[i].Cells[0].EditedFormattedValue;
                    if (selected)
                    {
                        this.file_list_dataGridView.Rows[i].Cells[0].Value = false;
                    }
                }
            }
        }

        // 列表文件全选下载(全部下载则不能进行删除)
        private void all_select_download_button_Click(object sender, EventArgs e)
        {
            // 判断文件列表是否为空
            int count = this.file_list_dataGridView.Rows.Count;
            if (count == 0)
            {
                MessageBox.Show("列表为空，无法进行下载", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            for (int i = 0; i < this.file_list_dataGridView.Rows.Count; i++)
            {
                string taskStatus = this.file_list_dataGridView.Rows[i].Cells[6].Value.ToString();
                bool selected = (bool)this.file_list_dataGridView.Rows[i].Cells[0].EditedFormattedValue;
                // 如果当前行被选中且任务状态为已上传，此时判断是否存在于下载列表中，处于则不用添加下载列表中
                if (selected && taskStatus == "已上传")
                {
                    string originFileName = this.file_list_dataGridView.Rows[i].Cells[2].Value.ToString();

                    // 判断选中的文件是否处于下载中，处于下载中则无法删除
                    bool isDownload = false;
                    for (int j = 0; j < this.file_download_list_dataGridView.Rows.Count; j++)
                    {
                        // 待下载文件名称
                        string downloadFileName = this.file_download_list_dataGridView.Rows[j].Cells[1].Value.ToString();
                        if (originFileName == downloadFileName)
                        {
                            // 文件名称已匹配，且文件已处于下载列表中，则无需再次添加至下载列表，设置isDownload为true
                            isDownload = true;
                            break;
                        }
                    }

                    if (!isDownload)
                    {
                        int index = this.file_download_list_dataGridView.Rows.Count;
                        this.file_download_list_dataGridView.Rows.Add();
                        this.file_download_list_dataGridView.Rows[index].Cells[1].Value = originFileName;
                        this.file_download_list_dataGridView.Rows[index].Cells[2].Value = getFileSize(long.Parse(this.file_list_dataGridView.Rows[i].Cells[4].Value.ToString()));
                        this.file_download_list_dataGridView.Rows[index].Cells[3].Value = "待下载";
                        this.file_download_list_dataGridView.Rows[index].Cells[8].Value = this.file_list_dataGridView.Rows[i].Cells[9].Value.ToString();
                        this.file_download_list_dataGridView.Rows[index].Cells[12].Value = NetServiceContext.globalDownloadPath + originFileName;
                        this.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ]  [ " + originFileName + " ] 成功添加至下载列表\r\n");
                        this.file_download_list_dataGridView.Rows[index].Cells[13].Value = long.Parse(this.file_list_dataGridView.Rows[i].Cells[4].Value.ToString());
                    }
                }
            }

            if (this.file_download_list_dataGridView.Rows.Count == 0)
            {
                MessageBox.Show("请在列表中勾选需要下载的文件", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            this.personal_file_tabPage.SelectedTab = this.personal_file_tabPage.TabPages["tabPage3"];
        }

        // 列表文件全部删除(全部删除则不能进行下载)
        private void all_select_delete_button_Click(object sender, EventArgs e)
        {
            // 判断文件列表是否为空
            int count = this.file_list_dataGridView.Rows.Count;
            if (count == 0)
            {
                MessageBox.Show("列表为空，无法进行删除", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            // 追加需要删除的文件到personalFileDeleteList
            int selectCount = 0;
            if (count > 0)
            {
                // 清空personalFileDeleteList中的记录,这样personalFileDeleteList一定记录的是某个时刻file_list_dataGridView需要待删除记录
                personalFileDeleteList.Clear();

                for (int i = 0; i < this.file_list_dataGridView.Rows.Count; i++)
                {
                    string taskStatus = this.file_list_dataGridView.Rows[i].Cells[6].Value.ToString();
                    bool selected = (bool)this.file_list_dataGridView.Rows[i].Cells[0].EditedFormattedValue;
                    // 如果当前行被选中且任务状态为删除中，则当前行无需追加至personalFileDeleteList列表，已经处于删除中
                    if (selected && taskStatus == "已上传")
                    {
                        string originFileName = this.file_list_dataGridView.Rows[i].Cells[2].Value.ToString();

                        // 判断选中的文件是否处于下载中，处于下载中则无法删除，如果处于待下载状态则可以进行删除
                        bool isDownload = false;
                        for (int j = 0; j < this.file_download_list_dataGridView.Rows.Count; j++)
                        {
                            // 待下载文件名称
                            string downloadFileName = this.file_download_list_dataGridView.Rows[j].Cells[1].Value.ToString();
                            string status = this.file_download_list_dataGridView.Rows[j].Cells[3].Value.ToString();
                            if (originFileName == downloadFileName && status == "下载中...")
                            {
                                // 文件名称已匹配，且文件处于下载中，则不能删除，设置isDownload为true
                                isDownload = true;
                                break;
                            }
                        }

                        if (!isDownload)
                        {
                            // 只追加选中的行文件
                            long fileId = long.Parse(this.file_list_dataGridView.Rows[i].Cells[4].Value.ToString());
                            Dictionary<string, object> dictionary = new Dictionary<string, object>();
                            dictionary.Add("fileId", fileId);
                            personalFileDeleteList.Add(dictionary);

                            selectCount++;
                        }
                    }
                }
            }

            if (personalFileDeleteList.Count == 0)
            {
                MessageBox.Show("请在列表中至少选中一个文件后进行删除", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            if (selectCount == 0)
            {
                MessageBox.Show("所有文件已处于任务中,无需重复删除", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            // 执行删除
            if (MessageBox.Show("确定删除吗？删除后将不可恢复", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
            {
                // 全部删除时禁止其他按钮对表格进行操作
                all_select_button.Enabled = false;
                all_cancel_select_button.Enabled = false;
                all_select_download_button.Enabled = false;
                all_select_delete_button.Enabled = false;
                all_file_refresh_button.Enabled = false;

                // 执行批量删除
                for (int i = 0; i < personalFileDeleteList.Count; i++)
                {
                    long id = long.Parse(personalFileDeleteList[i]["fileId"].ToString());
                    NetServiceContext.deleteFile(id);
                }
            }
        }

        // 列表文件刷新
        private void all_file_refresh_button_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            if (null != currentSelectedNode)
            {
                FileDto fileDto = (FileDto)currentSelectedNode.Tag;
                this.folder_create_time_label.Visible = true;
                this.folder_create_time_label.Text = Utils.ToDateTime(fileDto.getGmtCreate()).ToString();
                this.folder_create_path_label.Visible = true;
                this.folder_create_path_label.Text = fileDto.getFilePath();
                this.file_sum_count_label.Visible = true;
                this.file_sum_count_label.Text = "0";

                NetServiceContext.getFileList(((FileDto)currentSelectedNode.Tag).getId(), currentPage, pageSize);
            }
        }




        // 文件上传列表中点击上传、取消、删除(删除只是删除未上传和上传成功的记录，并不会真正影响文件)
        private void file_upload_list_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int CIndex = e.ColumnIndex;
            string tag = this.file_upload_list_dataGridView.CurrentRow.Cells[9].Value.ToString();
            string waitFileName = this.file_upload_list_dataGridView.CurrentRow.Cells[1].Value.ToString();
            string taskStatus = this.file_upload_list_dataGridView.CurrentRow.Cells[4].Value.ToString();

            // 上传
            if (CIndex == 6) // 上传
            {
                if (taskStatus == "上传中..." || taskStatus == "上传成功")
                {
                    MessageBox.Show("文件 [ " + waitFileName + " ] " + taskStatus + "，请勿重复上传！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                // 设置当前行上传文件处于上传中
                this.file_upload_list_dataGridView.CurrentRow.Cells[4].Value = "上传中...";

                // 获取当前行上传文件对应的map信息
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                object obj = this.file_upload_list_dataGridView.CurrentRow.Cells[11].Value;
                if (obj == null)
                {
                    // 文件流对象为空，说明文件上传过程被取消，所以当前行的文件流对象fileStream为null,此时重新构建文件流对象
                    FileStream fileStream = new FileStream(this.file_upload_list_dataGridView.CurrentRow.Cells[13].Value.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
                    this.file_upload_list_dataGridView.CurrentRow.Cells[11].Value = fileStream;
                    dictionary.Add("fileStream", fileStream);
                }
                else
                {
                    dictionary.Add("fileStream", (FileStream)obj);
                }

                dictionary.Add("fileSize", long.Parse(this.file_upload_list_dataGridView.CurrentRow.Cells[15].Value.ToString()));
                dictionary.Add("fileName", waitFileName);
                dictionary.Add("selectFilePath", this.file_upload_list_dataGridView.CurrentRow.Cells[13].Value.ToString());
                dictionary.Add("uploadFolderPath", this.file_upload_list_dataGridView.CurrentRow.Cells[2].Value.ToString());
                dictionary.Add("launchUserName", commonRes.getUserName());
                dictionary.Add("tag", this.file_upload_list_dataGridView.CurrentRow.Cells[9].Value.ToString());
                dictionary.Add("fileStatus", "NO_UPLOAD");
                dictionary.Add("pid", this.file_upload_list_dataGridView.CurrentRow.Cells[14].Value.ToString());
                this.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 正在上传 [ " + waitFileName + " ] 文件, 上传路径 [" + dictionary["uploadFolderPath"].ToString() + "]\r\n");

                // 构造异步文件上传任务,将上传任务与当前行进行挂钩
                AsyncPersonalFileUploadHelper helper = new AsyncPersonalFileUploadHelper(((DataGridViewProgressBarCell)this.file_upload_list_dataGridView.CurrentRow.Cells[5]),
                    this.file_upload_list_dataGridView.CurrentRow, dictionary);
                this.file_upload_list_dataGridView.CurrentRow.Cells[10].Value = helper;
                uploadHelper.Add(helper);
                // 删除会用到11列的dictionary用于判断当前行的文件是否上传成功来决定删除
                this.file_upload_list_dataGridView.CurrentRow.Cells[12].Value = dictionary;
                helper.Do();

            }

            // 取消
            if (CIndex == 7) // 取消 （未上传或上传中方可取消）
            {
                if (taskStatus == "上传成功" || taskStatus == "上传失败")
                {
                    MessageBox.Show("文件 [ " + waitFileName + " ] 未处于上传中，无需取消！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                // 获取当前行文件上传任务并自行任务终止
                if (this.file_upload_list_dataGridView.CurrentRow.Cells[10].Value != null)
                {
                    AsyncPersonalFileUploadHelper helper = (AsyncPersonalFileUploadHelper)this.file_upload_list_dataGridView.CurrentRow.Cells[10].Value;
                    if (helper.Bg_Worker.IsBusy && !helper.Bg_Worker.CancellationPending)
                    {
                        helper.Bg_Worker.CancelAsync();
                    }
                }
            }

            // 删除
            if (CIndex == 8) // 删除,只是删除行记录
            {
                if (taskStatus == "上传中...")
                {
                    MessageBox.Show("文件 [ " + waitFileName + " ] 上传中，暂时无法删除，请先取消或是等待其上传完成后删除！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                this.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 已删除 [ " + waitFileName + " ] 待上传文件\r\n");
                this.file_upload_list_dataGridView.Rows.Remove(this.file_upload_list_dataGridView.CurrentRow);
            }
        }

        // 文件上传列表全选上传
        private void file_upload_all_button_Click(object sender, EventArgs e)
        {
            if (this.file_upload_list_dataGridView.Rows.Count == 0)
            {
                MessageBox.Show("待上传文件列表为空，请先选取要上传的文件！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            // 遍历当前表格每个记录
            for (int i = 0; i < this.file_upload_list_dataGridView.Rows.Count; i++)
            {
                string taskStatus = this.file_upload_list_dataGridView.Rows[i].Cells[4].Value.ToString();
                if (taskStatus == "未上传" || taskStatus == "上传失败")
                {
                    string tag = this.file_upload_list_dataGridView.Rows[i].Cells[9].Value.ToString();
                    string waitFileName = this.file_upload_list_dataGridView.Rows[i].Cells[1].Value.ToString();

                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    // 获取文件流对象
                    object obj = this.file_upload_list_dataGridView.Rows[i].Cells[11].Value;
                    if (obj == null)
                    {
                        // 文件流对象为空，说明文件上传过程被取消，所以当前行的文件流对象fileStream为null,此时重新构建文件流对象
                        FileStream fileStream = new FileStream(this.file_upload_list_dataGridView.Rows[i].Cells[13].Value.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
                        this.file_upload_list_dataGridView.Rows[i].Cells[11].Value = fileStream;
                        dictionary.Add("fileStream", fileStream);
                    }
                    else
                    {
                        dictionary.Add("fileStream", (FileStream)obj);
                    }

                    dictionary.Add("fileSize", long.Parse(this.file_upload_list_dataGridView.Rows[i].Cells[15].Value.ToString()));
                    dictionary.Add("fileName", waitFileName);
                    dictionary.Add("selectFilePath", this.file_upload_list_dataGridView.Rows[i].Cells[13].Value.ToString());
                    dictionary.Add("uploadFolderPath", this.file_upload_list_dataGridView.Rows[i].Cells[2].Value.ToString());
                    dictionary.Add("launchUserName", commonRes.getUserName());
                    dictionary.Add("tag", this.file_upload_list_dataGridView.Rows[i].Cells[9].Value.ToString());
                    dictionary.Add("pid", this.file_upload_list_dataGridView.Rows[i].Cells[14].Value.ToString());
                    dictionary.Add("fileStatus", "NO_UPLOAD");

                    this.file_upload_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 正在上传 [ " + waitFileName + " ] 文件, 上传路径 [" + ((FileDto)currentSelectedNode.Tag).getFilePath() + "]\r\n");
                    this.file_upload_list_dataGridView.Rows[i].Cells[4].Value = "上传中...";
                    // 删除会用到11列的dictionary用于判断当前行的文件是否上传成功来决定删除
                    this.file_upload_list_dataGridView.Rows[i].Cells[12].Value = dictionary;

                    // 构造异步文件上传任务,将上传任务与当前行进行挂钩
                    AsyncPersonalFileUploadHelper helper = new AsyncPersonalFileUploadHelper(((DataGridViewProgressBarCell)this.file_upload_list_dataGridView.Rows[i].Cells[5]),
                        this.file_upload_list_dataGridView.Rows[i], dictionary);
                    this.file_upload_list_dataGridView.Rows[i].Cells[10].Value = helper;
                    uploadHelper.Add(helper);

                    helper.Do();
                    Thread.Sleep(10);
                }
            }
        }

        // 文件上传列表全部清空
        private void file_upload_clear_button_Click(object sender, EventArgs e)
        {
            fileUploadClear();
        }

        private void fileUploadClear()
        {
            // 清空条件，清空表格处于未上传或上传完成的内容，同步清空personalFileUploadList集合中任务fileStatus状态处于WellDone的任务
            if (this.file_upload_list_dataGridView.Rows.Count > 0)
            {
                for (int i = 0; i < this.file_upload_list_dataGridView.Rows.Count; i++)
                {
                    string tag = this.file_upload_list_dataGridView.Rows[i].Cells[9].Value.ToString();
                    string taskStatus = this.file_upload_list_dataGridView.Rows[i].Cells[4].Value.ToString();
                    if (taskStatus == "未上传" || taskStatus == "上传成功")
                    {
                        AsyncPersonalFileUploadHelper helper = (AsyncPersonalFileUploadHelper)this.file_upload_list_dataGridView.Rows[i].Cells[10].Value;
                        Dictionary<string, object> dictionary = (Dictionary<string, object>)this.file_upload_list_dataGridView.Rows[i].Cells[12].Value;
                        if ((dictionary["fileStatus"].ToString() == "NO_UPLOAD" || dictionary["fileStatus"].ToString() == "WellDone"))
                        {
                            this.file_upload_list_dataGridView.Rows.RemoveAt(i);
                            uploadHelper.Remove(helper);
                            i--;
                        }
                    }
                }
            }
        }

        // 网盘树中点击上传文件传送处理
        private void sendUploadFileHandler()
        {
            // 打开文件选择弹出框
            DialogResult result = DialogResult.Cancel;
            Thread openFileDialogThread = new Thread((ThreadStart)(() =>
            {
                result = this.openFileDialog.ShowDialog();
            }));
            openFileDialogThread.SetApartmentState(ApartmentState.STA);
            openFileDialogThread.Start();
            openFileDialogThread.Join();

            // 获取选择结果
            if (result == DialogResult.OK)
            {
                // 获取文件名称展示
                string[] safeFileNames = this.openFileDialog.SafeFileNames;
                string[] files = new string[safeFileNames.Length];
                if ((files = this.openFileDialog.FileNames).Length > 0)
                {
                    int index = 0;
                    // 已经有记录，则进行追加
                    if (this.file_upload_list_dataGridView.Rows.Count > 0)
                    {
                        index = this.file_upload_list_dataGridView.Rows.Count;
                        for (int i = 0; i < files.Length; i++)
                        {
                            this.file_upload_list_dataGridView.Rows.Add();
                            this.file_upload_list_dataGridView.Rows[index].Cells[0].Value = index + 1;
                            this.file_upload_list_dataGridView.Rows[index].Cells[1].Value = safeFileNames[i];
                            this.file_upload_list_dataGridView.Rows[index].Cells[2].Value = ((FileDto)currentSelectedNode.Tag).getFilePath();
                            FileStream fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                            this.file_upload_list_dataGridView.Rows[index].Cells[3].Value = getFileSize(fileStream.Length);
                            this.file_upload_list_dataGridView.Rows[index].Cells[4].Value = "未上传";
                            // TAG列, 标识当前我文件  ((FileDto)currentSelectedNode.Tag).getId();
                            this.file_upload_list_dataGridView.Rows[index].Cells[9].Value = System.Guid.NewGuid().ToString("N");
                            this.file_upload_list_dataGridView.Rows[index].Cells[11].Value = fileStream;
                            this.file_upload_list_dataGridView.Rows[index].Cells[13].Value = files[i];
                            this.file_upload_list_dataGridView.Rows[index].Cells[14].Value = ((FileDto)currentSelectedNode.Tag).getId();
                            this.file_upload_list_dataGridView.Rows[index].Cells[15].Value = fileStream.Length.ToString();
                            index = index + 1;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            this.file_upload_list_dataGridView.Rows.Add();
                            this.file_upload_list_dataGridView.Rows[index].Cells[0].Value = index + 1;
                            this.file_upload_list_dataGridView.Rows[index].Cells[1].Value = safeFileNames[i];
                            this.file_upload_list_dataGridView.Rows[index].Cells[2].Value = ((FileDto)currentSelectedNode.Tag).getFilePath();
                            FileStream fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                            this.file_upload_list_dataGridView.Rows[index].Cells[3].Value = getFileSize(fileStream.Length);
                            this.file_upload_list_dataGridView.Rows[index].Cells[4].Value = "未上传";
                            // TAG列, 标识当前我文件
                            this.file_upload_list_dataGridView.Rows[index].Cells[9].Value = System.Guid.NewGuid().ToString("N");
                            this.file_upload_list_dataGridView.Rows[index].Cells[11].Value = fileStream;
                            this.file_upload_list_dataGridView.Rows[index].Cells[13].Value = files[i];
                            this.file_upload_list_dataGridView.Rows[index].Cells[14].Value = ((FileDto)currentSelectedNode.Tag).getId();
                            this.file_upload_list_dataGridView.Rows[index].Cells[15].Value = fileStream.Length.ToString();
                            index = index + 1;
                        }
                    }
                }

                // 动态跳转到上传列表tab页
                this.personal_file_tabPage.SelectedTab = this.personal_file_tabPage.TabPages["tabPage2"];
            }
        }

        private string getFileSize(long fileSize)
        {
            // 1GB = 1024MB = 1024 * 1024KB = 1024 * 1024 * 1024B
            string fileLength = "";

            if (fileSize < Math.Pow(1024, 1))
            {
                // B
                fileLength = fileSize.ToString() + "B";
            }

            if (Math.Pow(1024, 1) < fileSize && fileSize <= Math.Pow(1024, 2))
            {
                // 1KB < x < 1024KB
                fileLength = Math.Round(fileSize / Math.Pow(1024, 1)).ToString() + "KB";
            }

            if (Math.Pow(1024, 2) < fileSize && fileSize <= Math.Pow(1024, 3))
            {
                // 1MB < x < 1024MB
                fileLength = Math.Round(fileSize / Math.Pow(1024, 2)).ToString() + "MB";
            }

            if (Math.Pow(1024, 3) < fileSize && fileSize <= Math.Pow(1024, 4))
            {
                // 1GB < x < 1024GB
                fileLength = Math.Round(fileSize / Math.Pow(1024, 3)).ToString() + "GB";
            }

            return fileLength;
        }


        // 文件下载列表中点击下载、取消、删除(删除只是删除未下载和下载成功的记录，并不会真正影响文件)
        private void file_download_list_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //FolderBrowserDialog
            int CIndex = e.ColumnIndex;
            string tag = this.file_download_list_dataGridView.CurrentRow.Cells[8].Value.ToString();
            string waitFileName = this.file_download_list_dataGridView.CurrentRow.Cells[1].Value.ToString();
            string taskStatus = this.file_download_list_dataGridView.CurrentRow.Cells[3].Value.ToString();

            // 下载
            if (CIndex == 5)
            {
                if (taskStatus == "下载中..." || taskStatus == "下载成功")
                {
                    MessageBox.Show("文件 [ " + waitFileName + " ] " + taskStatus + "，请勿重复下载！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                // 设置当前行下载文件处于下载中
                this.file_download_list_dataGridView.CurrentRow.Cells[3].Value = "下载中...";

                // 获取当前行下载文件对应的map信息
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                // 获取文件流对象
                object obj = this.file_download_list_dataGridView.CurrentRow.Cells[10].Value;
                if (obj == null)
                {
                    // 文件流对象为空，说明文件下载过程被取消，所以当前行的文件流对象fileStream为null,此时重新构建文件流对象
                    FileStream fileStream = new FileStream(this.file_download_list_dataGridView.CurrentRow.Cells[12].Value.ToString(), FileMode.Create, FileAccess.Write);
                    this.file_download_list_dataGridView.CurrentRow.Cells[10].Value = fileStream;
                    dictionary.Add("fileStream", fileStream);
                }
                else
                {
                    dictionary.Add("fileStream", (FileStream)obj);
                }
                dictionary.Add("fileSize", long.Parse(this.file_download_list_dataGridView.CurrentRow.Cells[13].Value.ToString()));
                dictionary.Add("fileName", waitFileName);
                dictionary.Add("downloadPath", this.file_download_list_dataGridView.CurrentRow.Cells[12].Value.ToString());
                dictionary.Add("launchUserName", commonRes.getUserName());
                dictionary.Add("tag", this.file_download_list_dataGridView.CurrentRow.Cells[8].Value.ToString());
                dictionary.Add("fileStatus", "NO_DOWNLOAD");
                this.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 正在下载 [ " + waitFileName + " ] 文件, 下载路径 [" + (NetServiceContext.globalDownloadPath + "\\" + waitFileName) + "]\r\n");

                // 构造异步文件上传任务,将上传任务与当前行进行挂钩
                AsyncPersonalFileDownloadHelper helper = new AsyncPersonalFileDownloadHelper(((DataGridViewProgressBarCell)this.file_download_list_dataGridView.CurrentRow.Cells[4]),
                    this.file_download_list_dataGridView.CurrentRow, dictionary);
                this.file_download_list_dataGridView.CurrentRow.Cells[9].Value = helper;
                downloadHelper.Add(helper);
                // 删除会用到11列的dictionary用于判断当前行的文件是否上传成功来决定删除
                this.file_download_list_dataGridView.CurrentRow.Cells[11].Value = dictionary;
                helper.Do();
            }

            // 取消
            if (CIndex == 6) // 取消 （未上传或上传中方可取消）
            {
                if (taskStatus == "下载成功" || taskStatus == "下载失败")
                {
                    MessageBox.Show("文件 [ " + waitFileName + " ] 未处于下载中，无需取消！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                // 获取当前行文件上传任务并自行任务终止
                if (this.file_download_list_dataGridView.CurrentRow.Cells[9].Value != null)
                {
                    AsyncPersonalFileDownloadHelper helper = (AsyncPersonalFileDownloadHelper)this.file_download_list_dataGridView.CurrentRow.Cells[9].Value;
                    if (helper.Bg_Worker.IsBusy && !helper.Bg_Worker.CancellationPending)
                    {
                        helper.Bg_Worker.CancelAsync();
                    }
                }
            }

            // 删除
            if (CIndex == 7) // 删除,只是删除行记录
            {
                if (taskStatus == "下载中...")
                {
                    MessageBox.Show("文件 [ " + waitFileName + " ] 下载中，暂时无法删除，请先取消或是等待其下载完成后删除！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                this.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 已删除 [ " + waitFileName + " ] 待下载文件\r\n");
                this.file_download_list_dataGridView.Rows.Remove(this.file_download_list_dataGridView.CurrentRow);
            }
        }

        private void file_download_all_button_Click(object sender, EventArgs e)
        {
            // 判断下载列表是否为空
            if (this.file_download_list_dataGridView.Rows.Count == 0)
            {
                MessageBox.Show("下载列表为空！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            // 遍历当前表格每个记录,判断是否处于下载中,下载中过滤
            for (int i = 0; i < this.file_download_list_dataGridView.Rows.Count; i++)
            {
                string taskStatus = this.file_download_list_dataGridView.Rows[i].Cells[3].Value.ToString();
                string waitFileName = this.file_download_list_dataGridView.Rows[i].Cells[1].Value.ToString();
                if (taskStatus == "待下载" || taskStatus == "下载失败")
                {
                    // 设置当前行下载文件处于下载中
                    this.file_download_list_dataGridView.Rows[i].Cells[3].Value = "下载中...";

                    // 获取当前行下载文件对应的map信息
                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    // 获取文件流对象
                    object obj = this.file_download_list_dataGridView.Rows[i].Cells[10].Value;
                    if (obj == null)
                    {
                        // 文件流对象为空，说明文件下载过程被取消，所以当前行的文件流对象fileStream为null,此时重新构建文件流对象
                        FileStream fileStream = new FileStream(this.file_download_list_dataGridView.Rows[i].Cells[12].Value.ToString(), FileMode.Create, FileAccess.Write);
                        this.file_download_list_dataGridView.Rows[i].Cells[10].Value = fileStream;
                        dictionary.Add("fileStream", fileStream);
                    }
                    else
                    {
                        dictionary.Add("fileStream", (FileStream)obj);
                    }
                    dictionary.Add("fileSize", long.Parse(this.file_download_list_dataGridView.Rows[i].Cells[13].Value.ToString()));
                    dictionary.Add("fileName", waitFileName);
                    dictionary.Add("downloadPath", this.file_download_list_dataGridView.Rows[i].Cells[12].Value.ToString());
                    dictionary.Add("launchUserName", commonRes.getUserName());
                    dictionary.Add("tag", this.file_download_list_dataGridView.Rows[i].Cells[8].Value.ToString());
                    dictionary.Add("fileStatus", "NO_DOWNLOAD");
                    this.file_download_log_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 正在下载 [ " + waitFileName + " ] 文件, 下载路径 [" + (NetServiceContext.globalDownloadPath + "\\" + waitFileName) + "]\r\n");

                    // 构造异步文件上传任务,将上传任务与当前行进行挂钩
                    AsyncPersonalFileDownloadHelper helper = new AsyncPersonalFileDownloadHelper(((DataGridViewProgressBarCell)this.file_download_list_dataGridView.Rows[i].Cells[4]),
                        this.file_download_list_dataGridView.Rows[i], dictionary);
                    this.file_download_list_dataGridView.Rows[i].Cells[9].Value = helper;
                    downloadHelper.Add(helper);
                    // 删除会用到11列的dictionary用于判断当前行的文件是否上传成功来决定删除
                    this.file_download_list_dataGridView.Rows[i].Cells[11].Value = dictionary;
                    helper.Do();

                    Thread.Sleep(10);
                }
            }
        }

        private void file_download_clear_button_Click(object sender, EventArgs e)
        {
            fileDownloadClear();
        }

        private void fileDownloadClear()
        {
            if (this.file_download_list_dataGridView.Rows.Count > 0)
            {
                for (int i = 0; i < this.file_download_list_dataGridView.Rows.Count; i++)
                {
                    string tag = this.file_download_list_dataGridView.Rows[i].Cells[8].Value.ToString();
                    string taskStatus = this.file_download_list_dataGridView.Rows[i].Cells[3].Value.ToString();
                    if (taskStatus == "待下载" || taskStatus == "下载成功")
                    {
                        AsyncPersonalFileDownloadHelper helper = (AsyncPersonalFileDownloadHelper)this.file_download_list_dataGridView.Rows[i].Cells[9].Value;
                        this.file_download_list_dataGridView.Rows.RemoveAt(i);
                        downloadHelper.Remove(helper);
                        i--;
                        //Dictionary<string, object> dictionary = (Dictionary<string, object>)this.file_download_list_dataGridView.Rows[i].Cells[11].Value;
                        //if ((dictionary["fileStatus"].ToString() == "NO_DOWNLOAD" || dictionary["fileStatus"].ToString() == "WellDone"))
                        //{
                        //    this.file_download_list_dataGridView.Rows.RemoveAt(i);
                        //    i--;
                        //}
                    }
                }
            }
        }

        // 配置文件下载路径
        private void select_download_path_button_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择文件路径";

            // 打开文件选择弹出框
            DialogResult result = DialogResult.Cancel;
            Thread openFileDialogThread = new Thread((ThreadStart)(() =>
            {
                result = folderBrowserDialog.ShowDialog();
            }));
            openFileDialogThread.SetApartmentState(ApartmentState.STA);
            openFileDialogThread.Start();
            openFileDialogThread.Join();

            if (result == DialogResult.OK)
            {
                // 配置的文件夹路径, 所有文件都下载到一个路径
                NetServiceContext.globalDownloadPath = folderBrowserDialog.SelectedPath + "\\";
                global_download_path_label.Text = "当前下载路径: " + NetServiceContext.globalDownloadPath;
            }
        }





        private void refreshFileRefreshTree(string fileName, string filePath, string userName)
        {
            // 获取个人网盘文件夹
            // 执行退出操作，弹出登录框，重新选择用户登录
            // 获取根目录文件列表 (Assuming root ID is 0)
            NetServiceContext.getFileList(0, 1, pageSize);
        }
    }
}
