using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.protocol
{
    /// <summary>
    /// 文件 MD5 计算工具，使用分块流式计算，避免一次性读取大文件导致内存飙升。
    /// 对应 chat-storage 中 FileTransferService 的 computeContentMD5。
    /// </summary>
    public static class Md5Helper
    {
        private const int ChunkSize = 4 * 1024 * 1024; // 4MB

        /// <summary>计算文件内容 MD5（返回小写十六进制字符串）。</summary>
        public static string ComputeContentMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[ChunkSize];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    md5.TransformBlock(buffer, 0, read, null, 0);
                }
                md5.TransformFinalBlock(new byte[0], 0, 0);
                byte[] hash = md5.Hash;
                StringBuilder sb = new StringBuilder(32);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
