using chat_service.net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.async
{
    class AsyncTaskRemoteConnnect
    {
        // 定义个委托,委托开启远程连接
        public delegate NetResponse RemoteConntect();
        // 执行远程调用
        private RemoteConntect rc = null; 
        // 异步调用结果返回
        private NetResponse connectNetResponse = null;
        // 后台任务
        private static BackgroundWorker bgWorker = new BackgroundWorker();

        // 异步回调结果
        private void AsyncCallbackImpl(IAsyncResult ar)
        {
            //获取执行完后的返回值
            this.connectNetResponse = rc.EndInvoke(ar);

            // 跨线程访问组件
            bgWorker.RunWorkerAsync(connectNetResponse);

            //throw new NotImplementedException();
        }

        public static void initAsyncTask()
        {
            /*bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgessChanged);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_WorkerCompleted);*/
        }

        public void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }

        public void bgWorker_ProgessChanged(object sender, ProgressChangedEventArgs e)
        {
            //string state = (string)e.UserState;//接收ReportProgress方法传递过来的userState
            //this.progressBar1.Value = e.ProgressPercentage;
            //this.label1.Text = "处理进度:" + Convert.ToString(e.ProgressPercentage) + "%";
        }

    }
}
