using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.protocol
{
    /// <summary>
    /// 帧类型枚举，与 chat-storage (macOS) 客户端保持一致的协议帧类型。
    /// 每个枚举值对应的原始字节值即帧头中的 Type 字段。
    /// </summary>
    public enum FrameTypeEnum : byte
    {
        // ========== 基础帧 (0x01-0x0F) ==========
        /// 元数据帧：包含文件名、大小、类型等信息
        MetaFrame = 0x01,
        /// 数据帧：包含文件字节流数据
        DataFrame = 0x02,
        /// 结束帧：标识文件传输结束
        EndFrame = 0x03,
        /// 确认帧：服务端发送给客户端的确认响应
        AckFrame = 0x04,
        /// 断点检查帧：客户端请求检查文件上传断点
        ResumeCheck = 0x05,
        /// 断点应答帧：服务端返回已上传大小和续传信息
        ResumeAck = 0x06,

        // ========== 目录操作帧 (0x10-0x1F) ==========
        /// 目录新建请求
        DirCreateReq = 0x10,
        /// 目录删除请求
        DirDeleteReq = 0x11,
        /// 目录更新请求
        DirUpdateReq = 0x12,
        /// 目录移动请求
        DirMoveReq = 0x13,
        /// 目录操作响应
        DirResponse = 0x14,
        /// 目录列表请求
        DirListReq = 0x15,

        // ========== 目录文件上传帧 (0x20-0x2F) ==========
        /// 目录文件元数据帧
        DirFileMeta = 0x20,
        /// 目录文件数据帧
        DirFileData = 0x21,
        /// 目录文件结束帧
        DirFileEnd = 0x22,
        /// 目录文件确认帧
        DirFileAck = 0x23,

        // ========== 用户认证帧 (0x30-0x3F) ==========
        /// 用户注册请求
        UserRegisterReq = 0x30,
        /// 用户登录请求
        UserLoginReq = 0x31,
        /// 用户修改密码请求
        UserChangePwdReq = 0x32,
        /// 用户退出登录请求
        UserLogoutReq = 0x33,
        /// 用户操作响应
        UserResponse = 0x34,

        /// 获取好友列表请求 (0x35)
        FriendListReq = 0x35,
        /// 搜索用户请求 (0x36)
        SearchUserReq = 0x36,
        /// 添加好友请求 (0x37)
        AddFriendReq = 0x37,
        /// 获取未处理好友申请请求 (0x38)
        PendingRequestsReq = 0x38,
        /// 处理好友申请请求 (0x39)
        HandleFriendReq = 0x39,

        // ========== 文件操作帧 (0x40-0x4F) ==========
        /// 文件列表分页请求
        FileListReq = 0x40,
        /// 文件删除请求 (0x41)
        FileDeleteReq = 0x41,
        /// 文件详情请求 (0x42)
        FileDetailReq = 0x42,
        /// 文件操作响应
        FileResponse = 0x43,
        /// 文件重命名请求 (0x44)
        FileRenameReq = 0x44,

        // ========== 聊天操作帧 (0x50-0x5F) ==========
        /// 发送聊天请求
        ChatSendReq = 0x50,
        /// 接收聊天消息推送
        ChatPushReq = 0x51,
        /// 接收聊天消息回执
        ChatReceiptReq = 0x52,
        /// 请求聊天历史记录
        ChatHistoryReq = 0x53,
        /// 响应聊天历史记录
        ChatHistoryRes = 0x54,
        /// 清除未读消息红点请求
        ChatClearUnreadReq = 0x55,
        /// 清除未读消息红点响应
        ChatClearUnreadRes = 0x56,
        /// 修改好友备注请求 (0x57)
        FriendUpdateAliasReq = 0x57,
        /// 修改好友备注响应 (0x58)
        FriendUpdateAliasResp = 0x58,
        /// 聊天消息动作请求：本地删除/撤回等 (0x59)
        ChatMessageActionReq = 0x59,
        /// 聊天消息动作响应 (0x5A)
        ChatMessageActionResp = 0x5A,
        /// 聊天消息动作推送 (0x5B)
        ChatMessageActionPush = 0x5B
    }

    public static class FrameTypeEnumExtensions
    {
        /// <summary>
        /// 帧类型描述（用于日志）
        /// </summary>
        public static string Describe(this FrameTypeEnum type)
        {
            switch (type)
            {
                case FrameTypeEnum.MetaFrame: return "元数据帧";
                case FrameTypeEnum.DataFrame: return "数据帧";
                case FrameTypeEnum.EndFrame: return "结束帧";
                case FrameTypeEnum.AckFrame: return "确认帧";
                case FrameTypeEnum.ResumeCheck: return "断点检查帧";
                case FrameTypeEnum.ResumeAck: return "断点应答帧";
                case FrameTypeEnum.DirCreateReq: return "目录新建请求";
                case FrameTypeEnum.DirDeleteReq: return "目录删除请求";
                case FrameTypeEnum.DirUpdateReq: return "目录更新请求";
                case FrameTypeEnum.DirMoveReq: return "目录移动请求";
                case FrameTypeEnum.DirResponse: return "目录操作响应";
                case FrameTypeEnum.DirListReq: return "目录列表请求";
                case FrameTypeEnum.DirFileMeta: return "目录文件元数据";
                case FrameTypeEnum.DirFileData: return "目录文件数据";
                case FrameTypeEnum.DirFileEnd: return "目录文件结束";
                case FrameTypeEnum.DirFileAck: return "目录文件确认";
                case FrameTypeEnum.UserRegisterReq: return "用户注册请求";
                case FrameTypeEnum.UserLoginReq: return "用户登录请求";
                case FrameTypeEnum.UserChangePwdReq: return "用户修改密码请求";
                case FrameTypeEnum.UserLogoutReq: return "用户退出登录请求";
                case FrameTypeEnum.UserResponse: return "用户操作响应";
                case FrameTypeEnum.FriendListReq: return "获取好友列表请求";
                case FrameTypeEnum.SearchUserReq: return "搜索用户请求";
                case FrameTypeEnum.AddFriendReq: return "添加好友请求";
                case FrameTypeEnum.PendingRequestsReq: return "获取好友申请列表";
                case FrameTypeEnum.HandleFriendReq: return "处理好友申请";
                case FrameTypeEnum.FileListReq: return "文件列表请求";
                case FrameTypeEnum.FileDetailReq: return "文件详情请求";
                case FrameTypeEnum.FileDeleteReq: return "文件删除请求";
                case FrameTypeEnum.FileResponse: return "文件操作响应";
                case FrameTypeEnum.FileRenameReq: return "文件重命名请求";
                case FrameTypeEnum.ChatSendReq: return "发送聊天请求";
                case FrameTypeEnum.ChatPushReq: return "接收聊天消息推送";
                case FrameTypeEnum.ChatReceiptReq: return "接收聊天消息回执";
                case FrameTypeEnum.ChatHistoryReq: return "请求聊天历史记录";
                case FrameTypeEnum.ChatHistoryRes: return "响应聊天历史记录";
                case FrameTypeEnum.ChatClearUnreadReq: return "清除未读消息请求";
                case FrameTypeEnum.ChatClearUnreadRes: return "清除未读消息响应";
                case FrameTypeEnum.FriendUpdateAliasReq: return "修改好友备注请求";
                case FrameTypeEnum.FriendUpdateAliasResp: return "修改好友备注响应";
                case FrameTypeEnum.ChatMessageActionReq: return "聊天消息动作请求";
                case FrameTypeEnum.ChatMessageActionResp: return "聊天消息动作响应";
                case FrameTypeEnum.ChatMessageActionPush: return "聊天消息动作推送";
                default: return "未知帧类型";
            }
        }

        /// <summary>
        /// 从原始字节值构造枚举，未知值时返回 null。
        /// </summary>
        public static FrameTypeEnum? FromRawValue(byte rawValue)
        {
            if (Enum.IsDefined(typeof(FrameTypeEnum), rawValue))
            {
                return (FrameTypeEnum)rawValue;
            }
            return null;
        }
    }
}
