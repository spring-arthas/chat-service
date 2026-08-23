using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 文件/目录数据传输对象，与 net-server 的 FileDto 严格对应。
    ///
    /// 服务端字段（fastjson 序列化）：
    /// id, parentId, childFileList, fileName, filePath, fileSize, fileType,
    /// isFile, isExist, hasChild, userName, userId, repeatCreate, fileCount, parentDirName,
    /// 以及继承自 BaseDTO 的 gmtCreated/gmtModified/delTime/del
    /// </summary>
    public class NetFileDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>父目录 ID（服务端字段名为 parentId，非 pId）。</summary>
        [JsonProperty("parentId")]
        public long ParentId { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [JsonProperty("fileSize")]
        public long? FileSize { get; set; }

        [JsonProperty("fileType")]
        public string FileType { get; set; }

        [JsonProperty("isFile")]
        public string IsFile { get; set; }

        [JsonProperty("isExist")]
        public string IsExist { get; set; }

        [JsonProperty("hasChild")]
        public string HasChild { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("userId")]
        public int? UserId { get; set; }

        [JsonProperty("repeatCreate")]
        public string RepeatCreate { get; set; }

        [JsonProperty("fileCount")]
        public long? FileCount { get; set; }

        [JsonProperty("parentDirName")]
        public string ParentDirName { get; set; }

        [JsonProperty("gmtCreated")]
        public long? GmtCreated { get; set; }

        [JsonProperty("gmtModified")]
        public long? GmtModified { get; set; }

        [JsonProperty("del")]
        public string Del { get; set; }

        [JsonProperty("delTime")]
        public long? DelTime { get; set; }

        [JsonProperty("childFileList")]
        public List<NetFileDto> ChildFileList { get; set; }

        [JsonProperty("md5")]
        public string Md5 { get; set; }

        public bool IsFileBoolean
        {
            get { return !string.IsNullOrEmpty(IsFile) && IsFile.ToUpper() == "Y"; }
        }

        public bool IsExistBoolean
        {
            get { return string.IsNullOrEmpty(IsExist) || IsExist.ToUpper() == "Y"; }
        }

        public bool HasChildBoolean
        {
            get { return !string.IsNullOrEmpty(HasChild) && HasChild.ToUpper() == "Y"; }
        }

        public bool IsDeleted
        {
            get { return !string.IsNullOrEmpty(Del) && Del.ToUpper() == "Y"; }
        }
    }

    /// <summary>
    /// 文件分页结果，与服务端 net-server 的 FilePageDto 严格对应。
    /// 字段: currentPage/pageSize/totalCount/totalPage/recordList
    /// </summary>
    public class FilePageResult
    {
        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalCount")]
        public long TotalCount { get; set; }

        [JsonProperty("totalPage")]
        public long TotalPage { get; set; }

        [JsonProperty("recordList")]
        public List<NetFileDto> RecordList { get; set; }

        public FilePageResult()
        {
            RecordList = new List<NetFileDto>();
        }
    }

    /// <summary>
    /// 通用分页结果（用于 history 等其他接口），服务端 PageResult&lt;T&gt; 字段为 modelList。
    /// 文件列表请使用 FilePageResult（recordList）。
    /// </summary>
    public class PageResult<T>
    {
        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalCount")]
        public long TotalCount { get; set; }

        [JsonProperty("totalPage")]
        public long TotalPage { get; set; }

        [JsonProperty("modelList")]
        public List<T> ModelList { get; set; }

        public PageResult()
        {
            ModelList = new List<T>();
        }
    }
}
