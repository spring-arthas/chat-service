using chat_service.util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace chat_service.protocol
{
    /// <summary>
    /// 调用 net-server 媒体 HTTP 服务，获取短期播放地址并通知进度跳转。
    /// 实际视频数据由系统播放器直接通过带签名的 URL 拉取。
    /// </summary>
    public sealed class MediaPlaybackService
    {
        private readonly string baseAddress;

        public MediaPlaybackService(string baseAddress)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                throw new ArgumentException("媒体服务地址不能为空", "baseAddress");
            }
            this.baseAddress = baseAddress.TrimEnd('/');
        }

        /// <summary>为指定视频创建带会话标识的短期播放地址。</summary>
        public MediaPlayUrlInfo RequestPlayUrl(long fileId, string transferToken, string sessionId)
        {
            ValidateIdentity(fileId, transferToken, sessionId);
            string requestUrl = baseAddress + "/media/play-url/" + fileId
                + "?sessionId=" + Uri.EscapeDataString(sessionId);
            HttpWebRequest request = CreateRequest(requestUrl, "GET", transferToken);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string body = ReadResponseBody(response);
                    MediaApiResponse envelope = JsonConvert.DeserializeObject<MediaApiResponse>(body);
                    if (envelope == null || envelope.Code != 200 || envelope.Data == null
                        || !envelope.Data.Playable || string.IsNullOrWhiteSpace(envelope.Data.PlayUrl))
                    {
                        string message = envelope == null ? "播放服务返回了无效响应" : envelope.Message;
                        throw new MediaPlaybackException(string.IsNullOrWhiteSpace(message)
                            ? "该视频暂不支持在线播放" : message);
                    }
                    return envelope.Data;
                }
            }
            catch (WebException ex)
            {
                throw BuildRequestException(ex, "获取播放地址失败");
            }
        }

        /// <summary>通知服务端播放器即将跳转，便于服务端区分主动取消与网络中断。</summary>
        public void NotifySeek(long fileId, string transferToken, string sessionId, double targetSeconds)
        {
            ValidateIdentity(fileId, transferToken, sessionId);
            string requestUrl = baseAddress + "/media/seek/" + fileId
                + "?sessionId=" + Uri.EscapeDataString(sessionId)
                + "&targetSeconds=" + Uri.EscapeDataString(targetSeconds.ToString("0.###",
                    System.Globalization.CultureInfo.InvariantCulture));
            HttpWebRequest request = CreateRequest(requestUrl, "POST", transferToken);
            request.ContentLength = 0;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.NoContent
                        && response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new MediaPlaybackException("服务端未接受播放进度跳转");
                    }
                }
            }
            catch (WebException ex)
            {
                throw BuildRequestException(ex, "播放进度跳转通知失败");
            }
        }

        /// <summary>解析媒体服务地址；未单独配置时复用主控服务主机和默认媒体端口。</summary>
        public static string ResolveBaseAddress()
        {
            string configured = string.Empty;
            try
            {
                configured = XmlConfigUtils.GetValue("remoteMediaServiceAddress");
            }
            catch (Exception)
            {
                configured = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? configured.TrimEnd('/') : "http://" + configured.TrimEnd('/');
            }

            string controlAddress = NetServiceContext.remoteServiceAddress ?? string.Empty;
            string host = ExtractHost(controlAddress);
            if (string.IsNullOrWhiteSpace(host)) host = "127.0.0.1";
            if (host.IndexOf(':') >= 0 && !host.StartsWith("[", StringComparison.Ordinal))
            {
                host = "[" + host + "]";
            }
            return "http://" + host + ":10188";
        }

        /// <summary>判断扩展名是否与 net-server 当前可播放视频白名单一致。</summary>
        public static bool IsPlayableVideo(string fileName)
        {
            string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            return extension == ".mp4" || extension == ".m4v" || extension == ".mov";
        }

        private static HttpWebRequest CreateRequest(string url, string method, string transferToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "application/json";
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;
            request.KeepAlive = false;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + transferToken;
            return request;
        }

        private static void ValidateIdentity(long fileId, string transferToken, string sessionId)
        {
            if (fileId <= 0) throw new MediaPlaybackException("文件标识无效");
            if (string.IsNullOrWhiteSpace(transferToken))
            {
                throw new MediaPlaybackException("登录凭据缺失，请重新登录");
            }
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new MediaPlaybackException("播放会话标识缺失");
            }
        }

        private static string ExtractHost(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;
            Uri endpoint;
            if (Uri.TryCreate("tcp://" + address.Trim(), UriKind.Absolute, out endpoint))
            {
                return endpoint.Host;
            }
            int separator = address.LastIndexOf(':');
            return separator > 0 ? address.Substring(0, separator) : address;
        }

        private static string ReadResponseBody(HttpWebResponse response)
        {
            Stream stream = response.GetResponseStream();
            if (stream == null) return string.Empty;
            using (stream)
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static MediaPlaybackException BuildRequestException(WebException exception, string fallbackMessage)
        {
            HttpWebResponse response = exception.Response as HttpWebResponse;
            if (response != null)
            {
                using (response)
                {
                    try
                    {
                        string body = ReadResponseBody(response);
                        MediaApiResponse envelope = JsonConvert.DeserializeObject<MediaApiResponse>(body);
                        if (envelope != null && !string.IsNullOrWhiteSpace(envelope.Message))
                        {
                            return new MediaPlaybackException(envelope.Message, exception);
                        }
                    }
                    catch (Exception)
                    {
                        // 响应体不是 JSON 时使用统一的用户提示。
                    }
                }
            }
            return new MediaPlaybackException(fallbackMessage + ": " + exception.Message, exception);
        }

        private sealed class MediaApiResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public MediaPlayUrlInfo Data { get; set; }
        }
    }

    public sealed class MediaPlayUrlInfo
    {
        [JsonProperty("playUrl")]
        public string PlayUrl { get; set; }

        [JsonProperty("fileId")]
        public long FileId { get; set; }

        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("expiresIn")]
        public long ExpiresIn { get; set; }

        [JsonProperty("playable")]
        public bool Playable { get; set; }
    }

    public class MediaPlaybackException : Exception
    {
        public MediaPlaybackException(string message) : base(message) { }

        public MediaPlaybackException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
