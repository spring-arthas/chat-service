using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.protocol
{
    /// <summary>
    /// 自定义协议帧结构，与 chat-storage (macOS) 客户端保持一致的帧格式。
    ///
    /// 帧格式（大端序）：
    /// +--------+--------+--------+--------+--------+
    /// | Magic  | Type   | Flags  | Length | Data   |
    /// | 2字节  | 1字节  | 1字节  | 4字节  | N字节  |
    /// +--------+--------+--------+--------+--------+
    /// </summary>
    public class Frame
    {
        /// <summary>魔数：0xFACE</summary>
        public static readonly byte[] MAGIC = new byte[] { 0xFA, 0xCE };

        /// <summary>帧头长度：8字节</summary>
        public const int HEADER_LENGTH = 8;

        /// <summary>bit0: 标识最后一帧</summary>
        public const byte FLAG_LAST_FRAME = 0x01;

        /// <summary>bit1: 请求接收端处理完该帧后返回 ACK</summary>
        public const byte FLAG_NEED_ACK = 0x02;

        /// <summary>bit2: DATA_FRAME 的 Data 前 8 字节为文件偏移量</summary>
        public const byte FLAG_HAS_OFFSET = 0x04;

        private readonly FrameTypeEnum type;
        private readonly byte flags;
        private readonly uint length;
        private readonly byte[] data;

        public FrameTypeEnum Type { get { return type; } }
        public byte Flags { get { return flags; } }
        public uint Length { get { return length; } }
        public byte[] Data { get { return data; } }

        public Frame(FrameTypeEnum type, byte[] data, byte flags = 0)
        {
            this.type = type;
            this.data = data ?? new byte[0];
            this.length = (uint)this.data.Length;
            this.flags = flags;
        }

        /// <summary>
        /// 将帧对象序列化为字节数组。
        /// </summary>
        public byte[] ToBytes()
        {
            byte[] bytes = new byte[HEADER_LENGTH + data.Length];
            // 1. 魔数 (2字节)
            bytes[0] = MAGIC[0];
            bytes[1] = MAGIC[1];
            // 2. 类型 (1字节)
            bytes[2] = (byte)type;
            // 3. 标志位 (1字节)
            bytes[3] = flags;
            // 4. 长度 (4字节，大端序)
            byte[] lengthBytes = ToBigEndian(length);
            Buffer.BlockCopy(lengthBytes, 0, bytes, 4, 4);
            // 5. 数据 (N字节)
            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, bytes, HEADER_LENGTH, data.Length);
            }
            return bytes;
        }

        /// <summary>
        /// 从字节数组构造帧对象（要求有完整帧头+数据）。
        /// </summary>
        public static Frame FromBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (count < HEADER_LENGTH) throw new FrameParseException("数据长度不足以构成帧头");
            if (offset < 0 || offset + count > bytes.Length) throw new ArgumentOutOfRangeException();

            // 验证魔数
            if (bytes[offset] != MAGIC[0] || bytes[offset + 1] != MAGIC[1])
            {
                throw new FrameParseException("魔数验证失败");
            }

            byte typeRaw = bytes[offset + 2];
            FrameTypeEnum? type = FrameTypeEnumExtensions.FromRawValue(typeRaw);
            if (type == null)
            {
                throw new FrameParseException(string.Format("未知的帧类型 0x{0:X2}", typeRaw));
            }

            byte flags = bytes[offset + 3];
            uint length = ToUInt32BigEndian(bytes, offset + 4);

            int expectedTotal = HEADER_LENGTH + (int)length;
            if (count < expectedTotal)
            {
                throw new FrameParseException(string.Format("数据长度不足: 期望 {0}, 实际 {1}", expectedTotal, count));
            }

            byte[] data = new byte[length];
            if (length > 0)
            {
                Buffer.BlockCopy(bytes, offset + HEADER_LENGTH, data, 0, (int)length);
            }

            return new Frame(type.Value, data, flags);
        }

        /// <summary>
        /// 获取帧体中数据（Data）的 UTF-8 字符串表示。
        /// </summary>
        public string GetDataAsString()
        {
            if (data == null || data.Length == 0) return "";
            return Encoding.UTF8.GetString(data);
        }

        public string Describe()
        {
            return string.Format("Frame {{ Type: {0} (0x{1:X2}), Flags: 0x{2:X2}, Length: {3} bytes }}",
                type.Describe(), (byte)type, flags, length);
        }

        // ---- 工具方法 ----

        private static byte[] ToBigEndian(uint value)
        {
            byte[] b = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }

        private static uint ToUInt32BigEndian(byte[] bytes, int offset)
        {
            // 手动按大端组装，避免字节序歧义
            return ((uint)bytes[offset] << 24)
                 | ((uint)bytes[offset + 1] << 16)
                 | ((uint)bytes[offset + 2] << 8)
                 | ((uint)bytes[offset + 3]);
        }
    }

    /// <summary>帧解析异常。</summary>
    public class FrameParseException : Exception
    {
        public FrameParseException(string message) : base(message) { }
    }
}
