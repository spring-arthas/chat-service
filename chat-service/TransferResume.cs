using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using chat_service.util;

namespace chat_service
{
    /// <summary>
    /// 文件断点续传（暂停/继续）与重传协作逻辑。
    /// 基于服务端 net-server 的断点持久化：
    ///   - 上传：服务端按 md5+用户 增量保存 checkpoint/PAUSED 任务，暂停后再传即可从断点续传；
    ///   - 下载：客户端按本地 .part 文件大小作为续传偏移；
    ///   - 暂停/重传通过 CancellationToken 中断，服务类的 using/finally 会释放 socket 与文件流。
    /// </summary>
    public partial class Main_Form
    {
        /// <summary>单个传输任务的状态，供暂停/继续/重传使用。</summary>
        private sealed class TransferTaskState
        {
            public string Key;                 // "upload:<filePath>" 或 "download:<fileId>|<fileName>"
            public string Kind;                // "upload" / "download"
            public string TaskId;              // 上传任务标识（服务端按 md5 落断点，taskId 主要作协议匹配）
            public string SourcePath;          // 上传：本地源文件路径
            public string DownloadPath;        // 下载：目标完整路径（含 .part）
            public long DirId;                 // 上传目标目录
            public long FileId;                // 下载文件ID
            public string FileName;
            public long FileSize;
            public CancellationTokenSource Cts; // 暂停/重传用
            public volatile int LastProgress;
            public volatile bool Cancelled;
        }

        private readonly Dictionary<string, TransferTaskState> transferTaskStates = new Dictionary<string, TransferTaskState>();

        // ==================== 状态登记 ====================

        private TransferTaskState GetOrCreateUploadState(string filePath, long dirId)
        {
            string key = "upload:" + filePath;
            TransferTaskState st;
            if (!transferTaskStates.TryGetValue(key, out st))
            {
                st = new TransferTaskState { Key = key, Kind = "upload", SourcePath = filePath, DirId = dirId, TaskId = System.Guid.NewGuid().ToString("N") };
                transferTaskStates[key] = st;
            }
            else st.DirId = dirId;
            return st;
        }

        private TransferTaskState GetOrCreateDownloadState(long fileId, string fileName, string targetPath, long fileSize)
        {
            string key = "download:" + fileId + "|" + fileName;
            TransferTaskState st;
            if (!transferTaskStates.TryGetValue(key, out st))
            {
                st = new TransferTaskState { Key = key, Kind = "download", FileId = fileId, FileName = fileName, DownloadPath = targetPath, FileSize = fileSize };
                transferTaskStates[key] = st;
            }
            return st;
        }

        private TransferTaskState FindUploadState(string filePath)
        {
            TransferTaskState st;
            return transferTaskStates.TryGetValue("upload:" + filePath, out st) ? st : null;
        }

        private TransferTaskState FindDownloadState(long fileId, string fileName)
        {
            TransferTaskState st;
            return transferTaskStates.TryGetValue("download:" + fileId + "|" + fileName, out st) ? st : null;
        }

        // ==================== 暂停 / 继续 / 重传（上传） ====================

        public void PauseUpload(string filePath)
        {
            TransferTaskState st = FindUploadState(filePath);
            if (st == null) return;
            st.Cancelled = true;
            try { if (st.Cts != null) st.Cts.Cancel(); } catch { }
        }

        public void ContinueUpload(string filePath, chat_service.protocol.UserDO user)
        {
            TransferTaskState st = FindUploadState(filePath);
            long dirId = st != null ? st.DirId : 0;
            if (st != null) { st.Cancelled = false; st.Cts = new CancellationTokenSource(); }
            QueueUploadFile(filePath, dirId, user);
        }

        public void RetransmitUpload(string filePath, chat_service.protocol.UserDO user)
        {
            TransferTaskState st = FindUploadState(filePath);
            long dirId = st != null ? st.DirId : 0;
            if (st != null)
            {
                st.Cancelled = true;
                try { if (st.Cts != null) st.Cts.Cancel(); } catch { }
                WaitForCancellation(st);
                // 服务端按 md5+用户 落断点；生成新 taskId，重传会从服务端保留的断点续传。
                st.TaskId = System.Guid.NewGuid().ToString("N");
                st.Cancelled = false;
                st.Cts = new CancellationTokenSource();
            }
            QueueUploadFile(filePath, dirId, user);
        }

        // ==================== 暂停 / 继续 / 重传（下载） ====================

        public void PauseDownload(long fileId, string fileName)
        {
            TransferTaskState st = FindDownloadState(fileId, fileName);
            if (st == null) return;
            st.Cancelled = true;
            try { if (st.Cts != null) st.Cts.Cancel(); } catch { }
        }

        public void ContinueDownload(long fileId, string fileName, long fileSize)
        {
            string targetPath = Path.Combine(NetServiceContext.globalDownloadPath ?? "", fileName);
            TransferTaskState st = GetOrCreateDownloadState(fileId, fileName, targetPath, fileSize);
            st.Cancelled = false;
            st.Cts = new CancellationTokenSource();
            UploadOnUi(delegate { DownloadSingleFile(fileId, fileName, fileSize); });
        }

