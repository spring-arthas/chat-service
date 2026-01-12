namespace chat_service
{
    partial class Register_Form
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
            this.login_button = new System.Windows.Forms.Button();
            this.userName_textBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.password_textBox = new System.Windows.Forms.TextBox();
            this.phone_textBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.mail_textBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // login_button
            // 
            this.login_button.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.login_button.Location = new System.Drawing.Point(198, 268);
            this.login_button.Name = "login_button";
            this.login_button.Size = new System.Drawing.Size(193, 42);
            this.login_button.TabIndex = 10;
            this.login_button.Text = "注册";
            this.login_button.UseVisualStyleBackColor = true;
            this.login_button.Click += new System.EventHandler(this.login_button_Click);
            // 
            // userName_textBox
            // 
            this.userName_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.userName_textBox.Location = new System.Drawing.Point(198, 57);
            this.userName_textBox.Name = "userName_textBox";
            this.userName_textBox.Size = new System.Drawing.Size(193, 29);
            this.userName_textBox.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(113, 113);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 19);
            this.label2.TabIndex = 7;
            this.label2.Text = "密码";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(113, 60);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 19);
            this.label1.TabIndex = 6;
            this.label1.Text = "用户名";
            // 
            // password_textBox
            // 
            this.password_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.password_textBox.Location = new System.Drawing.Point(198, 110);
            this.password_textBox.Name = "password_textBox";
            this.password_textBox.Size = new System.Drawing.Size(193, 29);
            this.password_textBox.TabIndex = 12;
            // 
            // phone_textBox
            // 
            this.phone_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.phone_textBox.Location = new System.Drawing.Point(198, 162);
            this.phone_textBox.Name = "phone_textBox";
            this.phone_textBox.Size = new System.Drawing.Size(193, 29);
            this.phone_textBox.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(113, 165);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 19);
            this.label3.TabIndex = 13;
            this.label3.Text = "联系方式";
            // 
            // mail_textBox
            // 
            this.mail_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.mail_textBox.Location = new System.Drawing.Point(198, 211);
            this.mail_textBox.Name = "mail_textBox";
            this.mail_textBox.Size = new System.Drawing.Size(193, 29);
            this.mail_textBox.TabIndex = 16;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(113, 214);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 19);
            this.label4.TabIndex = 15;
            this.label4.Text = "邮箱";
            // 
            // Register_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(548, 338);
            this.Controls.Add(this.mail_textBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.phone_textBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.password_textBox);
            this.Controls.Add(this.login_button);
            this.Controls.Add(this.userName_textBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "Register_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "用户注册";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button login_button;
        private System.Windows.Forms.TextBox userName_textBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox password_textBox;
        private System.Windows.Forms.TextBox phone_textBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox mail_textBox;
        private System.Windows.Forms.Label label4;
    }
}