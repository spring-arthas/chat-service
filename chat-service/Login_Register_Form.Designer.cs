namespace chat_service
{
    partial class Login_Register_Form
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.userName_textBox = new System.Windows.Forms.TextBox();
            this.password_textBox = new System.Windows.Forms.TextBox();
            this.login_button = new System.Windows.Forms.Button();
            this.register_button = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.setting_label = new System.Windows.Forms.Label();
            this.connect_label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(79, 106);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "用户名";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(79, 159);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 29);
            this.label2.TabIndex = 1;
            this.label2.Text = "密码";
            // 
            // userName_textBox
            // 
            this.userName_textBox.Location = new System.Drawing.Point(164, 103);
            this.userName_textBox.Name = "userName_textBox";
            this.userName_textBox.Size = new System.Drawing.Size(193, 40);
            this.userName_textBox.TabIndex = 2;
            this.userName_textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.userName_textBox_KeyDown);
            // 
            // password_textBox
            // 
            this.password_textBox.Location = new System.Drawing.Point(164, 156);
            this.password_textBox.Name = "password_textBox";
            this.password_textBox.Size = new System.Drawing.Size(193, 40);
            this.password_textBox.TabIndex = 3;
            this.password_textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.password_textBox_KeyDown);
            // 
            // login_button
            // 
            this.login_button.Location = new System.Drawing.Point(97, 231);
            this.login_button.Name = "login_button";
            this.login_button.Size = new System.Drawing.Size(112, 37);
            this.login_button.TabIndex = 4;
            this.login_button.Text = "登录";
            this.login_button.UseVisualStyleBackColor = true;
            this.login_button.Click += new System.EventHandler(this.login_button_Click);
            // 
            // register_button
            // 
            this.register_button.Location = new System.Drawing.Point(259, 231);
            this.register_button.Name = "register_button";
            this.register_button.Size = new System.Drawing.Size(112, 37);
            this.register_button.TabIndex = 5;
            this.register_button.Text = "注册";
            this.register_button.UseVisualStyleBackColor = true;
            this.register_button.Click += new System.EventHandler(this.register_button_Click);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(128, 36);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(327, 44);
            this.label3.TabIndex = 6;
            this.label3.Text = "Nio 网盘客户端";
            // 
            // setting_label
            // 
            this.setting_label.AutoSize = true;
            this.setting_label.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.setting_label.Location = new System.Drawing.Point(419, 9);
            this.setting_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.setting_label.Name = "setting_label";
            this.setting_label.Size = new System.Drawing.Size(52, 21);
            this.setting_label.TabIndex = 7;
            this.setting_label.Text = "设置";
            this.setting_label.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.setting_label.Click += new System.EventHandler(this.setting_label_Click);
            // 
            // connect_label
            // 
            this.connect_label.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.connect_label.AutoSize = true;
            this.connect_label.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.connect_label.Location = new System.Drawing.Point(4, 290);
            this.connect_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.connect_label.Name = "connect_label";
            this.connect_label.Size = new System.Drawing.Size(94, 21);
            this.connect_label.TabIndex = 8;
            this.connect_label.Text = "连接结果";
            this.connect_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.connect_label.Visible = false;
            // 
            // Login_Register_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(484, 313);
            this.Controls.Add(this.connect_label);
            this.Controls.Add(this.setting_label);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.register_button);
            this.Controls.Add(this.login_button);
            this.Controls.Add(this.password_textBox);
            this.Controls.Add(this.userName_textBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximizeBox = false;
            this.Name = "Login_Register_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Nio客户端";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Login_Register_Form_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button login_button;
        private System.Windows.Forms.Button register_button;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label setting_label;
        public System.Windows.Forms.TextBox userName_textBox;
        public System.Windows.Forms.TextBox password_textBox;
        public System.Windows.Forms.Label connect_label;
    }
}

