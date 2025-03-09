using MailKit.Net.Smtp;
using MailKit.Security;
using MFAWPF.Extensions;
using MimeKit;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MFAWPF.Helper;

public static class ExternalNotificationHelper
{
    #region 总调用入口

    public async static Task ExternalNotificationAsync(CancellationToken cancellationToken = default)
    {
        var enabledProviders = Instances.ExternalNotificationSettingsUserControlModel.EnabledExternalNotificationProviderList;

        foreach (var enabledProvider in enabledProviders)
        {
            switch (enabledProvider)
            {
                case Key.DingTalkKey:
                    await DingTalk.SendAsync(
                        Instances.ExternalNotificationSettingsUserControlModel.DingTalkToken,
                        Instances.ExternalNotificationSettingsUserControlModel.DingTalkSecret,
                        cancellationToken
                    );
                    break;
                case Key.EmailKey:
                    await Email.SendAsync(
                        Instances.ExternalNotificationSettingsUserControlModel.EmailAccount,
                        Instances.ExternalNotificationSettingsUserControlModel.EmailSecret,
                        cancellationToken
                    );
                    break;
                case Key.LarkKey:
                    await Lark.SendAsync(
                        Instances.ExternalNotificationSettingsUserControlModel.LarkId,
                        Instances.ExternalNotificationSettingsUserControlModel.LarkToken,
                        cancellationToken
                    );
                    break;
                case Key.WxPusherKey:
                    await WxPusher.SendAsync(
                        Instances.ExternalNotificationSettingsUserControlModel.WxPusherToken,
                        Instances.ExternalNotificationSettingsUserControlModel.WxPusherUid,
                        cancellationToken
                    );
                    break;
                case Key.TelegramKey:
                    await Telegram.SendAsync(
                        Instances.ExternalNotificationSettingsUserControlModel.TelegramBotToken,
                        Instances.ExternalNotificationSettingsUserControlModel.TelegramChatId,
                        cancellationToken
                    );
                    break;
                case Key.DiscordKey:
                    await Discord.SendAsync(
                        Instances.ExternalNotificationSettingsUserControlModel.DiscordUserId,
                        Instances.ExternalNotificationSettingsUserControlModel.DiscordBotToken,
                        cancellationToken
                    );
                    break;
                case Key.SmtpKey:
                    await Smtp.SendAsync(Instances.ExternalNotificationSettingsUserControlModel.SmtpServer, Instances.ExternalNotificationSettingsUserControlModel.SmtpPort, Instances.ExternalNotificationSettingsUserControlModel.SmtpUseSsl,
                        Instances.ExternalNotificationSettingsUserControlModel.SmtpRequireAuthentication, Instances.ExternalNotificationSettingsUserControlModel.SmtpFrom, Instances.ExternalNotificationSettingsUserControlModel.SmtpTo,
                        Instances.ExternalNotificationSettingsUserControlModel.SmtpUser, Instances.ExternalNotificationSettingsUserControlModel.SmtpPassword, cancellationToken);
                    break;
                case Key.QmsgKey:
                    await QMsg.SendAsync(Instances.ExternalNotificationSettingsUserControlModel.QmsgServer,
                        Instances.ExternalNotificationSettingsUserControlModel.QmsgKey, 
                        Instances.ExternalNotificationSettingsUserControlModel.QmsgUser,
                        Instances.ExternalNotificationSettingsUserControlModel.QmsgBot,cancellationToken);
                    break;
            }
        }
    }

    #endregion
    #region Keys

    public static class Key
    {
        public const string DingTalkKey = "DingTalk"; // 钉钉
        public const string EmailKey = "Email"; // 邮件
        public const string LarkKey = "Lark"; // 飞书
        public const string WxPusherKey = "WxPusher"; // 微信公众号
        public const string TelegramKey = "Telegram"; // 电报
        public const string DiscordKey = "Discord"; // Discord
        public const string QmsgKey = "Qmsg"; // QMsg酱
        public const string SmtpKey = "SMTP"; // SMTP协议

        public static readonly IReadOnlyList<string> AllKeys =
        [
            DingTalkKey,
            EmailKey,
            LarkKey,
            WxPusherKey,
            TelegramKey,
            DiscordKey,
            SmtpKey,
            QmsgKey,
        ];
    }

    #endregion
    #region 钉钉通知

