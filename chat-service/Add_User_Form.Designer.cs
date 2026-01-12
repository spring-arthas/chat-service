namespace chat_service
{
    partial class Add_User_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.userName_textBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.query_user_button = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.query_result_label = new System.Windows.Forms.Label();
            this.user_list_dataGridView = new System.Windows.Forms.DataGridView();
            this.传送文件 = new System.Windows.Forms.DataGridViewButtonColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.index = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.userName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.loginDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.registerDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.user_list_dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // userName_textBox
            // 
            this.userName_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.userName_textBox.Location = new System.Drawing.Point(77, 15);
            this.userName_textBox.Name = "userName_textBox";
            this.userName_textBox.Size = new System.Drawing.Size(193, 29);
            this.userName_textBox.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(7, 19);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 19);
            this.label1.TabIndex = 3;
            this.label1.Text = "用户名";
            // 
            // query_user_button
            // 
            this.query_user_button.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.query_user_button.Location = new System.Drawing.Point(281, 14);
            this.query_user_button.Name = "query_user_button";
            this.query_user_button.Size = new System.Drawing.Size(89, 32);
            this.query_user_button.TabIndex = 34;
            this.query_user_button.Text = "搜索";
            this.query_user_button.UseVisualStyleBackColor = true;
            this.query_user_button.Click += new System.EventHandler(this.query_user_button_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ForeColor = System.Drawing.Color.Green;
            this.label2.Location = new System.Drawing.Point(-4, 60);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 19);
            this.label2.TabIndex = 37;
            this.label2.Text = "| 搜索结果";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // query_result_label
            // 
            this.query_result_label.AutoSize = true;
            this.query_result_label.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.query_result_label.ForeColor = System.Drawing.Color.Green;
            this.query_result_label.Location = new System.Drawing.Point(122, 60);
            this.query_result_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.query_result_label.Name = "query_result_label";
            this.query_result_label.Size = new System.Drawing.Size(0, 19);
            this.query_result_label.TabIndex = 38;
            this.query_result_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // user_list_dataGridView
            // 
            this.user_list_dataGridView.AllowUserToAddRows = false;
            this.user_list_dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.user_list_dataGridView.BackgroundColor = System.Drawing.Color.Cornsilk;
            this.user_list_dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.user_list_dataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.user_list_dataGridView.ColumnHeadersHeight = 40;
            this.user_list_dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.index,
            this.userName,
            this.loginDate,
            this.registerDate,
            this.status,
            this.传送文件});
            this.user_list_dataGridView.GridColor = System.Drawing.SystemColors.Control;
            this.user_list_dataGridView.Location = new System.Drawing.Point(0, 86);
            this.user_list_dataGridView.MultiSelect = false;
            this.user_list_dataGridView.Name = "user_list_dataGridView";
            this.user_list_dataGridView.ReadOnly = true;
            this.user_list_dataGridView.RowHeadersWidth = 60;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.user_list_dataGridView.RowsDefaultCellStyle = dataGridViewCellStyle6;
            this.user_list_dataGridView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.user_list_dataGridView.RowTemplate.Height = 30;
            this.user_list_dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.user_list_dataGridView.Size = new System.Drawing.Size(881, 445);
            this.user_list_dataGridView.TabIndex = 39;
            // 
            // 传送文件
            // 
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.NullValue = "传送文件";
            this.传送文件.DefaultCellStyle = dataGridViewCellStyle5;
            this.传送文件.HeaderText = "操作";
            this.传送文件.Name = "传送文件";
            this.传送文件.ReadOnly = true;
            this.传送文件.Text = "";
            this.传送文件.Width = 121;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.FillWeight = 213.198F;
            this.dataGridViewTextBoxColumn1.HeaderText = "序号";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 70;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.FillWeight = 77.3604F;
            this.dataGridViewTextBoxColumn2.HeaderText = "在线用户";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 160;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.FillWeight = 77.3604F;
            this.dataGridViewTextBoxColumn3.HeaderText = "登陆时间";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 190;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.FillWeight = 77.3604F;
            this.dataGridViewTextBoxColumn4.HeaderText = "注册日期";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 190;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.FillWeight = 77.3604F;
            this.dataGridViewTextBoxColumn5.HeaderText = "状态";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 90;
            // 
            // index
            // 
            this.index.FillWeight = 213.198F;
            this.index.HeaderText = "序号";
            this.index.Name = "index";
            this.index.ReadOnly = true;
            this.index.Width = 70;
            // 
            // userName
            // 
            this.userName.FillWeight = 77.3604F;
            this.userName.HeaderText = "在线用户";
            this.userName.Name = "userName";
            this.userName.ReadOnly = true;
            this.userName.Width = 160;
            // 
            // loginDate
            // 
            this.loginDate.FillWeight = 77.3604F;
            this.loginDate.HeaderText = "登陆时间";
            this.loginDate.Name = "loginDate";
            this.loginDate.ReadOnly = true;
            this.loginDate.Width = 190;
            // 
            // registerDate
            // 
            this.registerDate.FillWeight = 77.3604F;
            this.registerDate.HeaderText = "注册日期";
            this.registerDate.Name = "registerDate";
            this.registerDate.ReadOnly = true;
            this.registerDate.Width = 190;
            // 
            // status
            // 
            this.status.FillWeight = 77.3604F;
            this.status.HeaderText = "状态";
            this.status.Name = "status";
            this.status.ReadOnly = true;
            this.status.Width = 90;
            // 
            // Add_User_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(882, 536);
            this.Controls.Add(this.user_list_dataGridView);
            this.Controls.Add(this.query_result_label);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.query_user_button);
            this.Controls.Add(this.userName_textBox);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "Add_User_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "添加好友";
            ((System.ComponentModel.ISupportInitialize)(this.user_list_dataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.TextBox userName_textBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button query_user_button;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label query_result_label;
        public System.Windows.Forms.DataGridView user_list_dataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn index;
        private System.Windows.Forms.DataGridViewTextBoxColumn userName;
        private System.Windows.Forms.DataGridViewTextBoxColumn loginDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn registerDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn status;
        private System.Windows.Forms.DataGridViewButtonColumn 传送文件;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
    }
}