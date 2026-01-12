namespace chat_service
{
    partial class Wait_Close_Form
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
            this.tip_label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tip_label
            // 
            this.tip_label.AutoSize = true;
            this.tip_label.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tip_label.ForeColor = System.Drawing.Color.Orange;
            this.tip_label.Location = new System.Drawing.Point(29, 41);
            this.tip_label.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.tip_label.Name = "tip_label";
            this.tip_label.Size = new System.Drawing.Size(267, 19);
            this.tip_label.TabIndex = 13;
            this.tip_label.Text = "正在结束传输任务，请稍后...";
            // 
            // Wait_Close_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(317, 109);
            this.Controls.Add(this.tip_label);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Wait_Close_Form";
            this.Text = "提示";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Wait_Close_Form_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label tip_label;
    }
}