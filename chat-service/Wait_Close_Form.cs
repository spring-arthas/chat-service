using chat_service.file;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace chat_service
{
    public partial class Wait_Close_Form : Form
    {
        private List<AsyncPersonalFileUploadHelper> uploadHelper;

        private List<AsyncPersonalFileDownloadHelper> downloadHelper;

        public Wait_Close_Form()
        {
            InitializeComponent();
        }

        public Wait_Close_Form(List<AsyncPersonalFileUploadHelper> uploadHelper, List<AsyncPersonalFileDownloadHelper> downloadHelper)
        {
            InitializeComponent();
            this.uploadHelper = uploadHelper;
            this.downloadHelper = downloadHelper;

            executeFinishTask();
        }

        public void executeFinishTask()
        {
            // 判断上传任务
            if (uploadHelper.Count > 0)
            {
                for (int i = 0; i < uploadHelper.Count; i++)
                {
                    AsyncPersonalFileUploadHelper helper = uploadHelper[i];
                    if (helper.Bg_Worker.IsBusy)
                    {
                        // 执行异步取消
                        helper.Bg_Worker.CancelAsync();

                        // 等待结果
                        while (true)
                        {
                            if (helper.doWorkEventArgs.Cancel)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                    }
                }
            }

            if (downloadHelper.Count > 0)
            {
                for (int i = 0; i < downloadHelper.Count; i++)
                {
                    AsyncPersonalFileDownloadHelper helper = downloadHelper[i];
                    if (helper.Bg_Worker.IsBusy)
                    {
                        // 执行异步取消
                        helper.Bg_Worker.CancelAsync();

                        // 等待结果
                        while (true)
                        {
                            if (helper.doWorkEventArgs.Cancel)
                            {
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                    }
                }
            }

            // 释放静态资源
            uploadHelper.Clear();
            downloadHelper.Clear();
            this.Close();
        }

        private void Wait_Close_Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (uploadHelper.Count > 0 || downloadHelper.Count > 0)
            {
                e.Cancel = true;
            }
        }
    }
}
