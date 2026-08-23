using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace chat_service
{
    public partial class Main_Form
    {
        private const int ModernNavigationWidth = 84;
        private const int ChatContactListWidth = 280;
        private const int DriveDirectoryTreeWidth = 370;
        private Panel modernNavPanel;
        private Button navChatButton;
        private Button navDriveButton;
        private Button navUploadButton;
        private Button navDownloadButton;
        private TabPage activityTabPage;
        private TabPage settingsTabPage;
        private Label currentDirectoryTitleLabel;
        private Label transferSummaryLabel;
        private Panel transferCenterPanel;
        private Panel transferLauncherHost;
        private Panel transferListHost;
        private Panel driveContentPanel;
        private Button transferClearButton;
        private DataGridView unifiedTransferGrid;
        private FlowLayoutPanel transferStatusFilters;
        private string activeTransferStatusFilter = "全部";
        private Panel fileDetailPanel;
        private Button fileDetailToggleButton;
        private Panel filePaginationPanel;
        private Label filePageInfoLabel;
        private Panel fileDetailContentPanel;
        private Label fileDetailPlaceholderLabel;
        private Label fileDetailNameLabel;
        private Label fileDetailTypeLabel;
        private Label fileDetailSizeLabel;
        private Label fileDetailDirectoryLabel;
        private Label fileDetailPathLabel;
        private Label fileDetailCreatedLabel;
        private Label fileDetailModifiedLabel;
        private Label fileDetailStatusLabel;
        private Panel filePreviewPanel;
        private PictureBox filePreviewPictureBox;
        private Label filePreviewMessageLabel;
        private Button fileDetailPlayButton;
        private ToolTip fileDetailPlayToolTip;
        private bool currentPreviewIsVideo;
        private string currentPreviewFileName;
        private Label profileAccountLabel;
        private ConversationListCanvas conversationList;
        private ChatMessageCanvas chatMessages;
        private bool fileActionColumnConfigured;

        private void BuildModernLayout()
        {
            SuspendLayout();
            Text = "云聊空间";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Normal;
            MaximizeBox = true;
            BackColor = Color.FromArgb(241, 247, 252);
            Font = UiTheme.FontBody;
            // 在字体缩放完成后再设定尺寸，避免高 DPI 下被二次放大。
            MinimumSize = new Size(1120, 650);
            ClientSize = new Size(1366, 768);

            Controls.Clear();
            modernNavPanel = BuildNavigation();

            main_tabControl.Dock = DockStyle.Fill;
            main_tabControl.Appearance = TabAppearance.FlatButtons;
            main_tabControl.ItemSize = new Size(0, 1);
            main_tabControl.SizeMode = TabSizeMode.Fixed;
            main_tabControl.Multiline = true;
            main_tabControl.Padding = new Point(0, 0);
            main_tabControl.Margin = Padding.Empty;

            // 固定功能栏和内容区边界，避免 Fill 停靠覆盖导航栏。
            TableLayoutPanel appShell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = BackColor, Margin = Padding.Empty, Padding = Padding.Empty };
            appShell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ModernNavigationWidth));
            appShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            appShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            modernNavPanel.Dock = DockStyle.Fill;
            appShell.Controls.Add(modernNavPanel, 0, 0);
            appShell.Controls.Add(main_tabControl, 1, 0);
            Controls.Add(appShell);

            BuildChatPageLayout();
            BuildDrivePageLayout();
            BuildActivityPageLayout();
            BuildSettingsPageLayout();
            SelectMainSection(0, 0);
            ResumeLayout(true);
        }

        private Panel BuildTopBar()
        {
            Panel bar = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White, Padding = new Padding(20, 10, 20, 10) };
            Label logo = new Label { Text = "C", Dock = DockStyle.Left, Width = 38, TextAlign = ContentAlignment.MiddleCenter, BackColor = UiTheme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 14F, FontStyle.Bold), Margin = new Padding(0, 4, 12, 4) };
            Panel titleBox = new Panel { Dock = DockStyle.Left, Width = 225 };
            Label title = new Label { Text = "云聊空间", Location = new Point(12, 2), Size = new Size(180, 24), Font = new Font("微软雅黑", 12F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            remote_address_label.Text = "服务器";
            remote_address_label.Font = new Font("微软雅黑", 8.5F);
            remote_address_label.ForeColor = UiTheme.TextSecondary;
            remote_address_label.Location = new Point(12, 28);
            remote_address_label.AutoSize = true;
            remote_address_textBox.BorderStyle = BorderStyle.None;
            remote_address_textBox.BackColor = Color.White;
            remote_address_textBox.ForeColor = UiTheme.TextSecondary;
            remote_address_textBox.Font = new Font("Segoe UI", 8.5F);
            remote_address_textBox.Location = new Point(60, 29);
            remote_address_textBox.Size = new Size(150, 18);
            titleBox.Controls.Add(title);
            titleBox.Controls.Add(remote_address_label);
            titleBox.Controls.Add(remote_address_textBox);

            Panel exitHost = new Panel { Dock = DockStyle.Right, Width = 62, Padding = new Padding(3, 7, 3, 7) };
            exist_button.Dock = DockStyle.Fill;
            exist_button.Text = "退出";
            exist_button.Font = new Font("微软雅黑", 9F);
            user_label.Dock = DockStyle.Right;
            user_label.Width = 330;
            user_label.TextAlign = ContentAlignment.MiddleRight;
            user_label.ForeColor = UiTheme.TextSecondary;
            user_label.Font = new Font("微软雅黑", 9F);

            bar.Controls.Add(user_label);
            exitHost.Controls.Add(exist_button);
            bar.Controls.Add(exitHost);
            bar.Controls.Add(titleBox);
            bar.Controls.Add(logo);
            return bar;
        }

        private Panel BuildNavigation()
        {
            Panel nav = new Panel { Dock = DockStyle.Left, Width = ModernNavigationWidth, BackColor = Color.FromArgb(19, 42, 70), Padding = new Padding(8, 20, 8, 14) };
            navChatButton = CreateNavButton("●\r\n聊天");
            navDriveButton = CreateNavButton("☁\r\n网盘");
            navUploadButton = CreateNavButton("◈\r\n动态");
            navDownloadButton = CreateNavButton("⚙\r\n设置");
            navChatButton.Click += delegate { SelectMainSection(0, 0); };
            navDriveButton.Click += delegate { SelectMainSection(1, 0); };
            navUploadButton.Click += delegate { SelectMainSection(2, 0); };
            navDownloadButton.Click += delegate { SelectMainSection(3, 0); };
            nav.Controls.Add(navDownloadButton);
            nav.Controls.Add(navUploadButton);
            nav.Controls.Add(navDriveButton);
            nav.Controls.Add(navChatButton);
            return nav;
        }

        private Button CreateNavButton(string text)
        {
            Button button = new Button { Text = text, Dock = DockStyle.Top, Height = 100, FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.FromArgb(208, 222, 239), Font = new Font("微软雅黑", 10.5F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0, 0, 0, 10), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 86, 138);
            return button;
        }

        private Panel BuildStatusBar()
        {
            Panel bar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White, Padding = new Padding(14, 0, 14, 0) };
            result_label.Dock = DockStyle.Left;
            result_label.Width = 360;
            result_label.TextAlign = ContentAlignment.MiddleLeft;
            result_label.Font = new Font("微软雅黑", 8.5F);
            net_rate_label.Dock = DockStyle.Left;
            net_rate_label.Width = 210;
            net_rate_label.TextAlign = ContentAlignment.MiddleLeft;
            net_rate_label.Font = new Font("微软雅黑", 8.5F);
            date_label.Dock = DockStyle.Right;
            date_label.Width = 320;
            date_label.TextAlign = ContentAlignment.MiddleRight;
            date_label.Font = new Font("微软雅黑", 8.5F);
            date_label.ForeColor = UiTheme.TextSecondary;
            bar.Controls.Add(date_label);
            bar.Controls.Add(net_rate_label);
            bar.Controls.Add(result_label);
            return bar;
        }

        private void BuildChatPageLayout()
        {
            chat_tabPage.SuspendLayout();
            chat_tabPage.Controls.Clear();
            chat_tabPage.Padding = new Padding(18);
            chat_tabPage.BackColor = Color.FromArgb(241, 247, 252);

            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = chat_tabPage.BackColor };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ChatContactListWidth));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            RoundedPanel contactsCard = new RoundedPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 252, 255), Padding = new Padding(16), Margin = new Padding(0, 0, 16, 0) };
            Panel profile = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = contactsCard.BackColor };
            Label avatar = new Label { Text = "我", Location = new Point(2, 12), Size = new Size(58, 58), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(41, 129, 244), ForeColor = Color.White, Font = new Font("微软雅黑", 18F, FontStyle.Bold) };
            Label account = new Label { Text = "当前账号", Location = new Point(76, 13), Size = new Size(200, 28), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            profileAccountLabel = account;
            Label online = new Label { Text = "● 在线", Location = new Point(77, 43), Size = new Size(120, 24), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 9F), ForeColor = Color.FromArgb(33, 186, 121) };
            Label divider = new Label { BorderStyle = BorderStyle.Fixed3D, Location = new Point(0, 84), Size = new Size(232, 1) };
            profile.Controls.Add(divider);
            profile.Controls.Add(online);
            profile.Controls.Add(account);
            profile.Controls.Add(avatar);

            Panel recentHeader = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = contactsCard.BackColor };
            Label recentTitle = new Label { Text = "最近会话", Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 14F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            query_user_textBox.Location = new Point(0, 44);
            query_user_textBox.Size = new Size(128, 34);
            query_user_textBox.Multiline = true;
            query_user_textBox.Font = new Font("微软雅黑", 9.5F);
            query_user_textBox.BorderStyle = BorderStyle.FixedSingle;
            query_user_button.Location = new Point(134, 44);
            query_user_button.Size = new Size(48, 34);
            query_user_button.Text = "搜索";
            query_user_button.Font = new Font("微软雅黑", 8.5F);
            refresh_button.Location = new Point(188, 44);
            refresh_button.Size = new Size(36, 34);
            refresh_button.Text = "↻";
            refresh_button.Font = new Font("Segoe UI", 13F);
            RoundedPanel addFriendIcon = new RoundedPanel { Location = new Point(200, 3), Size = new Size(32, 32), CornerRadius = 16, BackColor = Color.FromArgb(229, 242, 255), Cursor = Cursors.Hand };
            Label addFriendIconText = new Label { Text = "＋", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UiTheme.Primary, Cursor = Cursors.Hand };
            addFriendIcon.Controls.Add(addFriendIconText);
            recentHeader.Controls.Add(refresh_button);
            recentHeader.Controls.Add(query_user_button);
            recentHeader.Controls.Add(query_user_textBox);
            recentHeader.Controls.Add(addFriendIcon);
            recentHeader.Controls.Add(recentTitle);

            Panel addFriendRow = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.FromArgb(248, 252, 255), Cursor = Cursors.Hand };
            Label friendAvatar = new Label { Text = "♙", Location = new Point(10, 12), Size = new Size(44, 44), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Symbol", 18F), BackColor = Color.FromArgb(224, 151, 22), ForeColor = Color.White };
            Label friendTitle = new Label { Text = "新的好友", Location = new Point(68, 12), Size = new Size(130, 22), Font = new Font("微软雅黑", 10F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            Label friendHint = new Label { Text = "添加好友与处理申请", Location = new Point(68, 35), Size = new Size(135, 20), Font = new Font("微软雅黑", 8.5F), ForeColor = UiTheme.TextSecondary };
            Label friendArrow = new Label { Text = "›", Location = new Point(204, 21), Size = new Size(24, 30), Font = new Font("Segoe UI", 20F), ForeColor = Color.FromArgb(139, 158, 180), TextAlign = ContentAlignment.MiddleCenter };
            add_user_button.Visible = false;
            addFriendRow.Controls.Add(friendArrow);
            addFriendRow.Controls.Add(friendHint);
            addFriendRow.Controls.Add(friendTitle);
            addFriendRow.Controls.Add(friendAvatar);
            EventHandler openAddFriend = delegate { add_user_button_Click(add_user_button, EventArgs.Empty); };
            addFriendRow.Click += openAddFriend;
            friendAvatar.Click += openAddFriend;
            friendTitle.Click += openAddFriend;
            friendHint.Click += openAddFriend;
            friendArrow.Click += openAddFriend;

            Panel cloudTip = new Panel { Dock = DockStyle.Bottom, Height = 72, BackColor = Color.FromArgb(231, 241, 252), Padding = new Padding(14, 7, 14, 7) };
            Label cloudTitle = new Label { Text = "☁  云盘协作", Dock = DockStyle.Top, Height = 23, Font = new Font("微软雅黑", 9.5F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            Label cloudHint = new Label { Text = "聊天图片与文件可直接保存到个人云盘。", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 8.5F), ForeColor = UiTheme.TextSecondary };
            cloudTip.Controls.Add(cloudHint);
            cloudTip.Controls.Add(cloudTitle);

            addFriendIcon.Click += openAddFriend;
            addFriendIconText.Click += openAddFriend;

            // 在线用户表只保留为数据源，界面由会话卡片列表呈现。
            user_list_dataGridView.Visible = false;
            conversationList = new ConversationListCanvas { Dock = DockStyle.Fill, Margin = Padding.Empty };
            conversationList.ContactSelected += delegate(object sender, string userName) { SelectChatContact(userName); };
            RebuildConversationList();
            contactsCard.Controls.Add(conversationList);
            contactsCard.Controls.Add(cloudTip);
            contactsCard.Controls.Add(addFriendRow);
            contactsCard.Controls.Add(recentHeader);
            contactsCard.Controls.Add(profile);

            RoundedPanel chatCard = new RoundedPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = Padding.Empty, Margin = Padding.Empty };
            Panel chatHeader = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.White, Padding = new Padding(24, 0, 24, 0) };
            receive_form_label.Dock = DockStyle.Left;
            receive_form_label.Width = 330;
            receive_form_label.Text = "";
            receive_form_label.TextAlign = ContentAlignment.MiddleLeft;
            receive_form_label.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            receive_form_label.ForeColor = UiTheme.TextMain;
            chat_with_user_label.Dock = DockStyle.Left;
            chat_with_user_label.Width = 120;
            chat_with_user_label.Text = "";
            chat_with_user_label.TextAlign = ContentAlignment.MiddleLeft;
            chat_with_user_label.Font = new Font("微软雅黑", 9.5F);
            chat_with_user_label.ForeColor = Color.FromArgb(33, 186, 121);
            Label menu = new Label { Text = "•••", Dock = DockStyle.Right, Width = 42, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UiTheme.Primary };
            chatHeader.Controls.Add(menu);
            chatHeader.Controls.Add(chat_with_user_label);
            chatHeader.Controls.Add(receive_form_label);

            Panel messageSurface = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 249, 254), Padding = Padding.Empty };
            chatMessages = new ChatMessageCanvas { Dock = DockStyle.Fill };
            message_richTextBox.Visible = false;
            message_richTextBox.TextChanged += delegate { if (chatMessages != null) chatMessages.SetTranscript(message_richTextBox.Text); };
            chatMessages.SetTranscript(message_richTextBox.Text);
            messageSurface.Controls.Add(chatMessages);

            Panel composer = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = Color.White, Padding = new Padding(24, 12, 24, 18) };
            Label composerLine = new Label { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(227, 236, 245) };
            Panel editorRow = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0), BackColor = Color.White };
            Label tools = new Label { Text = "☺    ♧", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI Symbol", 15F), ForeColor = Color.FromArgb(122, 138, 157) };
            Panel sendHost = new Panel { Dock = DockStyle.Right, Width = 86, Padding = new Padding(10, 44, 0, 12), BackColor = Color.White };
            send_button.Dock = DockStyle.Fill;
            send_button.MinimumSize = new Size(0, 34);
            send_button.Text = "发送";
            send_button.Font = new Font("微软雅黑", 9.5F, FontStyle.Bold);
            UiTheme.StyleButton(send_button, UiTheme.Kind.Primary);
            sendHost.Controls.Add(send_button);
            send_message_richTextBox.Dock = DockStyle.Fill;
            send_message_richTextBox.BorderStyle = BorderStyle.None;
            send_message_richTextBox.BackColor = Color.White;
            send_message_richTextBox.Font = new Font("微软雅黑", 10.5F);
            send_message_richTextBox.ForeColor = UiTheme.TextMain;
            send_message_richTextBox.WordWrap = true;
            editorRow.Controls.Add(send_message_richTextBox);
            editorRow.Controls.Add(sendHost);
            composer.Controls.Add(editorRow);
            composer.Controls.Add(tools);
            composer.Controls.Add(composerLine);

            chatCard.Controls.Add(messageSurface);
            chatCard.Controls.Add(composer);
            chatCard.Controls.Add(chatHeader);
            shell.Controls.Add(contactsCard, 0, 0);
            shell.Controls.Add(chatCard, 1, 0);
            chat_tabPage.Controls.Add(shell);
            chat_tabPage.ResumeLayout(true);
        }

        private void DrawChatBackdrop(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(224, 238, 250), 2F))
            {
                int width = e.ClipRectangle.Width;
                int height = e.ClipRectangle.Height;
                e.Graphics.DrawArc(pen, -width / 4, height / 4, width + 180, height, 195, 135);
                e.Graphics.DrawArc(pen, width / 5, -height / 3, width, height + 180, 28, 120);
            }
        }

        private void RebuildConversationList()
        {
            if (conversationList == null) return;
            List<string> users = new List<string>();
            foreach (DataGridViewRow row in user_list_dataGridView.Rows)
            {
                if (row.IsNewRow || row.Cells.Count < 2 || row.Cells[1].Value == null) continue;
                users.Add(Convert.ToString(row.Cells[1].Value));
            }
            conversationList.SetContacts(users, currentSelectUser);
        }

        private void SelectChatContact(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return;
            currentSelectUser = userName;
            receive_form_label.Text = userName;
            chat_with_user_label.Text = "● 在线";
            RebuildConversationList();
        }

        private void UpdateChatProfile(string userName)
        {
            if (profileAccountLabel != null && !string.IsNullOrWhiteSpace(userName))
                profileAccountLabel.Text = userName;
        }

        private void BuildDrivePageLayout()
        {
            file_tabPage.SuspendLayout();
            file_tabPage.Controls.Clear();
            file_tabPage.Padding = Padding.Empty;
            file_tabPage.BackColor = UiTheme.Panel;
            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1024, 600), FixedPanel = FixedPanel.None, SplitterWidth = 1, BackColor = UiTheme.Border };
            split.Panel1MinSize = 200;
            split.Panel2MinSize = 560;
            SetInitialSplitterDistance(split, DriveDirectoryTreeWidth);
            split.Panel1.BackColor = Color.White;
            split.Panel2.BackColor = UiTheme.Panel;

            Panel driveHeader = new Panel { Dock = DockStyle.Top, Height = 138, BackColor = Color.White, Padding = new Padding(20, 18, 20, 12) };
            Label driveTitle = new Label { Text = "我的空间", Dock = DockStyle.Top, Height = 30, Font = new Font("微软雅黑", 14F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            Label driveHint = new Label { Text = "集中管理你的文件与传输任务", Dock = DockStyle.Top, Height = 25, Font = new Font("微软雅黑", 9F), ForeColor = UiTheme.TextSecondary };
            Panel storageCard = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(241, 246, 255), Padding = new Padding(12, 8, 10, 8) };
            Label storageTitle = new Label { Text = "☁  个人网盘", Dock = DockStyle.Top, Height = 21, Font = new Font("微软雅黑", 9.5F, FontStyle.Bold), ForeColor = UiTheme.Primary };
            Label storageHint = new Label { Text = "文件与文件夹实时同步", Dock = DockStyle.Fill, Font = new Font("微软雅黑", 8.5F), ForeColor = UiTheme.TextSecondary };
            file_refresh_button.Dock = DockStyle.Right;
            file_refresh_button.Size = new Size(34, 34);
            file_refresh_button.Margin = Padding.Empty;
            file_refresh_button.Text = "↻";
            file_refresh_button.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            file_refresh_button.AccessibleName = "同步目录";
            file_refresh_button.TabStop = true;
            storageCard.Controls.Add(storageHint);
            storageCard.Controls.Add(storageTitle);
            storageCard.Controls.Add(file_refresh_button);
            driveHeader.Controls.Add(storageCard);
            driveHeader.Controls.Add(driveHint);
            driveHeader.Controls.Add(driveTitle);
            personal_file_treeView.Dock = DockStyle.Fill;
            personal_file_treeView.BorderStyle = BorderStyle.None;
            personal_file_treeView.ItemHeight = 34;
            personal_file_treeView.Indent = 20;
            personal_file_treeView.BackColor = Color.White;
            personal_file_treeView.ForeColor = UiTheme.TextMain;
            personal_file_treeView.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            personal_file_treeView.ShowLines = false;
            personal_file_treeView.ShowPlusMinus = false;
            personal_file_treeView.ShowRootLines = false;
            personal_file_treeView.HideSelection = false;
            personal_file_treeView.FullRowSelect = true;
            personal_file_treeView.DrawNode -= DrawDirectoryNode;
            personal_file_treeView.DrawNode += DrawDirectoryNode;
            personal_file_treeView.NodeMouseClick -= DirectoryTreeNodeMouseClick;
            personal_file_treeView.NodeMouseClick += DirectoryTreeNodeMouseClick;
            split.Panel1.Controls.Add(personal_file_treeView);
            split.Panel1.Controls.Add(driveHeader);
            Panel driveContent = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Panel };
            driveContentPanel = driveContent;
            Panel fileContent = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Panel };
            BuildFileListTab(fileContent);
            // 先放置填充内容，再添加底部停靠控件，避免分页与传输中心互相覆盖。
            driveContent.Controls.Add(fileContent);
            BuildTransferCenter(driveContent);
            split.Panel2.Controls.Add(driveContent);
            file_tabPage.Controls.Add(split);
            file_tabPage.ResumeLayout(true);
        }

        private void BuildActivityPageLayout()
        {
            activityTabPage = new TabPage("动态") { BackColor = UiTheme.Panel, Padding = new Padding(34, 30, 34, 30) };
            main_tabControl.TabPages.Add(activityTabPage);

            Panel card = new Panel { Dock = DockStyle.Top, Height = 235, BackColor = Color.White, Padding = new Padding(28, 26, 28, 26) };
            Label title = new Label { Text = "动态", Dock = DockStyle.Top, Height = 36, Font = new Font("微软雅黑", 16F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            Label subtitle = new Label { Text = "查看文件传输和系统提醒", Dock = DockStyle.Top, Height = 26, Font = new Font("微软雅黑", 9.5F), ForeColor = UiTheme.TextSecondary };
            Label emptyIcon = new Label { Text = "◌", Dock = DockStyle.Top, Height = 78, TextAlign = ContentAlignment.BottomCenter, Font = new Font("Segoe UI", 32F), ForeColor = Color.FromArgb(130, 177, 248) };
            Label empty = new Label { Text = "暂时没有新的动态", Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.TopCenter, Font = UiTheme.FontTitle, ForeColor = UiTheme.TextMain };
            card.Controls.Add(empty);
            card.Controls.Add(emptyIcon);
            card.Controls.Add(subtitle);
            card.Controls.Add(title);
            activityTabPage.Controls.Add(card);
        }

        private void BuildSettingsPageLayout()
        {
            settingsTabPage = new TabPage("设置") { BackColor = UiTheme.Panel, Padding = new Padding(34, 30, 34, 30) };
            main_tabControl.TabPages.Add(settingsTabPage);

            Panel card = new Panel { Dock = DockStyle.Top, Height = 210, BackColor = Color.White, Padding = new Padding(28, 26, 28, 26) };
            Label title = new Label { Text = "设置", Dock = DockStyle.Top, Height = 36, Font = new Font("微软雅黑", 16F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            Label subtitle = new Label { Text = "管理服务连接与应用偏好", Dock = DockStyle.Top, Height = 28, Font = new Font("微软雅黑", 9.5F), ForeColor = UiTheme.TextSecondary };
            Panel actionRow = new Panel { Dock = DockStyle.Top, Height = 46 };
            Button openSettings = new Button { Text = "打开连接设置", Location = new Point(0, 4), Size = new Size(118, 36), TextAlign = ContentAlignment.MiddleCenter };
            UiTheme.StyleButton(openSettings, UiTheme.Kind.Primary);
            openSettings.Click += delegate
            {
                using (Setting_Form settings = new Setting_Form())
                    settings.ShowDialog(this);
            };
            actionRow.Controls.Add(openSettings);
            card.Controls.Add(actionRow);
            card.Controls.Add(subtitle);
            card.Controls.Add(title);
            settingsTabPage.Controls.Add(card);
        }

        private void BuildTransferCenter(Panel driveContent)
        {
            transferCenterPanel = new Panel { Dock = DockStyle.Bottom, Height = 260, BackColor = Color.White, Padding = new Padding(20, 0, 20, 16) };
            Panel header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };
            Label title = new Label { Text = "⇅  传输中心", Dock = DockStyle.Left, Width = 126, TextAlign = ContentAlignment.MiddleLeft, Font = UiTheme.FontTitle, ForeColor = UiTheme.TextMain };
            transferSummaryLabel = new Label { Dock = DockStyle.Left, Width = 210, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 9F), ForeColor = UiTheme.TextSecondary };
            Panel actions = new Panel { Dock = DockStyle.Right, Width = 164, Padding = new Padding(0, 13, 0, 13) };
            Button collapse = new Button { Text = "收起  ˅", Dock = DockStyle.Right, Width = 68, Font = new Font("微软雅黑", 9F) };
            transferClearButton = new Button { Text = "清空已完成", Dock = DockStyle.Right, Width = 92, Font = new Font("微软雅黑", 9F) };
            UiTheme.StyleButton(collapse, UiTheme.Kind.Default);
            UiTheme.StyleButton(transferClearButton, UiTheme.Kind.Default);
            collapse.Click += delegate { CollapseTransferCenter(); };
            transferClearButton.Click += delegate
            {
                fileUploadClear();
                fileDownloadClear();
                RefreshUnifiedTransferList();
            };
            actions.Controls.Add(collapse);
            actions.Controls.Add(transferClearButton);
            header.Controls.Add(actions);
            header.Controls.Add(transferSummaryLabel);
            header.Controls.Add(title);

            transferListHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            unifiedTransferGrid = CreateUnifiedTransferGrid();
            transferListHost.Controls.Add(unifiedTransferGrid);
            transferStatusFilters = BuildTransferStatusFilters();
            transferCenterPanel.Controls.Add(transferListHost);
            transferCenterPanel.Controls.Add(transferStatusFilters);
            transferCenterPanel.Controls.Add(header);

            transferLauncherHost = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = UiTheme.Panel, Padding = new Padding(0, 8, 20, 8) };
            Panel launcherSlot = new Panel { Dock = DockStyle.Right, Width = 132 };
            Button launcher = new Button { Text = "⇅  传输中心", Dock = DockStyle.Fill };
            UiTheme.StyleButton(launcher, UiTheme.Kind.Default);
            launcher.Click += delegate { ShowTransferCenter(false); };
            launcherSlot.Controls.Add(launcher);
            transferLauncherHost.Controls.Add(launcherSlot);

            driveContent.Controls.Add(transferCenterPanel);
            driveContent.Controls.Add(transferLauncherHost);
            file_upload_list_dataGridView.RowsAdded += delegate { RefreshUnifiedTransferList(); };
            file_upload_list_dataGridView.RowsRemoved += delegate { RefreshUnifiedTransferList(); };
            file_download_list_dataGridView.RowsAdded += delegate { RefreshUnifiedTransferList(); };
            file_download_list_dataGridView.RowsRemoved += delegate { RefreshUnifiedTransferList(); };
            // 实时同步进度/状态到统一传输网格，让进度条动态变化而不依赖整表重建。
            file_upload_list_dataGridView.CellValueChanged += TransferCellValueChanged;
            file_download_list_dataGridView.CellValueChanged += TransferCellValueChanged;
            RefreshUnifiedTransferList();
            CollapseTransferCenter();
        }

        private void ShowTransferCenter(bool upload)
        {
            if (transferCenterPanel == null || transferLauncherHost == null) return;
            RefreshUnifiedTransferList();
            transferCenterPanel.Visible = true;
            transferLauncherHost.Visible = false;
            driveContentPanel.PerformLayout();
        }

        private void CollapseTransferCenter()
        {
            if (transferCenterPanel == null || transferLauncherHost == null) return;
            transferCenterPanel.Visible = false;
            transferLauncherHost.Visible = true;
            driveContentPanel.PerformLayout();
        }

        private void UpdateTransferSummary()
        {
            if (transferSummaryLabel == null) return;
            transferSummaryLabel.Text = "共 " + (file_upload_list_dataGridView.Rows.Count + file_download_list_dataGridView.Rows.Count) + " 项任务";
        }

        private FlowLayoutPanel BuildTransferStatusFilters()
        {
            FlowLayoutPanel filters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.White,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 6)
            };
            filters.Controls.Add(CreateTransferFilterButton("全部", 68));
            filters.Controls.Add(CreateTransferFilterButton("待处理", 78));
            filters.Controls.Add(CreateTransferFilterButton("进行中", 78));
            filters.Controls.Add(CreateTransferFilterButton("已完成", 78));
            filters.Controls.Add(CreateTransferFilterButton("失败", 68));
            return filters;
        }

        private Button CreateTransferFilterButton(string filter, int width)
        {
            Button button = new Button
            {
                Tag = filter,
                Width = width,
                Height = 28,
                Margin = new Padding(0, 0, 7, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            button.Click += delegate
            {
                activeTransferStatusFilter = Convert.ToString(button.Tag);
                RefreshUnifiedTransferList();
            };
            return button;
        }

        private void UpdateTransferStatusFilters()
        {
            if (transferStatusFilters == null) return;
            foreach (Control control in transferStatusFilters.Controls)
            {
                Button button = control as Button;
                if (button == null) continue;
                string filter = Convert.ToString(button.Tag);
                bool selected = string.Equals(filter, activeTransferStatusFilter, StringComparison.Ordinal);
                button.Text = filter + " " + CountTransferTasks(filter);
                button.BackColor = selected ? Color.FromArgb(232, 248, 239) : Color.White;
                button.ForeColor = selected ? UiTheme.Success : UiTheme.TextSecondary;
                button.FlatAppearance.BorderColor = selected ? Color.FromArgb(159, 223, 187) : UiTheme.Border;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(242, 250, 246);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(224, 245, 234);
            }
        }

        private int CountTransferTasks(string filter)
        {
            return CountMatchingTransferTasks(file_upload_list_dataGridView, 4, filter)
                 + CountMatchingTransferTasks(file_download_list_dataGridView, 3, filter);
        }

        private int CountMatchingTransferTasks(DataGridView source, int statusIndex, string filter)
        {
            int count = 0;
            foreach (DataGridViewRow row in source.Rows)
            {
                if (!row.IsNewRow && MatchesTransferStatus(GetTaskCellText(row, statusIndex), filter)) count++;
            }
            return count;
        }

        private bool MatchesTransferStatus(string status, string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "全部") return true;
            status = status ?? string.Empty;
            if (filter == "待处理") return status.Contains("待") || status.Contains("排队");
            if (filter == "进行中") return status.Contains("进行") || status.Contains("上传中") || status.Contains("下载中");
            if (filter == "已完成") return status.Contains("完成") || status.Contains("成功") || status.Contains("已上传") || status.Contains("已下载");
            if (filter == "失败") return status.Contains("失败") || status.Contains("错误");
            return true;
        }

        private DataGridView CreateUnifiedTransferGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiTheme.StyleGrid(grid);
            grid.RowTemplate.Height = 38;
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "文件名称", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 138 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "文件大小", Width = 82, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            grid.Columns.Add(new StatusIconColumn { HeaderText = "状态", Width = 96, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UiTheme.FontBody } });
            grid.Columns.Add(new ModernProgressBarColumn { HeaderText = "进度", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 112, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            grid.Columns.Add(new DataGridViewButtonColumn { HeaderText = "操作", Text = "暂停/继续", UseColumnTextForButtonValue = true, Width = 86, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat });
            grid.Columns.Add(new DataGridViewButtonColumn { HeaderText = "操作", Text = "重传", UseColumnTextForButtonValue = true, Width = 66, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat });
            grid.CellContentClick += UnifiedTransferGrid_CellContentClick;
            return grid;
        }

        private void RefreshUnifiedTransferList()
        {
            if (unifiedTransferGrid == null || unifiedTransferGrid.IsDisposed) return;
            unifiedTransferGrid.Rows.Clear();
            AddUnifiedTransferRows(file_upload_list_dataGridView, 3, 4, 5);
            AddUnifiedTransferRows(file_download_list_dataGridView, 2, 3, 4);
            UpdateTransferSummary();
            UpdateTransferStatusFilters();
        }

        private void AddUnifiedTransferRows(DataGridView source, int sizeIndex, int statusIndex, int progressIndex)
        {
            foreach (DataGridViewRow row in source.Rows)
            {
                if (row.IsNewRow) continue;
                string status = GetTaskCellText(row, statusIndex);
                if (!MatchesTransferStatus(status, activeTransferStatusFilter)) continue;
                unifiedTransferGrid.Rows.Add(
                    GetTaskCellText(row, 1),
                    GetTaskCellText(row, sizeIndex),
                    status,
                    GetTaskCellText(row, progressIndex),
                    null,
                    null);
            }
        }

        private string GetTaskCellText(DataGridViewRow row, int index)
        {
            if (index < 0 || index >= row.Cells.Count || row.Cells[index].Value == null) return "—";
            string text = Convert.ToString(row.Cells[index].FormattedValue);
            return string.IsNullOrWhiteSpace(text) ? "—" : text;
        }

        // 源网格（上传/下载列表）的进度或状态变化时，把最新值同步到统一传输网格对应行。
        private void TransferCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            DataGridView src = sender as DataGridView;
            if (src == null || unifiedTransferGrid == null || unifiedTransferGrid.IsDisposed) return;
            int statusIdx, progressIdx;
            if (src == file_upload_list_dataGridView) { statusIdx = 4; progressIdx = 5; }
            else if (src == file_download_list_dataGridView) { statusIdx = 3; progressIdx = 4; }
            else return;
            if (e.ColumnIndex != statusIdx && e.ColumnIndex != progressIdx) return;
            if (e.RowIndex >= src.RowCount) return;

            string name = GetTaskCellText(src.Rows[e.RowIndex], 1);
            string status = GetTaskCellText(src.Rows[e.RowIndex], statusIdx);
            string progress = GetTaskCellText(src.Rows[e.RowIndex], progressIdx);
            UpdateUnifiedTransferRow(name, status, progress);
        }

        // 到统一传输网格中按文件名找到对应行，仅就地更新状态与进度两列，不整表重建。
        private void UpdateUnifiedTransferRow(string name, string status, string progress)
        {
            if (unifiedTransferGrid == null || unifiedTransferGrid.IsDisposed) return;
            foreach (DataGridViewRow row in unifiedTransferGrid.Rows)
            {
                if (row.IsNewRow) continue;
                if (!string.Equals(GetTaskCellText(row, 0), name, StringComparison.Ordinal)) continue;
                row.Cells[2].Value = status;
                row.Cells[3].Value = progress;
                break;
            }
            unifiedTransferGrid.Invalidate();
        }

        private void BuildFileListTab(Control host)
        {
            tabPage1.Controls.Clear();
            host.Controls.Clear();
            host.BackColor = UiTheme.Panel;
            host.Padding = new Padding(20, 16, 20, 18);
            Panel titleRow = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = UiTheme.Panel };
            currentDirectoryTitleLabel = new Label { Text = "", Dock = DockStyle.Top, Height = 34, Visible = false, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 15F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            Label description = new Label { Text = "从左侧目录查看、下载、重命名或删除文件", Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 9F), ForeColor = UiTheme.TextSecondary };
            file_sum_count_label.Dock = DockStyle.Right;
            file_sum_count_label.Width = 112;
            file_sum_count_label.TextAlign = ContentAlignment.MiddleLeft;
            file_sum_count_label.Font = new Font("微软雅黑", 9F);
            file_sum_count_label.ForeColor = UiTheme.Primary;
            titleRow.Controls.Add(file_sum_count_label);
            titleRow.Controls.Add(description);
            titleRow.Controls.Add(currentDirectoryTitleLabel);
            FlowLayoutPanel tools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = UiTheme.Panel, Padding = new Padding(0, 6, 0, 10) };
            PrepareToolButton(all_select_button, "全选", 64);
            PrepareToolButton(all_cancel_select_button, "取消全选", 84);
            PrepareToolButton(all_select_delete_button, "⌫ 删除所选", 98);
            all_select_download_button.Visible = false;
            all_file_refresh_button.Visible = false;
            tools.Controls.Add(all_select_button);
            tools.Controls.Add(all_cancel_select_button);
            tools.Controls.Add(all_select_delete_button);
            fileDetailToggleButton = new Button { Text = "ⓘ 文件详情" };
            PrepareToolButton(fileDetailToggleButton, "ⓘ 文件详情", 96);
            UiTheme.StyleButton(fileDetailToggleButton, UiTheme.Kind.Default);
            fileDetailToggleButton.Click += delegate
            {
                if (fileDetailPanel != null && fileDetailPanel.Visible) CloseFileDetailPanel();
                else OpenFileDetailPanel();
            };
            tools.Controls.Add(fileDetailToggleButton);
            file_list_dataGridView.Dock = DockStyle.Fill;
            file_list_dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ConfigureFileListColumns();
            file_list_dataGridView.BackgroundColor = Color.White;
            file_list_dataGridView.Paint -= DrawEmptyFileList;
            file_list_dataGridView.Paint += DrawEmptyFileList;
            Panel fileBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel fileListHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            filePaginationPanel = BuildFilePaginationBar();
            fileDetailPanel = BuildFileDetailPanel();
            // 分页栏属于文件列表，固定在表格正下方；传输中心展开时也不会被覆盖。
            fileListHost.Controls.Add(file_list_dataGridView);
            fileListHost.Controls.Add(filePaginationPanel);
            fileBody.Controls.Add(fileListHost);
            fileBody.Controls.Add(fileDetailPanel);
            host.Controls.Add(fileBody);
            host.Controls.Add(tools);
            host.Controls.Add(titleRow);
            CloseFileDetailPanel();
        }

        private Panel BuildFilePaginationBar()
        {
            Panel bar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 49,
                BackColor = Color.White,
                Padding = Padding.Empty
            };
            Panel divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = UiTheme.Border };
            TableLayoutPanel controls = new TableLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 336,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(0, 8, 0, 7)
            };
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            prePage_button.Visible = true;
            prePage_button.Anchor = AnchorStyles.None;
            prePage_button.Dock = DockStyle.Fill;
            prePage_button.Margin = new Padding(0, 0, 8, 0);
            prePage_button.Text = "上一页";
            prePage_button.AccessibleName = "文件列表上一页";
            UiTheme.StyleButton(prePage_button, UiTheme.Kind.Default);

            filePageInfoLabel = new Label
            {
                Text = "第 1 / 1 页 · 共 0 个文件",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 9F),
                ForeColor = UiTheme.TextSecondary,
                AutoEllipsis = true
            };

            nextPage_button.Visible = true;
            nextPage_button.Anchor = AnchorStyles.None;
            nextPage_button.Dock = DockStyle.Fill;
            nextPage_button.Margin = Padding.Empty;
            nextPage_button.Text = "下一页";
            nextPage_button.AccessibleName = "文件列表下一页";
            UiTheme.StyleButton(nextPage_button, UiTheme.Kind.Default);

            controls.Controls.Add(prePage_button, 0, 0);
            controls.Controls.Add(filePageInfoLabel, 1, 0);
            controls.Controls.Add(nextPage_button, 2, 0);
            bar.Resize += delegate
            {
                // 宽屏时保持紧凑右对齐；详情栏打开或窗口收窄时，自适应剩余列表宽度。
                controls.Width = Math.Min(336, Math.Max(0, bar.ClientSize.Width));
            };
            bar.Controls.Add(controls);
            bar.Controls.Add(divider);
            UpdateFilePagination(0, 1, 0);
            return bar;
        }

        private void SetFilePaginationLoading()
        {
            if (filePageInfoLabel == null) return;
            filePageInfoLabel.Text = "正在加载...";
            prePage_button.Enabled = false;
            nextPage_button.Enabled = false;
        }

        private void UpdateFilePagination(long totalCount, int page, int totalPages)
        {
            if (filePageInfoLabel == null) return;
            int displayTotalPages = Math.Max(1, totalPages);
            int displayPage = totalPages <= 0 ? 1 : Math.Max(1, Math.Min(page, totalPages));
            filePageInfoLabel.Text = string.Format(
                "第 {0} / {1} 页 · 共 {2} 个文件", displayPage, displayTotalPages, totalCount);
            prePage_button.Enabled = totalPages > 0 && displayPage > 1;
            nextPage_button.Enabled = totalPages > 0 && displayPage < totalPages;
        }

        private void ShowFilePaginationError()
        {
            if (filePageInfoLabel == null) return;
            filePageInfoLabel.Text = "列表加载失败";
            prePage_button.Enabled = false;
            nextPage_button.Enabled = false;
        }

        private Panel BuildFileDetailPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 276,
                BackColor = Color.White,
                Padding = new Padding(17, 0, 0, 0)
            };
            Panel divider = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = UiTheme.Border };
            Panel inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 0, 8, 8) };
            Panel header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White };
            Label title = new Label
            {
                Text = "文件详情",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = UiTheme.TextMain
            };
            Button closeButton = new Button
            {
                Text = "×",
                Dock = DockStyle.Right,
                Width = 34,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 13F),
                ForeColor = UiTheme.TextSecondary,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(242, 245, 249);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 237, 244);
            closeButton.Click += delegate { CloseFileDetailPanel(); };
            header.Controls.Add(title);
            header.Controls.Add(closeButton);

            fileDetailPlaceholderLabel = new Label
            {
                Text = "▤\r\n\r\n选择文件查看详情",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 10F),
                ForeColor = Color.FromArgb(151, 165, 181)
            };

            fileDetailContentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true, Visible = false };
            filePreviewPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 156,
                BackColor = Color.FromArgb(245, 248, 252),
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 8)
            };
            filePreviewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(238, 243, 249),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Visible = false
            };
            filePreviewMessageLabel = new Label
            {
                Text = "选择图片或视频查看预览",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 9F),
                ForeColor = UiTheme.TextSecondary
            };
            fileDetailPlayButton = new Button
            {
                Text = "▶",
                Size = new Size(54, 54),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 48, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Symbol", 17F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Visible = false,
                TabStop = true,
                AccessibleName = "在线播放视频"
            };
            fileDetailPlayButton.FlatAppearance.BorderSize = 0;
            fileDetailPlayButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 84);
            fileDetailPlayButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 82, 104);
            fileDetailPlayToolTip = new ToolTip(components);
            fileDetailPlayToolTip.SetToolTip(fileDetailPlayButton, "在线播放");
            fileDetailPlayButton.Click += delegate { OpenSelectedVideoPlayer(); };
            filePreviewPanel.Resize += delegate { PositionFileDetailPlayButton(); };
            filePreviewPictureBox.Click += delegate
            {
                if (currentPreviewIsVideo) OpenSelectedVideoPlayer();
                else if (filePreviewPictureBox.Image != null) OpenImagePreview();
            };
            filePreviewPanel.Controls.Add(filePreviewPictureBox);
            filePreviewPanel.Controls.Add(filePreviewMessageLabel);
            filePreviewPanel.Controls.Add(fileDetailPlayButton);
            PositionFileDetailPlayButton();
            fileDetailNameLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 58,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                ForeColor = UiTheme.TextMain,
                Padding = new Padding(0, 4, 0, 4)
            };
            TableLayoutPanel fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                ColumnCount = 2,
                RowCount = 7,
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            fileDetailTypeLabel = AddFileDetailField(fields, 0, "类型");
            fileDetailSizeLabel = AddFileDetailField(fields, 1, "大小");
            fileDetailDirectoryLabel = AddFileDetailField(fields, 2, "所属目录");
            fileDetailPathLabel = AddFileDetailField(fields, 3, "完整路径");
            fileDetailCreatedLabel = AddFileDetailField(fields, 4, "创建时间");
            fileDetailModifiedLabel = AddFileDetailField(fields, 5, "更新时间");
            fileDetailStatusLabel = AddFileDetailField(fields, 6, "状态");
            fileDetailContentPanel.Controls.Add(fields);
            fileDetailContentPanel.Controls.Add(fileDetailNameLabel);
            fileDetailContentPanel.Controls.Add(filePreviewPanel);

            inner.Controls.Add(fileDetailPlaceholderLabel);
            inner.Controls.Add(fileDetailContentPanel);
            inner.Controls.Add(header);
            panel.Controls.Add(inner);
            panel.Controls.Add(divider);
            return panel;
        }

        private void OpenFileDetailPanel()
        {
            if (fileDetailPanel == null) return;
            fileDetailPanel.Visible = true;
            if (fileDetailToggleButton != null) fileDetailToggleButton.Text = "收起详情";
            if (fileDetailPanel.Parent != null) fileDetailPanel.Parent.PerformLayout();
        }

        private void CloseFileDetailPanel()
        {
            if (fileDetailPanel == null) return;
            fileDetailPanel.Visible = false;
            if (fileDetailToggleButton != null) fileDetailToggleButton.Text = "ⓘ 文件详情";
            if (fileDetailPanel.Parent != null) fileDetailPanel.Parent.PerformLayout();
        }

        private Label AddFileDetailField(TableLayoutPanel fields, int row, string caption)
        {
            Label captionLabel = new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("微软雅黑", 9F),
                ForeColor = UiTheme.TextSecondary,
                Padding = new Padding(0, 7, 0, 0)
            };
            Label valueLabel = new Label
            {
                Text = "—",
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("微软雅黑", 9F),
                ForeColor = UiTheme.TextMain,
                Padding = new Padding(0, 7, 0, 0)
            };
            fields.Controls.Add(captionLabel, 0, row);
            fields.Controls.Add(valueLabel, 1, row);
            return valueLabel;
        }

        private void ShowFileDetailLoading(string fileName)
        {
            if (fileDetailPlaceholderLabel == null || fileDetailContentPanel == null) return;
            SetFileDetailPlayButtonVisible(false);
            OpenFileDetailPanel();
            fileDetailContentPanel.Visible = false;
            fileDetailPlaceholderLabel.Text = "正在加载\r\n" + (fileName ?? "文件详情");
            fileDetailPlaceholderLabel.Visible = true;
        }

        private void ShowFilePreviewLoading(bool video)
        {
            ClearFilePreviewImage();
            currentPreviewIsVideo = video;
            filePreviewMessageLabel.Text = video ? "正在生成视频缩略图…" : "正在加载图片预览…";
            filePreviewMessageLabel.Visible = true;
            filePreviewPictureBox.Visible = false;
        }

        private void ShowFilePreview(Image preview, bool video, string fileName)
        {
            ClearFilePreviewImage();
            currentPreviewIsVideo = video;
            currentPreviewFileName = fileName ?? "文件预览";
            filePreviewPictureBox.Image = preview;
            filePreviewPictureBox.Cursor = Cursors.Hand;
            filePreviewMessageLabel.Visible = false;
            filePreviewPictureBox.Visible = true;
            filePreviewPictureBox.Invalidate();
            SetFileDetailPlayButtonVisible(video && CanPlaySelectedVideo());
        }

        private void ShowFilePreviewUnavailable(string message)
        {
            ClearFilePreviewImage();
            currentPreviewIsVideo = CanPlaySelectedVideo();
            filePreviewMessageLabel.Text = string.IsNullOrWhiteSpace(message) ? "此文件暂不支持预览" : message;
            filePreviewMessageLabel.Visible = true;
            filePreviewPictureBox.Visible = false;
            SetFileDetailPlayButtonVisible(currentPreviewIsVideo);
        }

        private void SetFileDetailPlayButtonVisible(bool visible)
        {
            if (fileDetailPlayButton == null) return;
            fileDetailPlayButton.Visible = visible;
            if (visible)
            {
                PositionFileDetailPlayButton();
                fileDetailPlayButton.BringToFront();
            }
        }

        private bool CanPlaySelectedVideo()
        {
            return selectedFileDetail != null
                && chat_service.protocol.MediaPlaybackService.IsPlayableVideo(selectedFileDetail.FileName)
                && !selectedFileDetail.IsDeleted
                && selectedFileDetail.IsExistBoolean;
        }

        private void PositionFileDetailPlayButton()
        {
            if (filePreviewPanel == null || fileDetailPlayButton == null) return;
            int contentWidth = Math.Max(0, filePreviewPanel.ClientSize.Width
                - filePreviewPanel.Padding.Horizontal);
            int contentHeight = Math.Max(0, filePreviewPanel.ClientSize.Height
                - filePreviewPanel.Padding.Vertical);
            fileDetailPlayButton.Left = filePreviewPanel.Padding.Left
                + Math.Max(0, (contentWidth - fileDetailPlayButton.Width) / 2);
            fileDetailPlayButton.Top = filePreviewPanel.Padding.Top
                + Math.Max(0, (contentHeight - fileDetailPlayButton.Height) / 2);
        }

        private void ClearFilePreviewImage()
        {
            Image previous = filePreviewPictureBox == null ? null : filePreviewPictureBox.Image;
            if (filePreviewPictureBox != null) filePreviewPictureBox.Image = null;
            if (previous != null) previous.Dispose();
        }

        private void OpenImagePreview()
        {
            if (filePreviewPictureBox == null || filePreviewPictureBox.Image == null) return;
            Image previewCopy = new Bitmap(filePreviewPictureBox.Image);
            Form viewer = new Form
            {
                Text = currentPreviewFileName ?? "图片预览",
                StartPosition = FormStartPosition.CenterParent,
                WindowState = FormWindowState.Maximized,
                BackColor = Color.FromArgb(18, 23, 32),
                KeyPreview = true,
                ShowIcon = false
            };
            Panel header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(23, 30, 42) };
            Label title = new Label
            {
                Text = currentPreviewFileName ?? "图片预览",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Font = new Font("微软雅黑", 11F),
                ForeColor = Color.White
            };
            Button close = new Button
            {
                Text = "关闭",
                Dock = DockStyle.Right,
                Width = 76,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(23, 30, 42),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            close.FlatAppearance.BorderSize = 0;
            PictureBox image = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = previewCopy,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(18, 23, 32)
            };
            close.Click += delegate { viewer.Close(); };
            viewer.KeyDown += delegate(object sender, KeyEventArgs args)
            {
                if (args.KeyCode == Keys.Escape) viewer.Close();
            };
            viewer.FormClosed += delegate
            {
                image.Image = null;
                previewCopy.Dispose();
            };
            header.Controls.Add(title);
            header.Controls.Add(close);
            viewer.Controls.Add(image);
            viewer.Controls.Add(header);
            viewer.Show(this);
        }

        private void ResetFileDetail()
        {
            if (fileDetailPlaceholderLabel == null || fileDetailContentPanel == null) return;
            selectedFileDetail = null;
            SetFileDetailPlayButtonVisible(false);
            ClearFilePreviewImage();
            fileDetailContentPanel.Visible = false;
            fileDetailPlaceholderLabel.Text = "▤\r\n\r\n选择文件查看详情";
            fileDetailPlaceholderLabel.Visible = true;
            fileDetailPlaceholderLabel.BringToFront();
            CloseFileDetailPanel();
        }

        private void ShowFileDetail(chat_service.protocol.NetFileDto detail)
        {
            if (detail == null || fileDetailContentPanel == null) return;
            selectedFileDetail = detail;
            SetFileDetailPlayButtonVisible(CanPlaySelectedVideo());
            fileDetailNameLabel.Text = string.IsNullOrWhiteSpace(detail.FileName) ? "未命名文件" : detail.FileName;
            fileDetailTypeLabel.Text = string.IsNullOrWhiteSpace(detail.FileType) ? GetFileTypeText(detail.FileName) : detail.FileType;
            fileDetailSizeLabel.Text = detail.FileSize.HasValue ? getFileSize(detail.FileSize.Value) : "—";
            fileDetailDirectoryLabel.Text = string.IsNullOrWhiteSpace(detail.ParentDirName) ? "—" : detail.ParentDirName;
            fileDetailPathLabel.Text = string.IsNullOrWhiteSpace(detail.FilePath) ? "—" : detail.FilePath;
            fileDetailCreatedLabel.Text = FormatFileTime(detail.GmtCreated);
            fileDetailModifiedLabel.Text = FormatFileTime(detail.GmtModified);
            fileDetailStatusLabel.Text = detail.IsDeleted ? "已删除" : (detail.IsExistBoolean ? "正常" : "文件不存在");
            fileDetailStatusLabel.ForeColor = detail.IsDeleted || !detail.IsExistBoolean ? UiTheme.Danger : UiTheme.Success;
            fileDetailPlaceholderLabel.Visible = false;
            fileDetailContentPanel.Visible = true;
            fileDetailContentPanel.BringToFront();
        }

        private void ShowFileDetailError(string message)
        {
            if (fileDetailPlaceholderLabel == null || fileDetailContentPanel == null) return;
            selectedFileDetail = null;
            SetFileDetailPlayButtonVisible(false);
            fileDetailContentPanel.Visible = false;
            fileDetailPlaceholderLabel.Text = "详情加载失败\r\n" + (string.IsNullOrWhiteSpace(message) ? "请稍后重试" : message);
            fileDetailPlaceholderLabel.Visible = true;
            fileDetailPlaceholderLabel.BringToFront();
        }

        private string GetFileTypeText(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "文件";
            string extension = System.IO.Path.GetExtension(fileName);
            return string.IsNullOrWhiteSpace(extension) ? "文件" : extension.TrimStart('.').ToUpperInvariant() + " 文件";
        }

        private void UpdateDirectoryHeader(string directoryName)
        {
            if (currentDirectoryTitleLabel != null)
            {
                currentDirectoryTitleLabel.Text = directoryName ?? "";
                currentDirectoryTitleLabel.Visible = !string.IsNullOrWhiteSpace(directoryName);
            }
        }

        private void ConfigureFileListColumns()
        {
            DataGridView grid = file_list_dataGridView;
            if (grid.Columns.Count < 9) return;

            const string actionColumnName = "ModernFileActionColumn";
            if (!grid.Columns.Contains(actionColumnName))
            {
                DataGridViewComboBoxColumn actionColumn = new DataGridViewComboBoxColumn
                {
                    Name = actionColumnName,
                    HeaderText = "操作",
                    ReadOnly = false,
                    Width = 96,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    FlatStyle = FlatStyle.Flat,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                    DisplayStyleForCurrentCellOnly = false
                };
                actionColumn.Items.AddRange("下载", "删除", "重命名");
                actionColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                actionColumn.DefaultCellStyle.NullValue = "下载";
                grid.Columns.Add(actionColumn);
                grid.Columns[7].Visible = false;
                grid.Columns[8].Visible = false;
            }
            grid.ReadOnly = false;
            if (!fileActionColumnConfigured)
            {
                fileActionColumnConfigured = true;
                grid.CellValueChanged += file_list_dataGridView_ActionChanged;
                grid.CurrentCellDirtyStateChanged += delegate
                {
                    if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
                    {
                        grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    }
                };
            }

            const string directoryColumnName = "ParentDirectoryColumn";
            if (!grid.Columns.Contains(directoryColumnName))
            {
                DataGridViewTextBoxColumn directoryColumn = new DataGridViewTextBoxColumn
                {
                    Name = directoryColumnName,
                    HeaderText = "所属目录",
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = 110,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
                };
                grid.Columns.Add(directoryColumn);
                // 仅调整显示顺序，不改变旧列索引，保证下载、删除和文件标识逻辑不受影响。
                directoryColumn.DisplayIndex = 3;
            }

            // 固定信息保持紧凑，把可利用的宽度优先留给文件名与完整路径。
            SetFixedGridColumn(grid.Columns[0], 72); // 选择
            SetFillGridColumn(grid.Columns[2], 96);  // 文件名称
            grid.Columns[3].Visible = false; // 文件路径列按需求去除（隐藏列，不改变索引以兼容代码）
            SetFixedGridColumn(grid.Columns[4], 78); // 文件大小
            SetFixedGridColumn(grid.Columns[5], 126); // 上传时间
            grid.Columns[6].Visible = false; // 状态列按需求去除（隐藏列，不改变索引以兼容代码）
            grid.Columns[7].Visible = false;
            grid.Columns[8].Visible = false;
            SetFixedGridColumn(grid.Columns[actionColumnName], 96);

            grid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void file_list_dataGridView_ActionChanged(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView grid = file_list_dataGridView;
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count) return;
            if (grid.Columns[e.ColumnIndex].Name != "ModernFileActionColumn") return;
            string action = Convert.ToString(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
            grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = null;
            if (string.IsNullOrWhiteSpace(action)) return;
            HandleModernFileAction(e.RowIndex, action);
        }

        private void SetFixedGridColumn(DataGridViewColumn column, int width)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.Width = width;
        }

        private void SetFillGridColumn(DataGridViewColumn column, float weight)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.FillWeight = weight;
        }

        private void BuildTransferTab(TabPage page, DataGridView grid, Button primary, Button clear, RichTextBox log, string titleText)
        {
            page.Controls.Clear();
            page.BackColor = UiTheme.Panel;
            page.Padding = new Padding(18, 14, 18, 14);
            Panel header = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = UiTheme.Panel };
            Label title = new Label { Text = titleText, Dock = DockStyle.Left, Width = 160, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("微软雅黑", 14F, FontStyle.Bold), ForeColor = UiTheme.TextMain };
            clear.Dock = DockStyle.Right;
            clear.Width = 105;
            primary.Dock = DockStyle.Right;
            primary.Width = 105;
            primary.Margin = new Padding(0, 0, 8, 0);
            header.Controls.Add(clear);
            header.Controls.Add(primary);
            header.Controls.Add(title);
            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            log.Dock = DockStyle.Bottom;
            log.Height = 115;
            log.BorderStyle = BorderStyle.None;
            log.BackColor = Color.White;
            log.ForeColor = UiTheme.TextSecondary;
            page.Controls.Add(grid);
            page.Controls.Add(log);
            page.Controls.Add(header);
        }

        private void DrawDirectoryNode(object sender, DrawTreeNodeEventArgs e)
        {
            TreeView tree = sender as TreeView;
            if (tree == null) return;

            bool selected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;
            Rectangle row = new Rectangle(0, e.Bounds.Top, tree.ClientSize.Width, tree.ItemHeight);
            using (SolidBrush rowBrush = new SolidBrush(selected ? Color.FromArgb(235, 243, 255) : Color.White))
                e.Graphics.FillRectangle(rowBrush, row);

            // 以节点层级而非文字边界计算位置，保证子目录永远在父目录右侧。
            int iconLeft = 14 + e.Node.Level * 26;
            if (e.Node.Nodes.Count > 0)
            {
                Rectangle arrow = new Rectangle(iconLeft, e.Bounds.Top + 8, 12, 16);
                TextRenderer.DrawText(e.Graphics, e.Node.IsExpanded ? "⌄" : "›", UiTheme.FontBody, arrow, UiTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            int folderLeft = iconLeft + 15;
            Rectangle folderTab = new Rectangle(folderLeft + 2, e.Bounds.Top + 8, 10, 5);
            Rectangle folderBody = new Rectangle(folderLeft, e.Bounds.Top + 11, 18, 13);
            Color folderColor = selected ? UiTheme.Primary : Color.FromArgb(93, 157, 255);
            using (SolidBrush folderBrush = new SolidBrush(folderColor))
            using (SolidBrush tabBrush = new SolidBrush(Color.FromArgb(147, 194, 255)))
            {
                e.Graphics.FillRectangle(tabBrush, folderTab);
                e.Graphics.FillRectangle(folderBrush, folderBody);
            }

            Rectangle textBounds = new Rectangle(folderLeft + 25, e.Bounds.Top, Math.Max(0, tree.ClientSize.Width - folderLeft - 28), tree.ItemHeight);
            TextRenderer.DrawText(e.Graphics, e.Node.Text, UiTheme.FontBody, textBounds, selected ? UiTheme.Primary : UiTheme.TextMain, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DirectoryTreeNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Nodes.Count == 0) return;
            TreeView tree = sender as TreeView;
            if (tree == null) return;

            int toggleAreaRight = 14 + e.Node.Level * 26 + 14;
            if (e.Location.X <= toggleAreaRight)
                e.Node.Toggle();
        }

        private void DrawEmptyFileList(object sender, PaintEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || grid.Rows.Count > 0) return;

            int contentHeight = grid.ClientSize.Height - grid.ColumnHeadersHeight;
            // 传输中心展开后，空状态保持在文件区上半部，避免靠近底部边界。
            int y = grid.ColumnHeadersHeight + Math.Max(28, (contentHeight - 220) / 2);
            Rectangle icon = new Rectangle((grid.ClientSize.Width - 58) / 2, y, 58, 46);
            using (SolidBrush folder = new SolidBrush(Color.FromArgb(225, 237, 255)))
            using (SolidBrush tab = new SolidBrush(Color.FromArgb(184, 211, 255)))
            {
                e.Graphics.FillRectangle(tab, icon.Left + 6, icon.Top, 25, 12);
                e.Graphics.FillRectangle(folder, icon.Left, icon.Top + 8, icon.Width, icon.Height - 8);
            }
            Rectangle title = new Rectangle(0, icon.Bottom + 12, grid.ClientSize.Width, 26);
            Rectangle hint = new Rectangle(0, title.Bottom, grid.ClientSize.Width, 28);
            TextRenderer.DrawText(e.Graphics, "这个文件夹还是空的", UiTheme.FontTitle, title, UiTheme.TextMain, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, "可从左侧目录上传文件或同步最新内容", UiTheme.FontBody, hint, UiTheme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void PrepareToolButton(Button button, string text, int width)
        {
            button.Text = text;
            button.Size = new Size(width, 34);
            button.Margin = new Padding(0, 0, 8, 0);
        }

        // SplitContainer 在构造阶段仍是默认宽度，等它获得父容器尺寸后再应用统一宽度。
        private void SetInitialSplitterDistance(SplitContainer split, int preferredWidth)
        {
            EventHandler applyDistance = null;
            applyDistance = delegate
            {
                int maximum = split.ClientSize.Width - split.SplitterWidth - split.Panel2MinSize;
                if (maximum < split.Panel1MinSize)
                    return;

                split.SplitterDistance = Math.Max(split.Panel1MinSize, Math.Min(preferredWidth, maximum));
                split.HandleCreated -= applyDistance;
                split.SizeChanged -= applyDistance;
            };

            split.HandleCreated += applyDistance;
            split.SizeChanged += applyDistance;
        }

        private void SelectMainSection(int mainIndex, int fileIndex)
        {
            main_tabControl.SelectedIndex = mainIndex;
            SetNavState(navChatButton, mainIndex == 0);
            SetNavState(navDriveButton, mainIndex == 1 && fileIndex == 0);
            SetNavState(navUploadButton, mainIndex == 2);
            SetNavState(navDownloadButton, mainIndex == 3);
        }

        private void SetNavState(Button button, bool active)
        {
            if (button == null) return;
            button.BackColor = active ? UiTheme.Primary : Color.Transparent;
            button.ForeColor = active ? Color.White : Color.FromArgb(174, 191, 216);
        }
    }

    public partial class Login_Register_Form
    {
        private void BuildModernLoginLayout()
        {
            SuspendLayout();
            Text = "云聊空间 - 登录";
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            int loginWidth = Math.Max(720, Math.Min(900, (int)(workArea.Width * 0.54f)));
            int loginHeight = Math.Max(430, Math.Min(520, (int)(workArea.Height * 0.50f)));
            ClientSize = new Size(loginWidth, loginHeight);
            MinimumSize = new Size(720, 430);
            MaximumSize = Size.Empty;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = true;
            BackColor = Color.White;
            Font = UiTheme.FontBody;
            AcceptButton = login_button;

            Controls.Clear();
            // 所有主区域都交给 TableLayoutPanel 分配：窗口缩放或 DPI 缩放时不会再依赖固定坐标。
            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.White };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39F));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61F));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel brand = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 36, 58), Padding = new Padding(30, 34, 30, 30) };
            TableLayoutPanel brandLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
            brandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            Label mark = new Label { Text = "C", Dock = DockStyle.Left, Width = 58, TextAlign = ContentAlignment.MiddleCenter, BackColor = UiTheme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 22F, FontStyle.Bold) };
            Label brandTitle = new Label { Text = "云聊空间", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.White, Font = new Font("微软雅黑", 24F, FontStyle.Bold) };
            Label brandSub = new Label { Text = "聊天、文件共享与个人网盘\r\n统一桌面工作空间", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(174, 191, 216), Font = new Font("微软雅黑", 10.5F) };
            brandLayout.Controls.Add(mark, 0, 0);
            brandLayout.Controls.Add(brandTitle, 0, 1);
            brandLayout.Controls.Add(brandSub, 0, 2);
            brandLayout.Controls.Add(CreateLoginFeature("✓  实时消息与在线联系人"), 0, 4);
            brandLayout.Controls.Add(CreateLoginFeature("✓  网盘文件与聊天无缝共享"), 0, 5);
            brandLayout.Controls.Add(CreateLoginFeature("✓  上传下载任务集中管理"), 0, 6);
            brand.Controls.Add(brandLayout);

            TableLayoutPanel form = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(38, 32, 38, 18), ColumnCount = 2, RowCount = 14, AutoScroll = true };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            label3.Text = "欢迎登录";
            label3.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            label3.ForeColor = UiTheme.TextMain;
            label3.Dock = DockStyle.Fill;
            label3.TextAlign = ContentAlignment.MiddleLeft;
            Label hint = new Label { Text = "登录后继续使用云聊空间与个人网盘", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.TextSecondary, Font = new Font("微软雅黑", 9.5F) };
            label1.Text = "用户名";
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("微软雅黑", 9.5F);
            label1.ForeColor = UiTheme.TextMain;
            label1.TextAlign = ContentAlignment.MiddleLeft;
            label2.Text = "密码";
            label2.Dock = DockStyle.Fill;
            label2.Font = new Font("微软雅黑", 9.5F);
            label2.ForeColor = UiTheme.TextMain;
            label2.TextAlign = ContentAlignment.MiddleLeft;
            userName_textBox.Dock = DockStyle.Fill;
            userName_textBox.Margin = new Padding(0);
            userName_textBox.Font = new Font("Segoe UI", 11F);
            userName_textBox.BorderStyle = BorderStyle.FixedSingle;
            password_textBox.Dock = DockStyle.Fill;
            password_textBox.Margin = new Padding(0);
            password_textBox.Font = new Font("Segoe UI", 11F);
            password_textBox.BorderStyle = BorderStyle.FixedSingle;
            password_textBox.UseSystemPasswordChar = true;
            login_button.Dock = DockStyle.Fill;
            // 让登录按钮和下一行操作按钮之间保留明确的呼吸空间。
            login_button.Margin = new Padding(0, 0, 0, 8);
            login_button.Text = "登录";
            UiTheme.StyleButton(login_button, UiTheme.Kind.Primary);
            register_button.Dock = DockStyle.Fill;
            register_button.Margin = new Padding(0);
            register_button.Text = "注册账号";
            UiTheme.StyleButton(register_button, UiTheme.Kind.Default);
            setting_label.Dock = DockStyle.None;
            setting_label.AutoSize = false;
            setting_label.Size = new Size(120, 26);
            setting_label.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            setting_label.Margin = new Padding(0, 0, 10, 8);
            setting_label.Text = "⚙ 服务器设置";
            setting_label.TextAlign = ContentAlignment.MiddleCenter;
            setting_label.ForeColor = UiTheme.Primary;
            setting_label.Cursor = Cursors.Hand;
            connect_label.Dock = DockStyle.Fill;
            connect_label.ForeColor = UiTheme.Success;
            connect_label.Font = new Font("微软雅黑", 9F);
            connect_label.TextAlign = ContentAlignment.MiddleLeft;
            form.Controls.Add(label3, 0, 0);
            form.SetColumnSpan(label3, 2);
            form.Controls.Add(hint, 0, 1);
            form.SetColumnSpan(hint, 2);
            form.Controls.Add(label1, 0, 3);
            form.SetColumnSpan(label1, 2);
            form.Controls.Add(userName_textBox, 0, 4);
            form.SetColumnSpan(userName_textBox, 2);
            form.Controls.Add(label2, 0, 6);
            form.SetColumnSpan(label2, 2);
            form.Controls.Add(password_textBox, 0, 7);
            form.SetColumnSpan(password_textBox, 2);
            form.Controls.Add(login_button, 0, 9);
            form.SetColumnSpan(login_button, 2);
            form.Controls.Add(register_button, 0, 10);
            form.SetColumnSpan(register_button, 2);
            form.Controls.Add(connect_label, 0, 12);
            form.SetColumnSpan(connect_label, 2);
            // 服务器设置固定在表单右下角，保留明确的右侧和底部安全距离。
            form.Controls.Add(setting_label, 0, 13);
            form.SetColumnSpan(setting_label, 2);
            shell.Controls.Add(brand, 0, 0);
            shell.Controls.Add(form, 1, 0);
            Controls.Add(shell);
            ResumeLayout(true);
        }

        private Label CreateLoginFeature(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(203, 214, 232), Font = new Font("微软雅黑", 9.5F) };
        }
    }
}
