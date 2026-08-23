using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 认证服务，处理登录/注册，对应 chat-storage 的 AuthenticationService。
    /// </summary>
    public class AuthenticationService
    {
        private static readonly AuthenticationService _instance = new AuthenticationService();
        public static AuthenticationService Shared { get { return _instance; } }

        private SocketManager socketManager;

        private AuthenticationService()
        {
            socketManager = SocketManager.Shared;
        }

        /// <summary>用户登录。</summary>
        public UserDO Login(string userName, string password)
        {
            Frame requestFrame = FrameBuilder.BuildLoginRequest(userName, password);
            Frame responseFrame = socketManager.SendFrameAndWait(requestFrame, FrameTypeEnum.UserResponse, 10000);

            var wrapper = SocketManager.ParseResponseWrapper(responseFrame);
            if (wrapper.EffectiveCode != 200 || wrapper.Data == null)
            {
                throw new AuthException("登录失败: " + (wrapper.Message ?? "未知错误"));
            }

            string dataJson = JsonConvert.SerializeObject(wrapper.Data);
            UserDO user = JsonConvert.DeserializeObject<UserDO>(dataJson);
            if (user == null)
            {
                throw new AuthException("登录失败：响应数据无效");
            }

            // 更新全局登录状态
            socketManager.CurrentUser = user;
            socketManager.CurrentUserId = user.Id;
            return user;
        }

        /// <summary>用户注册。</summary>
        public UserDO Register(string userName, string password, string mail)
        {
            Frame requestFrame = FrameBuilder.BuildRegisterRequest(userName, password, mail);
            Frame responseFrame = socketManager.SendFrameAndWait(requestFrame, FrameTypeEnum.UserResponse, 10000);

            var wrapper = SocketManager.ParseResponseWrapper(responseFrame);
            if (wrapper.EffectiveCode != 200 || wrapper.Data == null)
            {
                throw new AuthException("注册失败: " + (wrapper.Message ?? "未知错误"));
            }

            string dataJson = JsonConvert.SerializeObject(wrapper.Data);
            UserDO user = JsonConvert.DeserializeObject<UserDO>(dataJson);
            if (user == null)
            {
                throw new AuthException("注册失败：响应数据无效");
            }

            socketManager.CurrentUser = user;
            socketManager.CurrentUserId = user.Id;
            return user;
        }

        /// <summary>退出登录。</summary>
        public void Logout()
        {
            socketManager.CurrentUser = null;
            socketManager.CurrentUserId = 0;
        }
    }

    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
    }
}
