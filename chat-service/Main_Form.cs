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
        private readonly SemaphoreSlim uploadTransferSlots = new SemaphoreSlim(3, 3);
        private readonly SemaphoreSlim downloadTransferSlots = new SemaphoreSlim(3, 3);
        private readonly SemaphoreSlim fileListRequestGate = new SemaphoreSlim(1, 1);
        // 当前登录用户数据
        public CommonRes commonRes = null;

        // 当前登录用户（新协议 UserDO）
        public chat_service.protocol.UserDO currentUser = null;

        // 当前目录下已加载的文件列表（新协议），用于获取原始文件大小等信息
        private List<chat_service.protocol.NetFileDto> currentFileList = new List<chat_service.protocol.NetFileDto>();

        // 当前文件列表实际使用的目录过滤条件。null 表示“全部文件”，与树的右键操作节点分离。
        private long? currentFileListDirectoryId = null;
        private string currentFileListDirectoryName = "全部文件";

        // 文件列表请求版本；快速切换目录或翻页时，旧请求不得覆盖新结果。
        private int fileListRequestVersion = 0;

        // 当前正在查看详情的文件，避免快速切换时旧请求覆盖新选择。
        private long selectedDetailFileId = -1;

        // 当前详情对应的完整文件信息，供视频在线播放入口复用。
        private chat_service.protocol.NetFileDto selectedFileDetail = null;

        // 单窗口播放，避免重复点击为同一用户建立多个并发视频流。
        private VideoPlayerForm activeVideoPlayer = null;
        private long activeVideoFileId = -1;

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

            // 应用现代化 UI 主题（仅外观，不影响业务逻辑）
            ApplyUiTheme();
            BuildModernLayout();
        }

        public Main_Form(object obj)
        {
            InitializeComponent();

            // 应用现代化 UI 主题（仅外观，不影响业务逻辑）
            ApplyUiTheme();
            BuildModernLayout();

            // 登陆成功后持有的用户信息
            this.commonRes = (CommonRes) obj;

            // 当前对象
            main_Form = this;

            // 初始化解面数据
            this.initData();
        }

        // 新协议登录后持有用户信息
        public Main_Form(chat_service.protocol.UserDO user)
        {
            InitializeComponent();

            // 应用现代化 UI 主题（仅外观，不影响业务逻辑）
            ApplyUiTheme();
            BuildModernLayout();

            // 保存用户信息
            this.currentUser = user;
            this.commonRes = new CommonRes();
            this.commonRes.setUserName(user.UserName);
            this.commonRes.setTime(DateTime.Now.ToLocalTime().ToString());

            // 当前对象
            main_Form = this;

            // 初始化解面数据
            this.initData();

            // 使用新协议加载网盘目录树
            this.loadNetDiskTree();
        }

        // 应用现代化 UI 主题（仅外观，不影响业务逻辑）
        private void ApplyUiTheme()
        {
            UiTheme.Apply(this);

            // ---- 聊天室 ----
            UiTheme.StyleButton(exist_button, UiTheme.Kind.Default);           // 退出
            UiTheme.StyleButton(query_user_button, UiTheme.Kind.Primary);      // 查询
            UiTheme.StyleButton(refresh_button, UiTheme.Kind.Primary);         // 刷新
            UiTheme.StyleButton(add_user_button, UiTheme.Kind.Default);        // 添加好友
            UiTheme.StyleButton(all_task_begin_button, UiTheme.Kind.Success);  // 全部开始
            UiTheme.StyleButton(all_task_stop_button, UiTheme.Kind.Danger);    // 全部停止
            UiTheme.StyleButton(send_button, UiTheme.Kind.Primary);            // 发送

            // ---- 个人网盘 ----
            UiTheme.StyleButton(file_refresh_button, UiTheme.Kind.Primary);          // 刷新（网盘树）
            UiTheme.StyleButton(all_select_button, UiTheme.Kind.Default);            // 全选
            UiTheme.StyleButton(all_cancel_select_button, UiTheme.Kind.Default);     // 取消全选
            UiTheme.StyleButton(all_select_download_button, UiTheme.Kind.Primary);   // 全部下载
            UiTheme.StyleButton(all_select_delete_button, UiTheme.Kind.Danger);      // 全部删除
            UiTheme.StyleButton(all_file_refresh_button, UiTheme.Kind.Primary);      // 刷新（文件列表）
            UiTheme.StyleButton(prePage_button, UiTheme.Kind.Default);               // 上一页
            UiTheme.StyleButton(nextPage_button, UiTheme.Kind.Default);              // 下一页
            UiTheme.StyleButton(file_upload_all_button, UiTheme.Kind.Primary);       // 开始上传
            UiTheme.StyleButton(file_upload_clear_button, UiTheme.Kind.Default);     // 清空上传
            UiTheme.StyleButton(file_download_all_button, UiTheme.Kind.Primary);     // 开始下载
            UiTheme.StyleButton(file_download_clear_button, UiTheme.Kind.Default);   // 清空下载
            UiTheme.StyleButton(select_download_path_button, UiTheme.Kind.Default);  // 选择下载路径
        }

        // 初始化展示数据
        private void initData()
        {

            // 远程服务地址
            remote_address_textBox.Text = NetServiceContext.remoteServiceAddress;

            // 欢迎术语
            user_label.Text = "欢迎，" + commonRes.getUserName() + "使用，登录时间: " + commonRes.getTime();
            UpdateChatProfile(commonRes.getUserName());

            // 与服务器连接结果
            result_label.Visible = true;
            result_label.ForeColor = UiTheme.Success;
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
            date_label.ForeColor = UiTheme.Success;
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
                    UserModel userModel = new UserModel();
                    userModel.setUserName(commonRes.getUserName());
                    NetServiceContext.sendMessageNotWaiting(1, JsonConvert.SerializeObject(userModel), this);

                    releaseTaskResource();

                } else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                // 执行退出操作，弹出登录框，重新选择用户登录
                UserModel userModel = new UserModel();
                userModel.setUserName(commonRes.getUserName());
                NetServiceContext.sendMessageNotWaiting(1, JsonConvert.SerializeObject(userModel), this);

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
            RebuildConversationList();
        }

        // 好友列表选中某行触发
        private void user_list_dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex > -1)
            {
                // 或
                SelectChatContact(this.user_list_dataGridView.Rows[e.RowIndex].Cells[1].Value.ToString());
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
                this.loadNetDiskTree();
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
            // 刷新目录树，并同时加载当前用户的全部文件（loadNetDiskTree 内部会加载全部文件）
            this.loadNetDiskTree();
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

                    chat_service.protocol.NetFileDto fileDto = (chat_service.protocol.NetFileDto)currentSelectedNode.Tag;
                    this.folder_create_time_label.Visible = true;
                    this.folder_create_time_label.Text = FormatFileTime(fileDto.GmtCreated);
                    this.folder_create_path_label.Visible = true;
                    this.folder_create_path_label.Text = fileDto.FilePath;
                    this.file_sum_count_label.Visible = true;
                }
            }

            // 刷新所选中节点文件下的个人网盘列表，
            if (e.Button == MouseButtons.Left)
            {
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode CurrentNode = this.personal_file_treeView.GetNodeAt(ClickPoint);
                if (null != CurrentNode)
                {
                    // 只有确实选中目录时才重置页码；点击树空白区域不能改变当前列表状态。
                    currentPage = 1;
                    currentSelectedNode = CurrentNode;
                    chat_service.protocol.NetFileDto fileDto = (chat_service.protocol.NetFileDto)CurrentNode.Tag;
                    this.folder_create_time_label.Visible = true;
                    this.folder_create_time_label.Text = FormatFileTime(fileDto.GmtCreated);
                    this.folder_create_path_label.Visible = true;
                    this.folder_create_path_label.Text = fileDto.FilePath;
                    this.file_sum_count_label.Visible = true;
                    this.file_sum_count_label.Text = "0";
                    // 顶层目录代表“全部文件”，查询时不能携带 dirId；子目录才按目录 ID 过滤。
                    long? directoryId = GetDirectoryFilter(CurrentNode);
                    currentFileListDirectoryName = directoryId.HasValue ? fileDto.FileName : "全部文件";
                    this.loadFileList(directoryId);
                }
            }

        }

        // 将树节点转换为文件列表查询条件：顶层节点不带目录 ID，子节点按实际 ID 查询。
        private long? GetDirectoryFilter(TreeNode node)
        {
            if (node == null || node.Parent == null) return null;
            chat_service.protocol.NetFileDto directory = node.Tag as chat_service.protocol.NetFileDto;
            return directory != null && directory.Id > 0 ? (long?)directory.Id : null;
        }

        // 使用新协议加载文件分页列表。directoryId 为 null 时查询当前用户的全部文件。
        private void loadFileList(long? directoryId)
        {
            selectedDetailFileId = -1;
            ResetFileDetail();

            long? queryDirectoryId = directoryId.HasValue && directoryId.Value > 0
                ? directoryId
                : (long?)null;
            bool scopeChanged = currentFileListDirectoryId != queryDirectoryId;
            currentFileListDirectoryId = queryDirectoryId;
            if (!queryDirectoryId.HasValue)
            {
                currentFileListDirectoryName = "全部文件";
                UpdateDirectoryHeader("全部文件");
            }
            else
            {
                if (scopeChanged && currentSelectedNode != null &&
                    currentSelectedNode.Tag is chat_service.protocol.NetFileDto)
                {
                    chat_service.protocol.NetFileDto selectedDirectory =
                        (chat_service.protocol.NetFileDto)currentSelectedNode.Tag;
                    if (selectedDirectory.Id == queryDirectoryId.Value &&
                        !string.IsNullOrWhiteSpace(selectedDirectory.FileName))
                    {
                        currentFileListDirectoryName = selectedDirectory.FileName;
                    }
                }
                UpdateDirectoryHeader(currentFileListDirectoryName);
            }

            string selectedDirectoryName = queryDirectoryId.HasValue
                ? currentFileListDirectoryName
                : "（全部文件）";
            int requestedPage = Math.Max(1, Main_Form.currentPage);
            Main_Form.currentPage = requestedPage;
            int requestVersion = Interlocked.Increment(ref fileListRequestVersion);
            SetFilePaginationLoading();

            Thread listThread = new Thread(() =>
            {
                try
                {
                    chat_service.protocol.FilePageResult page;
                    fileListRequestGate.Wait();
                    try
                    {
                        // SocketManager 当前按响应帧类型分发；同类列表请求必须串行，避免响应串线。
                        if (requestVersion != Volatile.Read(ref fileListRequestVersion)) return;
                        page = chat_service.protocol.DirectoryService.Shared
                            .FetchFileList(queryDirectoryId, "", requestedPage, Main_Form.pageSize);
                    }
                    finally
                    {
                        fileListRequestGate.Release();
                    }

                    if (requestVersion != Volatile.Read(ref fileListRequestVersion)) return;

                    int totalPages = page.TotalPage <= 0
                        ? 0
                        : (page.TotalPage > int.MaxValue ? int.MaxValue : (int)page.TotalPage);

                    // 删除末页最后一条记录等场景会让当前页越界，回退后重新获取有效末页。
                    if (totalPages > 0 && requestedPage > totalPages)
                    {
                        this.file_list_dataGridView.BeginInvoke(new MethodInvoker(delegate ()
                        {
                            if (requestVersion != Volatile.Read(ref fileListRequestVersion)) return;
                            Main_Form.currentPage = totalPages;
                            this.loadFileList(queryDirectoryId);
                        }));
                        return;
                    }

                    int responsePage = totalPages == 0
                        ? 1
                        : Math.Max(1, Math.Min(page.CurrentPage > 0 ? page.CurrentPage : requestedPage, totalPages));
                    int responsePageSize = page.PageSize > 0 ? page.PageSize : Main_Form.pageSize;

                    this.file_list_dataGridView.BeginInvoke(new MethodInvoker(delegate ()
                    {
                        if (requestVersion != Volatile.Read(ref fileListRequestVersion)) return;

                        this.file_list_dataGridView.Rows.Clear();
                        Main_Form.currentPage = responsePage;
                        Main_Form.sumPageCount = totalPages;
                        this.file_sum_count_label.Text = page.TotalCount.ToString();
                        UpdateFilePagination(page.TotalCount, responsePage, totalPages);

                        List<chat_service.protocol.NetFileDto> list = page.RecordList;
                        this.currentFileList = list ?? new List<chat_service.protocol.NetFileDto>();
                        if (list != null && list.Count > 0)
                        {
                            int i = 0;
                            foreach (var filedto in list)
                            {
                                this.file_list_dataGridView.Rows.Add();
                                // 复用旧列结构：0=checkbox,1=序号,2=文件名,3=文件路径,4=文件大小,5=上传时间,6=状态,7=下载按钮,9=文件标识(id)
                                this.file_list_dataGridView.Rows[i].Cells[1].Value =
                                    ((long)responsePage - 1L) * responsePageSize + i + 1L;
                                this.file_list_dataGridView.Rows[i].Cells[2].Value = filedto.FileName;
                                this.file_list_dataGridView.Rows[i].Cells["ParentDirectoryColumn"].Value =
                                    string.IsNullOrWhiteSpace(filedto.ParentDirName) ? selectedDirectoryName : filedto.ParentDirName;
                                this.file_list_dataGridView.Rows[i].Cells[3].Value = filedto.FilePath;
                                this.file_list_dataGridView.Rows[i].Cells[4].Value = filedto.FileSize != null
                                    ? getFileSize(filedto.FileSize.Value) : "-";
                                this.file_list_dataGridView.Rows[i].Cells[5].Value = FormatFileTime(filedto.GmtCreated);
                                this.file_list_dataGridView.Rows[i].Cells[6].Value = "已上传";
                                this.file_list_dataGridView.Rows[i].Cells[9].Value = filedto.Id.ToString();
                                // 实际值保持为空，以“下载”作为默认显示；这样再次选择下载也会触发操作。
                                this.file_list_dataGridView.Rows[i].Cells["ModernFileActionColumn"].Value = null;
                                i++;
                            }
                        }
                    }));
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new MethodInvoker(delegate ()
                    {
                        if (requestVersion != Volatile.Read(ref fileListRequestVersion)) return;
                        ShowFilePaginationError();
                        this.result_label.Text = "文件列表加载失败: " + ex.Message;
                    }));
                }
            });
            listThread.IsBackground = true;
            listThread.Start();
        }

        // 格式化文件时间戳为本地日期字符串
        private string FormatFileTime(long? timestamp)
        {
            if (timestamp == null || timestamp.Value <= 0) return "-";
            try
            {
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp.Value).ToLocalTime();
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
            catch
            {
                return "-";
            }
        }

        // 添加文件夹
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                // 弹框创建文件夹
                File_Create_Form file_Create_Form = new File_Create_Form(currentSelectedNode, commonRes.getUserName(), "CREATE");
                if (file_Create_Form.ShowDialog(this) == DialogResult.OK)
                {
                    loadNetDiskTree();
                }
            }
        }

        // 删除文件夹
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null && currentSelectedNode.Tag is chat_service.protocol.NetFileDto)
            {
                chat_service.protocol.NetFileDto dir = (chat_service.protocol.NetFileDto)currentSelectedNode.Tag;
                DialogResult dr = MessageBox.Show("确定删除文件夹 [ " + dir.FileName + " ] 吗？", "系统提示",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.OK)
                {
                    try
                    {
                        chat_service.protocol.DirectoryService.Shared.DeleteDirectory(dir.Id);
                        // 刷新目录树
                        this.loadNetDiskTree();
                        currentSelectedNode = null;
                        this.result_label.Text = "文件夹删除成功";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("文件夹删除失败: " + ex.Message, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
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
                if (file_Create_Form.ShowDialog(this) == DialogResult.OK)
                {
                    loadNetDiskTree();
                }
            }
        }

        // 上传文件
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (currentSelectedNode != null)
            {
                this.initOpenFileDialog();
                chat_service.protocol.NetFileDto fileDto = (chat_service.protocol.NetFileDto)currentSelectedNode.Tag;
                this.sendUploadFileHandler();
            }
        }

        // 文件列表点击checkBox列
        private void file_list_dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= this.file_list_dataGridView.Rows.Count) return;
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

            string fileIdText = Convert.ToString(this.file_list_dataGridView.Rows[e.RowIndex].Cells[9].Value);
            long fileId;
            if (long.TryParse(fileIdText, out fileId))
            {
                LoadFileDetail(fileId, Convert.ToString(this.file_list_dataGridView.Rows[e.RowIndex].Cells[2].Value));
            }
        }

        private void LoadFileDetail(long fileId, string fileName)
        {
            selectedDetailFileId = fileId;
            selectedFileDetail = null;
            ShowFileDetailLoading(fileName);
            Thread detailThread = new Thread(() =>
            {
                try
                {
                    chat_service.protocol.NetFileDto detail = chat_service.protocol.DirectoryService.Shared.FetchFileDetail(fileId);
                    if (this.IsDisposed || !this.IsHandleCreated) return;
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        if (selectedDetailFileId == fileId)
                        {
                            ShowFileDetail(detail);
                            LoadFilePreview(detail);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    if (this.IsDisposed || !this.IsHandleCreated) return;
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        if (selectedDetailFileId == fileId) ShowFileDetailError(ex.Message);
                    }));
                }
            });
            detailThread.IsBackground = true;
            detailThread.Start();
        }

        private void LoadFilePreview(chat_service.protocol.NetFileDto detail)
        {
            if (detail == null || selectedDetailFileId != detail.Id) return;
            string extension = Path.GetExtension(detail.FileName ?? "").ToLowerInvariant();
            bool isImage = extension == ".jpg" || extension == ".jpeg" || extension == ".png"
                || extension == ".gif";
            bool isVideo = chat_service.protocol.MediaPlaybackService.IsPlayableVideo(detail.FileName);
            if (!isImage && !isVideo)
            {
                ShowFilePreviewUnavailable("此文件暂不支持预览");
                return;
            }
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.TransferToken))
            {
                ShowFilePreviewUnavailable("登录凭据缺失，无法加载预览");
                return;
            }

            ShowFilePreviewLoading(isVideo);
            long previewFileId = detail.Id;
            string previewFileName = detail.FileName;
            string transferToken = currentUser.TransferToken;
            Thread previewThread = new Thread(() =>
            {
                Image preview = null;
                try
                {
                    preview = FetchMediaThumbnail(previewFileId, transferToken);
                    if (this.IsDisposed || !this.IsHandleCreated)
                    {
                        if (preview != null) preview.Dispose();
                        return;
                    }
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        if (selectedDetailFileId == previewFileId)
                        {
                            ShowFilePreview(preview, isVideo, previewFileName);
                            preview = null;
                        }
                        if (preview != null) preview.Dispose();
                    }));
                }
                catch (Exception ex)
                {
                    if (preview != null) preview.Dispose();
                    if (this.IsDisposed || !this.IsHandleCreated) return;
                    this.BeginInvoke(new MethodInvoker(delegate
                    {
                        if (selectedDetailFileId == previewFileId)
                        {
                            ShowFilePreviewUnavailable("预览加载失败，请稍后重试");
                            this.result_label.Text = "文件预览加载失败: " + ex.Message;
                        }
                    }));
                }
            });
            previewThread.IsBackground = true;
            previewThread.Start();
        }

        private Image FetchMediaThumbnail(long fileId, string transferToken)
        {
            string baseAddress = ResolveMediaPreviewBaseAddress();
            string requestUrl = baseAddress.TrimEnd('/') + "/media/thumbnail/" + fileId;
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(requestUrl);
            request.Method = "GET";
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;
            request.Headers[System.Net.HttpRequestHeader.Authorization] = "Bearer " + transferToken;
            using (System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (Image source = Image.FromStream(stream))
            {
                return new Bitmap(source);
            }
        }

        private string ResolveMediaPreviewBaseAddress()
        {
            return chat_service.protocol.MediaPlaybackService.ResolveBaseAddress();
        }

        private void OpenSelectedVideoPlayer()
        {
            chat_service.protocol.NetFileDto detail = selectedFileDetail;
            if (detail == null || detail.Id <= 0
                || !chat_service.protocol.MediaPlaybackService.IsPlayableVideo(detail.FileName))
            {
                this.result_label.Text = "请选择支持在线播放的视频文件。";
                return;
            }
            if (detail.IsDeleted || !detail.IsExistBoolean)
            {
                this.result_label.Text = "视频文件不存在，无法在线播放。";
                return;
            }
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.TransferToken))
            {
                this.result_label.Text = "登录凭据缺失，请重新登录后再播放。";
                return;
            }

            try
            {
                if (activeVideoPlayer != null && !activeVideoPlayer.IsDisposed)
                {
                    if (activeVideoFileId == detail.Id)
                    {
                        activeVideoPlayer.Activate();
                        return;
                    }
                    activeVideoPlayer.Close();
                }

                VideoPlayerForm player = new VideoPlayerForm(
                    detail.Id,
                    detail.FileName,
                    currentUser.TransferToken,
                    chat_service.protocol.MediaPlaybackService.ResolveBaseAddress());
                activeVideoPlayer = player;
                activeVideoFileId = detail.Id;
                player.FormClosed += delegate
                {
                    if (ReferenceEquals(activeVideoPlayer, player))
                    {
                        activeVideoPlayer = null;
                        activeVideoFileId = -1;
                    }
                };
                player.Show(this);
            }
            catch (Exception ex)
            {
                activeVideoPlayer = null;
                activeVideoFileId = -1;
                this.result_label.Text = "播放器启动失败: " + ex.Message;
            }
        }
         
        // 兼容旧文件列表按钮；现代布局使用统一的下拉操作列。
        private void file_list_dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == 7) HandleModernFileAction(e.RowIndex, "下载");
            else if (e.ColumnIndex == 8) HandleModernFileAction(e.RowIndex, "删除");
        }

        private void HandleModernFileAction(int rowIndex, string action)
        {
            if (rowIndex < 0 || rowIndex >= file_list_dataGridView.Rows.Count) return;
            DataGridViewRow row = file_list_dataGridView.Rows[rowIndex];
            string tag = Convert.ToString(row.Cells[9].Value);
            string fileName = Convert.ToString(row.Cells[2].Value);
            string taskStatus = Convert.ToString(row.Cells[6].Value);
            long fileId;
            if (!long.TryParse(tag, out fileId))
            {
                MessageBox.Show("文件标识无效！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (action == "下载")
            {
                long fileSize = 0;
                chat_service.protocol.NetFileDto target = this.currentFileList.FirstOrDefault(f => f.Id == fileId);
                if (target != null && target.FileSize.HasValue) fileSize = target.FileSize.Value;
                DownloadSingleFile(fileId, fileName, fileSize);
                ShowTransferCenter(false);
                return;
            }

            if (action == "删除")
            {
                if (taskStatus == "删除成功")
                {
                    MessageBox.Show("文件 [ " + fileName + " ] 已删除，无需再次删除！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (IsFileDownloading(fileName))
                {
                    MessageBox.Show("文件 [ " + fileName + " ] 正在下载，暂时无法删除。", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show("确定删除文件 [ " + fileName + " ] 吗？删除后将不可恢复。", "系统提示",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                try
                {
                    chat_service.protocol.DirectoryService.Shared.DeleteFile(fileId);
                    RefreshCurrentFileList();
                    this.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + fileName + " ] 删除成功\r\n");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("文件删除失败: " + ex.Message, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            if (action == "重命名") RenameFileFromList(fileId, fileName);
        }

        private bool IsFileDownloading(string fileName)
        {
            for (int i = 0; i < file_download_list_dataGridView.Rows.Count; i++)
            {
                string downloadFileName = Convert.ToString(file_download_list_dataGridView.Rows[i].Cells[1].Value);
                string status = Convert.ToString(file_download_list_dataGridView.Rows[i].Cells[3].Value);
                if (string.Equals(fileName, downloadFileName, StringComparison.OrdinalIgnoreCase) && status == "下载中...") return true;
            }
            return false;
        }

        private void RenameFileFromList(long fileId, string originalFileName)
        {
            if (IsFileDownloading(originalFileName))
            {
                MessageBox.Show("文件 [ " + originalFileName + " ] 正在下载，暂时无法修改名称。", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string extension = Path.GetExtension(originalFileName) ?? "";
            string originalBaseName = Path.GetFileNameWithoutExtension(originalFileName);
            string newBaseName = PromptForFileBaseName(originalBaseName, extension);
            if (newBaseName == null) return;
            newBaseName = newBaseName.Trim();
            if (newBaseName.Length == 0)
            {
                MessageBox.Show("文件名称不能为空。", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.IsNullOrEmpty(Path.GetExtension(newBaseName)) ||
                (!string.IsNullOrEmpty(extension) && newBaseName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("请只填写文件名称，不要输入文件扩展名。扩展名 " + extension + " 会自动保留。",
                    "扩展名不能修改", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (newBaseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newBaseName.EndsWith(".") || newBaseName.EndsWith(" "))
            {
                MessageBox.Show("文件名称包含无效字符，请重新输入。", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newFileName = newBaseName + extension;
            if (string.Equals(newFileName, originalFileName, StringComparison.Ordinal)) return;
            try
            {
                chat_service.protocol.DirectoryService.Shared.RenameFile(fileId, newFileName);
                RefreshCurrentFileList();
                this.message_richTextBox.AppendText("[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + originalFileName + " ] 已修改为 [ " + newFileName + " ]\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("文件名修改失败: " + ex.Message, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string PromptForFileBaseName(string currentName, string extension)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "重命名文件";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ClientSize = new Size(430, 156);
                dialog.Font = new Font("微软雅黑", 9F);

                Label hint = new Label { Text = "只修改名称，扩展名将固定保留：", Location = new Point(20, 18), AutoSize = true };
                TextBox input = new TextBox { Text = currentName, Location = new Point(20, 50), Size = new Size(300, 27) };
                Label extensionLabel = new Label
                {
                    Text = string.IsNullOrEmpty(extension) ? "（无扩展名）" : extension,
                    Location = new Point(326, 53),
                    AutoSize = true,
                    ForeColor = UiTheme.TextSecondary
                };
                Label warning = new Label { Text = "请勿在名称中输入扩展名。", Location = new Point(20, 84), AutoSize = true, ForeColor = UiTheme.Danger };
                Button cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(244, 112), Size = new Size(78, 30) };
                Button confirm = new Button { Text = "保存", DialogResult = DialogResult.OK, Location = new Point(332, 112), Size = new Size(78, 30) };
                dialog.Controls.Add(hint);
                dialog.Controls.Add(input);
                dialog.Controls.Add(extensionLabel);
                dialog.Controls.Add(warning);
                dialog.Controls.Add(cancel);
                dialog.Controls.Add(confirm);
                dialog.AcceptButton = confirm;
                dialog.CancelButton = cancel;
                dialog.Shown += delegate { input.SelectAll(); input.Focus(); };
                return dialog.ShowDialog(this) == DialogResult.OK ? input.Text : null;
            }
        }

        private void RefreshCurrentFileList()
        {
            loadFileList(GetCurrentListDirId());
        }

        // 上一页
        private void prePage_button_Click(object sender, EventArgs e)
        {
            NavigateToFilePage(currentPage - 1);
        }

        // 下一页
        private void nextPage_button_Click(object sender, EventArgs e)
        {
            NavigateToFilePage(currentPage + 1);
        }

        private void NavigateToFilePage(int targetPage)
        {
            if (sumPageCount <= 0 || targetPage < 1 || targetPage > sumPageCount || targetPage == currentPage)
                return;

            currentPage = targetPage;
            this.loadFileList(GetCurrentListDirId());
        }

        // 返回当前已经展示的查询范围，不读取可能被右键操作改变的 currentSelectedNode。
        private long? GetCurrentListDirId()
        {
            return currentFileListDirectoryId;
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

            this.ShowTransferCenter(false);
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
                            Dictionary<string, object> dictionary = new Dictionary<string, object>();
                            dictionary.Add("fileName", originFileName);
                            dictionary.Add("filePath", this.file_list_dataGridView.Rows[i].Cells[3].Value.ToString());
                            dictionary.Add("tag", this.file_list_dataGridView.Rows[i].Cells[9].Value.ToString());
                            //dictionary.Add("rowNumber", i);
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

                List<Dictionary<string, object>> deleteBatch = new List<Dictionary<string, object>>(personalFileDeleteList);
                Thread deleteThread = new Thread((ThreadStart)delegate
                {
                    Exception failure = null;
                    try
                    {
                        List<long> fileIds = deleteBatch
                            .Select(dic => Convert.ToString(dic["tag"]))
                            .Select(value => { long id; return long.TryParse(value, out id) ? (long?)id : null; })
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .ToList();
                        chat_service.protocol.DirectoryService.Shared.DeleteFiles(fileIds);
                    }
                    catch (Exception ex) { failure = ex; }
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new MethodInvoker(delegate
                    {
                        all_select_button.Enabled = true;
                        all_cancel_select_button.Enabled = true;
                        all_select_download_button.Enabled = true;
                        all_select_delete_button.Enabled = true;
                        all_file_refresh_button.Enabled = true;
                        personalFileDeleteList.Clear();
                        if (failure != null) MessageBox.Show("批量删除失败: " + failure.Message, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        else RefreshCurrentFileList();
                    }));
                });
                deleteThread.IsBackground = true;
                deleteThread.Start();
            }
        }

        // 列表文件刷新
        private void all_file_refresh_button_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            this.RefreshCurrentFileList();
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
                // 使用新协议直接上传（文件路径在 Cells[13]）
                string filePath = "";
                try { filePath = Convert.ToString(this.file_upload_list_dataGridView.CurrentRow.Cells[13].Value); } catch { }
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    MessageBox.Show("无法获取有效的本地文件路径！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                chat_service.protocol.UserDO user = this.currentUser;
                if (user == null || string.IsNullOrEmpty(user.TransferToken))
                {
                    MessageBox.Show("登录凭证缺失，请重新登录！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                long dirId = 0;
                if (currentSelectedNode != null && currentSelectedNode.Tag is chat_service.protocol.NetFileDto)
                {
                    dirId = ((chat_service.protocol.NetFileDto)currentSelectedNode.Tag).Id;
                }
                if (dirId == 0)
                {
                    MessageBox.Show("请选择目标文件夹！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                this.QueueUploadFile(filePath, dirId, user);
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
            // 说明：新协议版本中文件在选择后即立即上传，无需通过本按钮再次触发。
            MessageBox.Show("文件已在选择后自动上传，请在网盘文件列表中刷新查看结果。", "系统提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (currentSelectedNode == null)
            {
                MessageBox.Show("请先在目录树中选择目标文件夹！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
            if (result != DialogResult.OK)
            {
                return;
            }

            string[] files = this.openFileDialog.FileNames;
            if (files == null || files.Length == 0)
            {
                return;
            }

            chat_service.protocol.NetFileDto targetFolder = (chat_service.protocol.NetFileDto)currentSelectedNode.Tag;
            long dirId = targetFolder.Id;

            // 取当前登录用户信息（新协议）
            chat_service.protocol.UserDO user = this.currentUser;
            if (user == null || string.IsNullOrEmpty(user.TransferToken))
            {
                MessageBox.Show("登录凭证缺失，请重新登录！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 逐文件上传（每个文件一个独立上传任务，走断点续传协议）
            this.ShowTransferCenter(true);
            foreach (string filePath in files)
            {
                QueueUploadFile(filePath, dirId, user);
            }
        }

        // 使用新协议 FileDownloadService 下载单个文件
        private void DownloadSingleFile(long fileId, string fileName, long fileSize)
        {
            chat_service.protocol.UserDO user = this.currentUser;
            if (user == null || string.IsNullOrEmpty(user.TransferToken))
            {
                MessageBox.Show("登录凭证缺失，请重新登录！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int downloadRowIndex = EnsureDownloadTaskRow(fileId, fileName, fileSize);
            string targetPath = NetServiceContext.globalDownloadPath + "\\" + fileName;

            TransferTaskState st = GetOrCreateDownloadState(fileId, fileName, targetPath, fileSize);
            st.Cancelled = false;
            st.Cts = new CancellationTokenSource();
            st.LastProgress = 0;

            Thread dlThread = new Thread(() =>
            {
                downloadTransferSlots.Wait();
                try
                {
                    string downloadHost = NetServiceContext.remoteFileDownloadServiceAddress.Split(':')[0];
                    int downloadPort = Convert.ToInt32(NetServiceContext.remoteFileDownloadServiceAddress.Split(':')[1]);
                    string taskId = System.Guid.NewGuid().ToString("N");

                    AppendTransferLog(file_download_log_richTextBox,
                        "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 正在下载 [ " + fileName + " ] ...\r\n");

                    chat_service.protocol.FileDownloadService service = new chat_service.protocol.FileDownloadService(downloadHost, downloadPort);
                    service.DownloadFile(
                        fileId,
                        targetPath,
                        fileSize,
                        (int)user.Id,
                        user.UserName,
                        user.TransferToken,
                        taskId,
                        0,
                        (progress, speed) =>
                        {
                            st.LastProgress = (int)(progress * 100);
                            UpdateDownloadTaskRow(downloadRowIndex, "下载中...", (int)(progress * 100));
                        },
                        st.Cts.Token);

                    st.Cancelled = false;
                    UpdateDownloadTaskRow(downloadRowIndex, "下载成功", 100);

                    AppendTransferLog(file_download_log_richTextBox,
                        "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + fileName + " ] 下载完成: " + targetPath + "\r\n");
                }
                catch (OperationCanceledException)
                {
                    st.Cancelled = true;
                    UpdateDownloadTaskRow(downloadRowIndex, "已暂停", st.LastProgress);
                    AppendTransferLog(file_download_log_richTextBox,
                        "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + fileName + " ] 下载已暂停（已保留断点，可继续）\r\n");
                }
                catch (Exception ex)
                {
                    UpdateDownloadTaskRow(downloadRowIndex, "下载失败", 0);
                    AppendTransferLog(file_download_log_richTextBox,
                        "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件下载失败: " + ex.Message + "\r\n");
                    PostToUi(delegate { MessageBox.Show("文件下载失败: " + ex.Message, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); });
                }
                finally { downloadTransferSlots.Release(); }
            });
            dlThread.IsBackground = true;
            dlThread.Start();
        }

        // 使用新协议 FileTransferService 上传单个文件到指定目录
        private void QueueUploadFile(string filePath, long dirId, chat_service.protocol.UserDO user)
        {
            int uploadRowIndex = EnsureUploadTaskRow(filePath);
            ThreadPool.QueueUserWorkItem(delegate
            {
                uploadTransferSlots.Wait();
                try { UploadSingleFile(filePath, dirId, user, uploadRowIndex); }
                finally { uploadTransferSlots.Release(); }
            });
        }

        private void UploadSingleFile(string filePath, long dirId, chat_service.protocol.UserDO user, int uploadRowIndex)
        {
            TransferTaskState st = GetOrCreateUploadState(filePath, dirId);
            st.Cancelled = false;
            st.Cts = new CancellationTokenSource();
            st.LastProgress = 0;
            string fileName = System.IO.Path.GetFileName(filePath);
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    // 清理历史 FileStream 可能残留的问题（此方法不复用 FileStream）
                }

                AppendTransferLog(file_upload_log_richTextBox,
                    "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 正在上传 [ " + fileName + " ] ...\r\n");

                string uploadHost = NetServiceContext.remoteFileServiceAddress.Split(':')[0];
                int uploadPort = Convert.ToInt32(NetServiceContext.remoteFileServiceAddress.Split(':')[1]);

                chat_service.protocol.FileTransferService service = new chat_service.protocol.FileTransferService(uploadHost, uploadPort);
                // 复用任务标识：服务端按 md5+用户 落断点，暂停后再传即可从上次偏移续传。
                string taskId = string.IsNullOrEmpty(st.TaskId) ? System.Guid.NewGuid().ToString("N") : st.TaskId;
                UpdateUploadTaskRow(uploadRowIndex, "上传中...", 0);

                long? fileId = service.UploadFile(
                    filePath,
                    dirId,
                    (int)user.Id,
                    user.UserName,
                    user.TransferToken,
                    taskId,
                    (progress, speed) =>
                    {
                        st.LastProgress = (int)(progress * 100);
                        UpdateUploadTaskRow(uploadRowIndex, "上传中...", (int)(progress * 100));
                    },
                    st.Cts.Token);

                st.Cancelled = false;
                UpdateUploadTaskRow(uploadRowIndex, "上传成功", 100);

                AppendTransferLog(file_upload_log_richTextBox,
                    "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + fileName + " ] 上传完成, fileId=" + (fileId != null ? fileId.ToString() : "未知") + "\r\n");

                // 上传完成后刷新用户当前正在查看的列表；顶层视图仍保持“全部文件”查询。
                PostToUi(delegate { RefreshCurrentFileList(); });
            }
            catch (OperationCanceledException)
            {
                st.Cancelled = true;
                UpdateUploadTaskRow(uploadRowIndex, "已暂停", st.LastProgress);
                AppendTransferLog(file_upload_log_richTextBox,
                    "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件 [ " + fileName + " ] 上传已暂停（断点已保存，可继续）\r\n");
            }
            catch (Exception ex)
            {
                UpdateUploadTaskRow(uploadRowIndex, "上传失败", 0);
                AppendTransferLog(file_upload_log_richTextBox,
                    "[ " + DateTime.Now.ToLocalTime().ToString() + " ] 文件上传失败: " + ex.Message + "\r\n");
                PostToUi(delegate { MessageBox.Show("文件上传失败: " + ex.Message, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); });
            }
        }

        private int EnsureUploadTaskRow(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            for (int i = 0; i < file_upload_list_dataGridView.Rows.Count; i++)
            {
                string cur = Convert.ToString(file_upload_list_dataGridView.Rows[i].Cells[1].Value);
                string status = Convert.ToString(file_upload_list_dataGridView.Rows[i].Cells[4].Value);
                if (string.Equals(cur, fileName, StringComparison.OrdinalIgnoreCase)
                    && (status == "上传中..." || status == "已暂停" || status == "排队中")) return i;
            }
            int index = file_upload_list_dataGridView.Rows.Add();
            DataGridViewRow row = file_upload_list_dataGridView.Rows[index];
            row.Cells[0].Value = index + 1;
            row.Cells[1].Value = fileName;
            row.Cells[2].Value = filePath;
            row.Cells[3].Value = getFileSize(new FileInfo(filePath).Length);
            row.Cells[4].Value = "排队中";
            row.Cells[5].Value = 0;
            row.Cells[13].Value = filePath;
            return index;
        }

        private int EnsureDownloadTaskRow(long fileId, string fileName, long fileSize)
        {
            for (int i = 0; i < file_download_list_dataGridView.Rows.Count; i++)
            {
                if (Convert.ToString(file_download_list_dataGridView.Rows[i].Cells[8].Value) == fileId.ToString()) return i;
            }
            int index = file_download_list_dataGridView.Rows.Add();
            DataGridViewRow row = file_download_list_dataGridView.Rows[index];
            row.Cells[0].Value = index + 1;
            row.Cells[1].Value = fileName;
            row.Cells[2].Value = getFileSize(fileSize);
            row.Cells[3].Value = "下载中...";
            row.Cells[4].Value = 0;
            row.Cells[8].Value = fileId.ToString();
            row.Cells[13].Value = fileSize;
            return index;
        }

        private void UpdateUploadTaskRow(int rowIndex, string status, int progress)
        {
            PostToUi(delegate
            {
                if (rowIndex < file_upload_list_dataGridView.Rows.Count)
                {
                    file_upload_list_dataGridView.Rows[rowIndex].Cells[4].Value = status;
                    file_upload_list_dataGridView.Rows[rowIndex].Cells[5].Value = progress;
                    RefreshUnifiedTransferList();
                }
            });
        }

        private void UpdateDownloadTaskRow(int rowIndex, string status, int progress)
        {
            PostToUi(delegate
            {
                if (rowIndex < file_download_list_dataGridView.Rows.Count)
                {
                    file_download_list_dataGridView.Rows[rowIndex].Cells[3].Value = status;
                    file_download_list_dataGridView.Rows[rowIndex].Cells[4].Value = progress;
                    RefreshUnifiedTransferList();
                }
            });
        }

        private void AppendTransferLog(RichTextBox logControl, string message)
        {
            PostToUi(delegate
            {
                if (logControl != null && !logControl.IsDisposed) logControl.AppendText(message);
            });
        }

        private void PostToUi(Action action)
        {
            if (action == null || IsDisposed || Disposing) return;
            try
            {
                if (InvokeRequired)
                {
                    if (!IsHandleCreated) return;
                    BeginInvoke(new MethodInvoker(delegate
                    {
                        if (!IsDisposed && !Disposing) action();
                    }));
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
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

                // 使用新协议直接下载
                long fileId;
                if (!long.TryParse(tag, out fileId))
                {
                    MessageBox.Show("文件标识无效！", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                this.file_download_list_dataGridView.CurrentRow.Cells[3].Value = "下载中...";
                long fileSize = 0;
                try { fileSize = long.Parse(this.file_download_list_dataGridView.CurrentRow.Cells[13].Value.ToString()); } catch { }
                this.DownloadSingleFile(fileId, waitFileName, fileSize);
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
            UserModel userModel = new UserModel();
            userModel.setRefreshFile("true");
            userModel.setUserName(userName);
            userModel.setFileName(fileName);
            userModel.setFilePath(filePath);
            userModel.setCurrentPage(currentPage = 1);
            userModel.setPageSize(pageSize);
            NetServiceContext.sendMessageNotWaiting(6, JsonConvert.SerializeObject(userModel), this);
        }

        // 使用新协议加载网盘目录树
        private void loadNetDiskTree()
        {
            Thread treeThread = new Thread(() =>
            {
                try
                {
                    List<chat_service.protocol.NetFileDto> roots = chat_service.protocol.DirectoryService.Shared.LoadDirectoryTree();
                    this.personal_file_treeView.BeginInvoke(new MethodInvoker(delegate ()
                    {
                        this.personal_file_treeView.Nodes.Clear();
                        currentSelectedNode = null;
                        foreach (var dto in roots)
                        {
                            TreeNode node = BuildNetDirectoryTreeNode(dto);
                            this.personal_file_treeView.Nodes.Add(node);
                        }
                        this.personal_file_treeView.ExpandAll();

                        // 默认进入网盘即展示当前用户上传的全部文件
                        Main_Form.currentPage = 1;
                        currentFileListDirectoryName = "全部文件";
                        this.loadFileList(null);
                    }));
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new MethodInvoker(delegate ()
                    {
                        this.result_label.Text = "网盘目录树加载失败: " + ex.Message;
                    }));
                }
            });
            treeThread.IsBackground = true;
            treeThread.Start();
        }

        // 递归构建网盘目录树节点
        private TreeNode BuildNetDirectoryTreeNode(chat_service.protocol.NetFileDto dto)
        {
            TreeNode node = new TreeNode(dto.FileName ?? "");
            node.Tag = dto;
            if (dto.ChildFileList != null && dto.ChildFileList.Count > 0)
            {
                foreach (var child in dto.ChildFileList)
                {
                    node.Nodes.Add(BuildNetDirectoryTreeNode(child));
                }
            }
            return node;
        }
    }
}
