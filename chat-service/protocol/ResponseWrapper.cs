using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.protocol
{
    /// <summary>
    /// 通用响应包装器，兼容服务端两种响应格式：
    /// 1. { "success": true/false, "message": "...", "data": {...} }
    /// 2. { "code": 200, "message": "...", "data": {...} }
    /// 与 chat-storage 的 ResponseWrapper&lt;T&gt; 对应，但为 C# 泛型实现。
    /// </summary>
    public class ResponseWrapper
    {
        public bool? Success { get; set; }
        public int? Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        /// <summary>
        /// 实际响应码：有 code 用 code，否则 success==true -> 200，否则 400。
        /// </summary>
        public int EffectiveCode
        {
            get
            {
                if (Code.HasValue) return Code.Value;
                return (Success == true) ? 200 : 400;
            }
        }

        public bool IsSuccess
        {
            get { return EffectiveCode == 200; }
        }
    }
}
