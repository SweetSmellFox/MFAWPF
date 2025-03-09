using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Helper;
using MFAWPF.ViewModels.Tool;
using System.Web;


namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class ExternalNotificationSettingsUserControlModel : ViewModel
{
    #region 初始

    public ExternalNotificationSettingsUserControlModel()
    {
        UpdateExternalNotificationProvider();
    }

    public static readonly List<LocalizationViewModel> ExternalNotificationProviders = ExternalNotificationHelper.Key.AllKeys.Select(k => new LocalizationViewModel(k)).ToList();

    public void UpdateExternalNotificationProvider()
    {
        DingTalkEnabled = EnabledExternalNotificationProviderList.Contains("DingTalk");
        EmailEnabled = EnabledExternalNotificationProviderList.Contains("Email");
        LarkEnabled = EnabledExternalNotificationProviderList.Contains("Lark");
        QmsgEnabled = EnabledExternalNotificationProviderList.Contains("Qmsg");
        WxPusherEnabled = EnabledExternalNotificationProviderList.Contains("WxPusher");
        SmtpEnabled = EnabledExternalNotificationProviderList.Contains("SMTP");
        TelegramEnabled = EnabledExternalNotificationProviderList.Contains("Telegram");
        DiscordEnabled = EnabledExternalNotificationProviderList.Contains("Discord");
    }

    public static List<Tool.LocalizationViewModel> ExternalNotificationProvidersShow => ExternalNotificationProviders;

    private static object[] _enabledExternalNotificationProviders = ExternalNotificationProviders.Where(s => MFAConfiguration.GetConfiguration(ConfigurationKeys.ExternalNotificationEnabled, string.Empty).Split(',').Contains(s.ResourceKey))
        .Distinct()
        .ToArray();

    public object[] EnabledExternalNotificationProviders
    {
        get => _enabledExternalNotificationProviders;

        set
        {
            try
            {
                var settingViewModels = value.Cast<Tool.LocalizationViewModel>();
                SetProperty(ref _enabledExternalNotificationProviders, value);
                var validProviders = settingViewModels
                    .Where(provider => ExternalNotificationProviders.ContainsKey(provider.ResourceKey ?? string.Empty))
                    .Select(provider => provider.ResourceKey)
                    .Distinct();

                var config = string.Join(",", validProviders);
                MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationEnabled, config);
                UpdateExternalNotificationProvider();
                EnabledExternalNotificationProviderCount = _enabledExternalNotificationProviders.Length;
            }
            catch (Exception e)
            {
                LoggerService.LogError(e);
            }
        }
    }

    [ObservableProperty] private int _enabledExternalNotificationProviderCount = _enabledExternalNotificationProviders.Length;

    public string[] EnabledExternalNotificationProviderList => EnabledExternalNotificationProviders
        .Select(s => s.ToString() ?? string.Empty)
        .ToArray();


    [RelayCommand]
    private void ExternalNotificationSendTest()
        =>
            ExternalNotificationHelper.ExternalNotificationAsync();

    #endregion

    #region 钉钉

    [ObservableProperty] private bool _dingTalkEnabled;

    [ObservableProperty] private string _dingTalkToken = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationDingTalkToken, string.Empty);
    public static bool TryExtractDingTalkToken(string url, out string token)
    {
        token = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return false;
            }

            var uri = new Uri(url);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            string rawToken = queryParams["access_token"];

            if (string.IsNullOrEmpty(rawToken))
            {
                return false;
            }

            token = rawToken;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    partial void OnDingTalkTokenChanged(string value)
    {
        if (TryExtractDingTalkToken(value, out var token))
            DingTalkToken = token;
        else
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationDingTalkToken, SimpleEncryptionHelper.Encrypt(value));
    }

    [ObservableProperty] private string _dingTalkSecret = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationDingTalkSecret, string.Empty);
    partial void OnDingTalkSecretChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationDingTalkSecret, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region 邮箱

    [ObservableProperty] private bool _emailEnabled;

    [ObservableProperty] private string _emailAccount = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationEmailAccount, string.Empty);
    partial void OnEmailAccountChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationEmailAccount, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _emailSecret = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationEmailSecret, string.Empty);
    partial void OnEmailSecretChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationEmailSecret, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region 飞书

    [ObservableProperty] private bool _larkEnabled;
    [ObservableProperty] private string _larkId = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationLarkID, string.Empty);

    partial void OnLarkIdChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationLarkID, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _larkToken = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationLarkToken, string.Empty);
    partial void OnLarkTokenChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationLarkToken, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region 微信公众号

    [ObservableProperty] private bool _wxPusherEnabled;

    [ObservableProperty] private string _wxPusherToken = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationWxPusherToken, string.Empty);

    partial void OnWxPusherTokenChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationWxPusherToken, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _wxPusherUid = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationWxPusherUID, string.Empty);
    partial void OnWxPusherUidChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationWxPusherUID, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region Telegram

    [ObservableProperty] private bool _telegramEnabled;

    [ObservableProperty] private string _telegramBotToken = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationTelegramBotToken, string.Empty);

    partial void OnTelegramBotTokenChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationTelegramBotToken, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _telegramChatId = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationTelegramChatId, string.Empty);
    partial void OnTelegramChatIdChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationTelegramChatId, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region Discord

    [ObservableProperty] private bool _discordEnabled;

    [ObservableProperty] private string _discordBotToken = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationDiscordBotToken, string.Empty);
    partial void OnDiscordBotTokenChanged(string value)
        => MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationDiscordBotToken, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _discordUserId = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationDiscordUserId, string.Empty);
    partial void OnDiscordUserIdChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationDiscordUserId, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region SMTP

    [ObservableProperty] private bool _smtpEnabled;


    [ObservableProperty] private string _smtpServer = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpServer, string.Empty);

    partial void OnSmtpServerChanged(string value)
    {
        MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationSmtpServer, SimpleEncryptionHelper.Encrypt(value));
    }


    [ObservableProperty] private string _smtpPort = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpPort, string.Empty);

    partial void OnSmtpPortChanged(string value) =>
        MFAConfiguration.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpPort, value);

    [ObservableProperty] private string _smtpUser = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpUser, string.Empty);

    partial void OnSmtpUserChanged(string value) =>
        MFAConfiguration.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpUser, value);

    [ObservableProperty] private string _smtpPassword = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpPassword, string.Empty);

    partial void OnSmtpPasswordChanged(string value) =>
        MFAConfiguration.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpPassword, value);

    [ObservableProperty] private string _smtpFrom = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpFrom, string.Empty);

    partial void OnSmtpFromChanged(string value) =>
        MFAConfiguration.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpFrom, value);

    [ObservableProperty] private string _smtpTo = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpTo, string.Empty);

    partial void OnSmtpToChanged(string value) =>
        MFAConfiguration.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpTo, value);

    [ObservableProperty] private bool _smtpUseSsl = MFAConfiguration.GetConfiguration(ConfigurationKeys.ExternalNotificationSmtpUseSsl, false);

    partial void OnSmtpUseSslChanged(bool value) =>
        MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationSmtpUseSsl, value.ToString());

    [ObservableProperty] private bool _smtpRequireAuthentication = MFAConfiguration.GetConfiguration(ConfigurationKeys.ExternalNotificationSmtpRequiresAuthentication, false);

    partial void OnSmtpRequireAuthenticationChanged(bool value) =>
        MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationSmtpRequiresAuthentication, value.ToString());

    #endregion

    #region QMsg

    [ObservableProperty] private bool _qmsgEnabled;

    [ObservableProperty] private string _qmsgServer = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgServer, string.Empty);

    partial void OnQmsgServerChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationQmsgServer, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _qmsgKey = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgKey, string.Empty);

    partial void OnQmsgKeyChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationQmsgKey, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _qmsgUser = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgUser, string.Empty);
    partial void OnQmsgUserChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationQmsgUser, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _qmsgBot = MFAConfiguration.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgBot, string.Empty);
    partial void OnQmsgBotChanged(string value)
        =>
            MFAConfiguration.SetConfiguration(ConfigurationKeys.ExternalNotificationQmsgBot, SimpleEncryptionHelper.Encrypt(value));

    #endregion
}
