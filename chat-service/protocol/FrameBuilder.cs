using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 帧构建器，提供 JSON 负载 -> Frame 的便捷构造方法。
    /// 与 chat-storage 的 FrameBuilder 对应。
    /// </summary>
    public static class FrameBuilder
    {
        /// <summary>使用对象构建帧（JSON 序列化）。</summary>
        public static Frame Build(FrameTypeEnum type, object payload, byte flags = 0)
        {
            string json = JsonConvert.SerializeObject(payload);
            return new Frame(type, Encoding.UTF8.GetBytes(json), flags);
        }

        /// <summary>使用字典构建帧。</summary>
        public static Frame Build(FrameTypeEnum type, Dictionary<string, object> dictionary, byte flags = 0)
        {
            string json = JsonConvert.SerializeObject(dictionary);
            return new Frame(type, Encoding.UTF8.GetBytes(json), flags);
        }

        /// <summary>使用原始 JSON 数据构建帧。</summary>
        public static Frame Build(FrameTypeEnum type, byte[] jsonData, byte flags = 0)
        {
            return new Frame(type, jsonData, flags);
        }

        /// <summary>构建空帧（无数据）。</summary>
        public static Frame BuildEmpty(FrameTypeEnum type, byte flags = 0)
        {
            return new Frame(type, new byte[0], flags);
        }

        /// <summary>构建用户登录请求帧。</summary>
        public static Frame BuildLoginRequest(string userName, string password)
        {
            var request = new Dictionary<string, object>
            {
                { "userName", userName },
                { "password", password }
            };
            return Build(FrameTypeEnum.UserLoginReq, request);
        }

        /// <summary>构建用户注册请求帧。</summary>
        public static Frame BuildRegisterRequest(string userName, string password, string mail)
        {
            var request = new Dictionary<string, object>
            {
                { "userName", userName },
                { "password", password },
                { "mail", mail }
            };
            return Build(FrameTypeEnum.UserRegisterReq, request);
        }
    }
}
