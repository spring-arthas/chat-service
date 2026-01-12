namespace chat_service
{
    partial class File_Create_Form
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
            this.file_create_button = new System.Windows.Forms.Button();
            this.new_file_textBox = new System.Windows.Forms.TextBox();
            this.remote_address_label = new System.Windows.Forms.Label();
            this.parent_file_node_label = new System.Windows.Forms.Label();
            this.create_description_label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // file_create_button
            // 
            this.file_create_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.file_create_button.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.file_create_button.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.file_create_button.Location = new System.Drawing.Point(297, 70);
            this.file_create_button.Name = "file_create_button";
            this.file_create_button.Size = new System.Drawing.Size(89, 32);
            this.file_create_button.TabIndex = 26;
            this.file_create_button.Text = "保存";
            this.file_create_button.UseVisualStyleBackColor = true;
            this.file_create_button.Click += new System.EventHandler(this.file_create_button_Click);
            // 
            // new_file_textBox
            // 
            this.new_file_textBox.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.new_file_textBox.Location = new System.Drawing.Point(89, 72);
            this.new_file_textBox.Name = "new_file_textBox";
            this.new_file_textBox.Size = new System.Drawing.Size(193, 29);
            this.new_file_textBox.TabIndex = 27;
            // 
            // remote_address_label
            // 
            this.remote_address_label.AutoSize = true;
            this.remote_address_label.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.remote_address_label.Location = new System.Drawing.Point(14, 75);
            this.remote_address_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.remote_address_label.Name = "remote_address_label";
            this.remote_address_label.Size = new System.Drawing.Size(67, 19);
            this.remote_address_label.TabIndex = 28;
            this.remote_address_label.Text = "名称: ";
            // 
            // parent_file_node_label
            // 
            this.parent_file_node_label.AutoSize = true;
            this.parent_file_node_label.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.parent_file_node_label.Location = new System.Drawing.Point(14, 25);
            this.parent_file_node_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.parent_file_node_label.Name = "parent_file_node_label";
            this.parent_file_node_label.Size = new System.Drawing.Size(152, 19);
            this.parent_file_node_label.TabIndex = 29;
            this.parent_file_node_label.Text = "当前所属文件夹:";
            // 
            // create_description_label
            // 
            this.create_description_label.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.create_description_label.AutoSize = true;
            this.create_description_label.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.create_description_label.ForeColor = System.Drawing.Color.Red;
            this.create_description_label.Location = new System.Drawing.Point(16, 114);
            this.create_description_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.create_description_label.Name = "create_description_label";
            this.create_description_label.Size = new System.Drawing.Size(112, 14);
            this.create_description_label.TabIndex = 30;
            this.create_description_label.Text = "当前所属文件夹:";
            this.create_description_label.Visible = false;
            // 
            // File_Create_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(398, 141);
            this.Controls.Add(this.create_description_label);
            this.Controls.Add(this.parent_file_node_label);
            this.Controls.Add(this.remote_address_label);
            this.Controls.Add(this.new_file_textBox);
            this.Controls.Add(this.file_create_button);
            this.Name = "File_Create_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "创建/修改文件夹";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button file_create_button;
        public System.Windows.Forms.TextBox new_file_textBox;
        private System.Windows.Forms.Label remote_address_label;
        public System.Windows.Forms.Label parent_file_node_label;
        public System.Windows.Forms.Label create_description_label;
    }
}