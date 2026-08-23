using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace chat_service.protocol
{
    /// <summary>
    /// 帧解析器，负责从字节流中抽取完整帧。与 chat-storage 的 FrameParser 对应。
    /// </summary>
    public static class FrameParser
    {
        /// <summary>
        /// 尝试从缓冲区抽取一个完整帧。
        /// </summary>
        /// <param name="buffer">累积的数据缓冲区。</param>
        /// <returns>若存在完整帧，返回 (frame, consumedBytes)，否则返回 null。</returns>
        public static Tuple<Frame, int> ExtractFrame(byte[] buffer, int startOffset, int availableLength)
        {
            if (availableLength < Frame.HEADER_LENGTH) return null;

            // 校验魔数
            if (buffer[startOffset] != Frame.MAGIC[0] || buffer[startOffset + 1] != Frame.MAGIC[1])
            {
                // 魔数不匹配，说明字节流错位，丢弃一个字节以便重新对齐（防御性处理）
                return new Tuple<Frame, int>(null, 1);
            }

            // 读取长度字段（大端）
            uint length = ((uint)buffer[startOffset + 4] << 24)
                        | ((uint)buffer[startOffset + 5] << 16)
                        | ((uint)buffer[startOffset + 6] << 8)
                        | ((uint)buffer[startOffset + 7]);

            int totalLength = Frame.HEADER_LENGTH + (int)length;
            if (availableLength < totalLength) return null; // 数据不完整，等待更多数据

            Frame frame = Frame.FromBytes(buffer, startOffset, totalLength);
            return new Tuple<Frame, int>(frame, totalLength);
        }
    }
}
