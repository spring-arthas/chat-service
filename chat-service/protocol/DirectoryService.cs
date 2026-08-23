using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 目录服务，处理目录树与文件列表。对应 chat-storage 的 DirectoryService。
    /// 帧类型：
    ///  - 目录列表 0x15 -> 响应 0x14
    ///  - 目录新建 0x10 -> 响应 0x14
    ///  - 目录删除 0x11 -> 响应 0x14
    ///  - 目录重命名 0x12 -> 响应 0x14
    ///  - 文件列表 0x40 -> 响应 0x43
    ///  - 文件详情 0x42 -> 响应 0x43
    ///  - 文件删除 0x41 -> 响应 0x43
    ///  - 文件重命名 0x44 -> 响应 0x43
    /// </summary>
    public class DirectoryService
    {
        private static readonly DirectoryService _instance = new DirectoryService();
        public static DirectoryService Shared { get { return _instance; } }

        private SocketManager socketManager;

        private DirectoryService()
        {
            socketManager = SocketManager.Shared;
        }

        /// <summary>加载根目录树。</summary>
        public List<NetFileDto> LoadDirectoryTree()
        {
            Frame request = FrameBuilder.BuildEmpty(FrameTypeEnum.DirListReq);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.DirResponse, 15000);
            return ParseDirectoryResponse(response);
        }

        /// <summary>按目录 ID 懒加载下一层子目录。</summary>
        public List<NetFileDto> LoadDirectoryChildren(long dirId)
        {
            var dict = new Dictionary<string, object> { { "dirId", dirId } };
            Frame request = FrameBuilder.Build(FrameTypeEnum.DirListReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.DirResponse, 15000);
            return ParseDirectoryResponse(response);
        }

        /// <summary>创建目录。</summary>
        public void CreateDirectory(long pId, string name)
        {
            var dict = new Dictionary<string, object>
            {
                { "pId", pId },
                { "dirName", name }
            };
            Frame request = FrameBuilder.Build(FrameTypeEnum.DirCreateReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.DirResponse, 10000);
            EnsureSuccess(response, "目录创建");
        }

        /// <summary>重命名目录。</summary>
        public void RenameDirectory(long id, string name)
        {
            var dict = new Dictionary<string, object>
            {
                { "id", id },
                { "dirName", name }
            };
            Frame request = FrameBuilder.Build(FrameTypeEnum.DirUpdateReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.DirResponse, 10000);
            EnsureSuccess(response, "目录重命名");
        }

        /// <summary>删除目录。</summary>
        public void DeleteDirectory(long id)
        {
            var dict = new Dictionary<string, object> { { "id", id } };
            Frame request = FrameBuilder.Build(FrameTypeEnum.DirDeleteReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.DirResponse, 10000);
            EnsureSuccess(response, "目录删除");
        }

        /// <summary>分页获取文件列表。响应为 FilePageDto（字段 recordList）。</summary>
        public FilePageResult FetchFileList(long dirId, string fileName = "", int pageNum = 1, int pageSize = 10)
        {
            var dict = new Dictionary<string, object>
            {
                { "dirId", dirId },
                { "fileName", fileName },
                { "pageNum", pageNum },
                { "pageSize", pageSize }
            };
            Frame request = FrameBuilder.Build(FrameTypeEnum.FileListReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.FileResponse, 15000);

            var wrapper = SocketManager.ParseResponseWrapper(response);
            if (!wrapper.IsSuccess)
            {
                throw new DirectoryException(string.Format("文件列表请求失败: {0}", wrapper.Message ?? "未知错误"));
            }
            if (wrapper.Data == null)
            {
                return new FilePageResult { CurrentPage = pageNum, PageSize = pageSize };
            }

            string dataJson = JsonConvert.SerializeObject(wrapper.Data);
            return JsonConvert.DeserializeObject<FilePageResult>(dataJson)
                   ?? new FilePageResult { CurrentPage = pageNum, PageSize = pageSize };
        }

        /// <summary>获取文件详情。</summary>
        public NetFileDto FetchFileDetail(long fileId)
        {
            var dict = new Dictionary<string, object> { { "fileId", fileId } };
            Frame request = FrameBuilder.Build(FrameTypeEnum.FileDetailReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.FileResponse, 10000);

            var wrapper = SocketManager.ParseResponseWrapper(response);
            if (!wrapper.IsSuccess || wrapper.Data == null)
            {
                throw new DirectoryException("文件详情请求失败: " + (wrapper.Message ?? "未知错误"));
            }
            string dataJson = JsonConvert.SerializeObject(wrapper.Data);
            return JsonConvert.DeserializeObject<NetFileDto>(dataJson);
        }

        /// <summary>删除文件。</summary>
        public void DeleteFile(long fileId)
        {
            var dict = new Dictionary<string, object> { { "fileId", fileId } };
            Frame request = FrameBuilder.Build(FrameTypeEnum.FileDeleteReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.FileResponse, 10000);
            EnsureSuccess(response, "文件删除");
        }

        /// <summary>批量删除文件，同时由服务端清理物理文件和数据库记录。</summary>
        public void DeleteFiles(IEnumerable<long> fileIds)
        {
            List<long> ids = fileIds == null ? new List<long>() : fileIds.Distinct().ToList();
            if (ids.Count == 0) return;
            if (ids.Count > 200) throw new DirectoryException("单次最多删除200个文件");
            var dict = new Dictionary<string, object> { { "fileIds", ids } };
            Frame request = FrameBuilder.Build(FrameTypeEnum.FileDeleteReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.FileResponse, 30000);
            EnsureSuccess(response, "批量文件删除");
        }

        /// <summary>重命名文件。</summary>
        public void RenameFile(long fileId, string newFileName)
        {
            var dict = new Dictionary<string, object>
            {
                { "fileId", fileId },
                { "newFileName", newFileName }
            };
            Frame request = FrameBuilder.Build(FrameTypeEnum.FileRenameReq, dict);
            Frame response = socketManager.SendFrameAndWait(request, FrameTypeEnum.FileResponse, 10000);
            EnsureSuccess(response, "文件重命名");
        }

        // ============ 内部方法 ============

        /// <summary>解析目录响应帧为 FileDto 列表。</summary>
        private List<NetFileDto> ParseDirectoryResponse(Frame frame)
        {
            var wrapper = SocketManager.ParseResponseWrapper(frame);
            if (!wrapper.IsSuccess) return new List<NetFileDto>();
            if (wrapper.Data == null) return new List<NetFileDto>();

            string dataJson = JsonConvert.SerializeObject(wrapper.Data);

            // 尝试作为数组解析
            try
            {
                var list = JsonConvert.DeserializeObject<List<NetFileDto>>(dataJson);
                if (list != null) return list;
            }
            catch { }

            // 尝试作为单个对象解析
            try
            {
                var single = JsonConvert.DeserializeObject<NetFileDto>(dataJson);
                if (single != null) return new List<NetFileDto> { single };
            }
            catch { }

            return new List<NetFileDto>();
        }

        private void EnsureSuccess(Frame response, string operation)
        {
            var wrapper = SocketManager.ParseResponseWrapper(response);
            if (!wrapper.IsSuccess)
            {
                throw new DirectoryException(operation + "失败: " + (wrapper.Message ?? "未知错误"));
            }
        }
    }

    public class DirectoryException : Exception
    {
        public DirectoryException(string message) : base(message) { }
    }
}