        public void RetransmitDownload(long fileId, string fileName, long fileSize)
        {
            string targetPath = Path.Combine(NetServiceContext.globalDownloadPath ?? "", fileName);
            TransferTaskState st = GetOrCreateDownloadState(fileId, fileName, targetPath, fileSize);
            st.Cancelled = true;
            try { if (st.Cts != null) st.Cts.Cancel(); } catch { }
            WaitForCancellation(st);
            // 删除本地 .part，强制从 0 重传。
            string partPath = targetPath + ".part";
            try { if (File.Exists(partPath)) File.Delete(partPath); } catch { }
            st.Cancelled = false;
            st.Cts = new CancellationTokenSource();
            UploadOnUi(delegate { DownloadSingleFile(fileId, fileName, fileSize); });
        }

        // ==================== 传输中心网格操作分发 ====================

        private void UnifiedTransferGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || unifiedTransferGrid == null) return;
            DataGridView grid = unifiedTransferGrid;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count) return;
            string name = Convert.ToString(grid.Rows[e.RowIndex].Cells[0].Value);
            string status = Convert.ToString(grid.Rows[e.RowIndex].Cells[2].Value);
            if (string.IsNullOrEmpty(name)) return;

            if (e.ColumnIndex == 4) // 暂停 / 继续
            {
                bool paused = status.Contains("暂停") || status.Contains("排队");
                if (paused) ResumeTransferByName(name);
                else PauseTransferByName(name);
            }
            else if (e.ColumnIndex == 5) // 重传
            {
                RetransmitTransferByName(name);
            }
        }

        private void PauseTransferByName(string name)
        {
            DataGridViewRow up = FindUploadRowByName(name);
            if (up != null)
            {
                string path = Convert.ToString(up.Cells[13].Value);
                if (!string.IsNullOrEmpty(path)) PauseUpload(path);
                return;
            }
            DataGridViewRow dn = FindDownloadRowByName(name);
            if (dn != null)
            {
                long fid; long.TryParse(Convert.ToString(dn.Cells[8].Value), out fid);
                PauseDownload(fid, Convert.ToString(dn.Cells[1].Value));
            }
        }

        private void ResumeTransferByName(string name)
        {
            DataGridViewRow up = FindUploadRowByName(name);
            if (up != null)
            {
                string path = Convert.ToString(up.Cells[13].Value);
                if (!string.IsNullOrEmpty(path) && currentUser != null) ContinueUpload(path, currentUser);
                return;
            }
            DataGridViewRow dn = FindDownloadRowByName(name);
            if (dn != null)
            {
                long fid; long.TryParse(Convert.ToString(dn.Cells[8].Value), out fid);
                long fsize; try { fsize = long.Parse(Convert.ToString(dn.Cells[13].Value)); } catch { fsize = 0; }
                ContinueDownload(fid, Convert.ToString(dn.Cells[1].Value), fsize);
            }
        }

        private void RetransmitTransferByName(string name)
        {
            DataGridViewRow up = FindUploadRowByName(name);
            if (up != null)
            {
                string path = Convert.ToString(up.Cells[13].Value);
                if (!string.IsNullOrEmpty(path) && currentUser != null) RetransmitUpload(path, currentUser);
                return;
            }
            DataGridViewRow dn = FindDownloadRowByName(name);
            if (dn != null)
            {
                long fid; long.TryParse(Convert.ToString(dn.Cells[8].Value), out fid);
                long fsize; try { fsize = long.Parse(Convert.ToString(dn.Cells[13].Value)); } catch { fsize = 0; }
                RetransmitDownload(fid, Convert.ToString(dn.Cells[1].Value), fsize);
            }
        }

        // ==================== 辅助 ====================

        private void WaitForCancellation(TransferTaskState st)
        {
            // 中断为协作式：让被取消的线程有时间运行其 finally（关闭 socket/文件流、落断点）。
            Thread.Sleep(200);
        }

        private void UploadOnUi(Action action)
        {
            try
            {
                if (InvokeRequired)
                {
                    if (!IsHandleCreated) return;
                    BeginInvoke(new MethodInvoker(delegate { if (!IsDisposed && !Disposing) action(); }));
                }
                else action();
            }
            catch { }
        }

        /// <summary>按名称在上传列表中查找行（用于传输中心操作映射）。</summary>
        private DataGridViewRow FindUploadRowByName(string name)
        {
            foreach (DataGridViewRow row in file_upload_list_dataGridView.Rows)
            {
                if (row.IsNewRow) continue;
                string n = Convert.ToString(row.Cells[1].Value);
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return row;
            }
            return null;
        }

        /// <summary>按名称在下载列表中查找行。</summary>
        private DataGridViewRow FindDownloadRowByName(string name)
        {
            foreach (DataGridViewRow row in file_download_list_dataGridView.Rows)
            {
                if (row.IsNewRow) continue;
                string n = Convert.ToString(row.Cells[1].Value);
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return row;
            }
            return null;
        }
    }
}
