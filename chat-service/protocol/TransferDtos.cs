using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 断点检查请求 (0x05) 上传时的请求体。与服务端 FileUploadRequest 严格对应。
    /// </summary>
    public class ResumeCheckRequest
    {
        [JsonProperty("md5")]
        public string Md5 { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        [JsonProperty("fileType")]
        public string FileType { get; set; }

        [JsonProperty("dirId")]
        public long DirId { get; set; }

        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("transferToken")]
        public string TransferToken { get; set; }
    }

    /// <summary>
    /// 断点应答 (0x06) 响应体。
    /// status: "resume"=断点续传, "new"=全新上传, "complete"=已完成
    /// </summary>
    public class ResumeAckResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("uploadedSize")]
        public long? UploadedSize { get; set; }

        [JsonProperty("fileId")]
        public long? FileId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// 上传元数据请求 (0x01 metaFrame)。
    /// </summary>
    public class FileMetaRequest
    {
        [JsonProperty("md5")]
        public string Md5 { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        [JsonProperty("fileType")]
        public string FileType { get; set; }

        [JsonProperty("dirId")]
        public long DirId { get; set; }

        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("transferToken")]
        public string TransferToken { get; set; }
    }

    /// <summary>
    /// 标准 ACK 响应 (0x04)。status 可为 ready/progress/success。
    /// </summary>
    public class StandardAckResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("uploadedSize")]
        public long? UploadedSize { get; set; }

        [JsonProperty("fileId")]
        public long? FileId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// 上传结束请求 (0x03)。
    /// </summary>
    public class EndUploadRequest
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }
    }

    /// <summary>
    /// 下载请求 (metaFrame 0x01) 请求体。
    /// </summary>
    public class DownloadRequest
    {
        [JsonProperty("fileId")]
        public long FileId { get; set; }

        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("startOffset")]
        public long StartOffset { get; set; }

        [JsonProperty("userId")]
        public long UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("transferToken")]
        public string TransferToken { get; set; }
    }
}