    public static class DingTalk
    {
        public async static Task<bool> SendAsync(string accessToken, string secret, CancellationToken cancellationToken = default)
        {
            var timestamp = GetTimestamp();
            var sign = CalculateSignature(timestamp, secret);
            var message = new
            {
                msgtype = "text",
                text = new
                {
                    content = "TaskAllCompleted".ToLocalization()
                }
            };

            try
            {
                var apiUrl = string.IsNullOrWhiteSpace(secret) ? $"https://oapi.dingtalk.com/robot/send?access_token={accessToken}" : $"https://oapi.dingtalk.com/robot/send?access_token={accessToken}&timestamp={timestamp}&sign={sign}";
                using var client = new HttpClient();
                var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    LoggerService.LogInfo("钉钉消息发送成功");
                    return true;
                }

                LoggerService.LogError($"钉钉消息发送失败: {response.StatusCode} {await response.Content.ReadAsStringAsync(cancellationToken)}");
                return false;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("钉钉消息发送操作已取消");
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"钉钉消息发送错误: {ex.Message}");
                return false;
            }
        }

        private static string GetTimestamp()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
        }

        private static string CalculateSignature(string timestamp, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}\n{secret}"));
            return WebUtility.UrlEncode(Convert.ToBase64String(hash))
                .Replace("+", "%20")
                .Replace("/", "%2F")
                .Replace("=", "%3D");
        }
    }

    #endregion

    #region 邮箱通知

    public static class Email
    {
        public async static Task SendAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var smtpConfig = GetSmtpConfigByEmail(email);
                using var client = new SmtpClient();
                var mail = CreateEmailMessage(email);

                await client.ConnectAsync(
                    smtpConfig.Host,
                    smtpConfig.Port,
                    smtpConfig.UseSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto,
                    cancellationToken
                );

                await client.AuthenticateAsync(email, password, cancellationToken);
                await client.SendAsync(mail, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                LoggerService.LogInfo("邮件发送成功");
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("邮件发送操作已取消");
            }
            catch (AuthenticationException ex)
            {
                LoggerService.LogError($"邮件认证失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"邮件发送错误: {ex.Message}");
            }
        }

        private static MimeMessage CreateEmailMessage(string email)
        {
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress("", email));
            mail.To.Add(new MailboxAddress("", email));
            mail.Subject = "TaskAllCompleted".ToLocalization();
            mail.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = "TaskAllCompleted".ToLocalization()
            };
            return mail;
        }

        private static (string Host, int Port, bool UseSSL, string Notes) GetSmtpConfigByEmail(string email)
        {
            if (!email.Contains('@') || email.Split('@').Length != 2)
                throw new ArgumentException("无效的邮箱地址格式");

            var domain = email.Split('@')[1].ToLower().Trim();
            var configs = new Dictionary<string, (string Host, int Port, bool UseSSL, string Notes)>
            {
                ["qq.com"] = ("smtp.qq.com", 465, true, "需使用授权码"),
                ["163.com"] = ("smtp.163.com", 994, true, "推荐使用SSL"),
                // ... 其他邮箱配置保持不变
            };

            return configs.TryGetValue(domain, out var config)
                ? HandleSpecialCases(domain, config)
                : throw new Exception("不支持的邮箱服务");
        }

        private static (string Host, int Port, bool UseSSL, string Notes) HandleSpecialCases(
            string domain,
            (string Host, int Port, bool UseSSL, string Notes) config)
        {
            // 处理特殊域名逻辑
            if (domain.EndsWith(".edu.cn")) return (config.Host.Replace("[DOMAIN]", domain), 25, false, "教育邮箱");
            if (domain.EndsWith(".gov.cn")) return (config.Host.Replace("[DOMAIN]", domain), 25, false, "政府邮箱");
            return config;
        }
    }

    #endregion

    #region 飞书通知

    public static class Lark
    {
        public async static Task<bool> SendAsync(string appId, string appSecret, CancellationToken cancellationToken = default)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var sign = GenerateSignature(timestamp, appSecret);

            var message = new
            {
                msg_type = "text",
                content = new
                {
                    text = "TaskAllCompleted".ToLocalization()
                }
            };

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync(
                    $"https://open.feishu.cn/open-apis/bot/v2/hook/{appId}?timestamp={timestamp}&sign={sign}",
                    new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"),
                    cancellationToken
                );
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("飞书通知已取消");
                return false;
            }
        }
        private static string GenerateSignature(string timestamp, string secret)
        {
            var stringToSign = $"{timestamp}\n{secret}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(hash);
        }
    }

    #endregion

    #region 微信公众号通知

    public static class WxPusher
    {
        public async static Task<bool> SendAsync(string appToken, string uid, CancellationToken cancellationToken = default)
        {
            const string apiUrl = "https://wxpusher.zjiecode.com/api/send/message";
            var payload = new
            {
                appToken,
                content = "TaskAllCompleted".ToLocalization(),
                contentType = 1,
                uids = new[]
                {
                    uid
                }
            };

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync(
                    apiUrl,
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
                    cancellationToken
                );
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("微信推送已取消");
                return false;
            }
        }
    }

    #endregion

    #region Telegram通知

    public static class Telegram
    {
        public async static Task<bool> SendAsync(string botToken, string chatId, CancellationToken cancellationToken = default)
        {
            var message = WebUtility.UrlEncode("TaskAllCompleted".ToLocalization());
            var apiUrl = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={message}";

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(apiUrl, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("电报通知已取消");
                return false;
            }
        }
    }

    #endregion

    #region SMTP通知

    public static class Smtp
    {
        public async static Task<bool> SendAsync(
            string host,
            string port,
            bool useSSL,
            bool requireLogin,
            string fromAddress,
            string toAddress,
            string username = "",
            string password = "",
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new SmtpClient();
                var secureOptions = useSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

                // 连接服务器（支持超时取消）
                await client.ConnectAsync(host, Convert.ToInt32(port), secureOptions, cancellationToken);

                // 认证逻辑（根据是否需要登录）
                if (requireLogin)
                {
                    ValidateCredentials(username, password);
                    await client.AuthenticateAsync(
                        new NetworkCredential(username, password),
                        cancellationToken
                    );
                }

                // 构建邮件消息
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("", fromAddress));
                message.To.Add(new MailboxAddress("", toAddress));
                message.Subject = "TaskAllCompleted".ToLocalization();
                message.Body = new TextPart("plain")
                {
                    Text = "TaskAllCompleted".ToLocalization()
                };

                // 发送操作（支持发送过程取消）
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                LoggerService.LogInfo("SMTP邮件发送成功");
                return true;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("SMTP邮件发送已取消");
                return false;
            }
            catch (AuthenticationException ex)
            {
                LoggerService.LogError($"SMTP认证失败: {ex.Message}");
                return false;
            }
            catch (SmtpProtocolException ex)
            {
                LoggerService.LogError($"SMTP协议错误: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"SMTP未知错误: {ex.Message}");
                return false;
            }
        }

        private static void ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("需要登录认证时用户名和密码不能为空");
            }
        }
    }

    #endregion

    #region Discord通知

    public static class Discord
    {
        public async static Task<bool> SendAsync(
            string channelId,
            string botToken,
            CancellationToken cancellationToken = default)
        {
            const string apiUrl = "https://discord.com/api/v10/channels/{0}/messages";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", botToken);

                var payload = new
                {
                    content = "TaskAllCompleted".ToLocalization(),
                    allowed_mentions = new
                    {
                        parse = new[]
                        {
                            "users"
                        }
                    }
                };

                var response = await client.PostAsync(
                    string.Format(apiUrl, channelId),
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
                    cancellationToken
                );

                if (response.IsSuccessStatusCode)
                {
                    LoggerService.LogInfo("Discord消息发送成功");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                LoggerService.LogError($"Discord消息发送失败: {errorContent}");
                return false;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("Discord消息发送已取消");
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Discord通信异常: {ex.Message}");
                return false;
            }
        }
    }

    #endregion

    #region QMsg酱通知

    public static class QMsg
    {
        public async static Task<bool> SendAsync(
            string serverUrl,
            string apiKey,
            string userQq,
            string botQq = "",
            CancellationToken cancellationToken = default)
        {
            var apiEndpoint = $"{serverUrl}/send/{apiKey}";

            try
            {
                using var client = new HttpClient();
                var parameters = new Dictionary<string, string>
                {
                    ["msg"] = "TaskAllCompleted".ToLocalization(),
                    ["qq"] = userQq
                };

                if (!string.IsNullOrEmpty(botQq))
                    parameters["bot"] = botQq;

                var content = new FormUrlEncodedContent(parameters);

                var response = await client.PostAsync(apiEndpoint, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode && responseBody.Contains("\"success\":true"))
                {
                    LoggerService.LogInfo("QMsg消息发送成功");
                    return true;
                }

                LoggerService.LogError($"QMsg发送失败: {responseBody}");
                return false;
            }
            catch (OperationCanceledException)
            {
                LoggerService.LogWarning("QMsg消息发送已取消");
                return false;
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"QMsg通信异常: {ex.Message}");
                return false;
            }
        }
    }

    #endregion
}
