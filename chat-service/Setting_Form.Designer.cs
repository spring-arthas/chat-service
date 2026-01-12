namespace chat_service
{
    partial class Setting_Form
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
            this.label3 = new System.Windows.Forms.Label();
            this.Remote_Server_textBox = new System.Windows.Forms.TextBox();
            this.login_button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(14, 29);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 19);
            this.label3.TabIndex = 14;
            this.label3.Text = "服务地址";
            // 
            // Remote_Server_textBox
            // 
            this.Remote_Server_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Remote_Server_textBox.Location = new System.Drawing.Point(107, 26);
            this.Remote_Server_textBox.Name = "Remote_Server_textBox";
            this.Remote_Server_textBox.Size = new System.Drawing.Size(193, 29);
            this.Remote_Server_textBox.TabIndex = 15;
            // 
            // login_button
            // 
            this.login_button.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.login_button.Location = new System.Drawing.Point(306, 24);
            this.login_button.Name = "login_button";
            this.login_button.Size = new System.Drawing.Size(89, 32);
            this.login_button.TabIndex = 16;
            this.login_button.Text = "连接";
            this.login_button.UseVisualStyleBackColor = true;
            this.login_button.Click += new System.EventHandler(this.login_button_Click);
            // 
            // Setting_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(418, 86);
            this.Controls.Add(this.login_button);
            this.Controls.Add(this.Remote_Server_textBox);
            this.Controls.Add(this.label3);
            this.MaximizeBox = false;
            this.Name = "Setting_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "配置服务地址";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox Remote_Server_textBox;
        private System.Windows.Forms.Button login_button;
    }
}