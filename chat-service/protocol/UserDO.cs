using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 用户数据对象，与 chat-storage 的 UserDO 对应。
    /// 服务端字段: userId/userName/nickName/avatar/mail/phone/createTime/updateTime/status/transferToken
    /// </summary>
    public class UserDO
    {
        [JsonProperty("userId")]
        public long Id { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("nickName")]
        public string NickName { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("mail")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("createTime")]
        public long? CreateTime { get; set; }

        [JsonProperty("updateTime")]
        public long? UpdateTime { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }

        /// <summary>登录后由服务端签发，用于文件上传、下载和图片流请求认证。</summary>
        [JsonProperty("transferToken")]
        public string TransferToken { get; set; }

        /// <summary>
        /// 兼容服务端可能返回 "id" 而非 "userId" 的情况。
        /// </summary>
        [JsonProperty("id")]
        private long? idFallback
        {
            set
            {
                if (Id == 0 && value.HasValue) Id = value.Value;
            }
        }
    }
}
