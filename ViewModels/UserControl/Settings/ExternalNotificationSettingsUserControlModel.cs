using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Configuration;
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
        DingTalkEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.DingTalkKey);
        EmailEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.EmailKey);
        LarkEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.LarkKey);
        QmsgEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.QmsgKey);
        WxPusherEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.WxPusherKey);
        SmtpEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.SmtpKey);
        TelegramEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.TelegramKey);
        DiscordEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.DiscordKey);
        OnebotEnabled = EnabledExternalNotificationProviderList.Contains(ExternalNotificationHelper.Key.OneBotKey);
    }

    public static List<Tool.LocalizationViewModel> ExternalNotificationProvidersShow => ExternalNotificationProviders;

    private static object[] _enabledExternalNotificationProviders = ExternalNotificationProviders.Where(s => ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationEnabled, string.Empty).Split(',').Contains(s.ResourceKey))
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
                ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationEnabled, config);
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
        => ExternalNotificationHelper.ExternalNotificationAsync();

    #endregion

    #region 钉钉

    [ObservableProperty] private bool _dingTalkEnabled;

    [ObservableProperty] private string _dingTalkToken = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationDingTalkToken, string.Empty);
    private static bool TryExtractDingTalkToken(string url, out string token)
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
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationDingTalkToken, SimpleEncryptionHelper.Encrypt(value));
    }

    [ObservableProperty] private string _dingTalkSecret = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationDingTalkSecret, string.Empty);
    partial void OnDingTalkSecretChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationDingTalkSecret, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region 邮箱

    [ObservableProperty] private bool _emailEnabled;

    [ObservableProperty] private string _emailAccount = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationEmailAccount, string.Empty);
    partial void OnEmailAccountChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationEmailAccount, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _emailSecret = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationEmailSecret, string.Empty);
    partial void OnEmailSecretChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationEmailSecret, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region 飞书

    [ObservableProperty] private bool _larkEnabled;
    [ObservableProperty] private string _larkId = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationLarkID, string.Empty);

    partial void OnLarkIdChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationLarkID, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _larkToken = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationLarkToken, string.Empty);
    partial void OnLarkTokenChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationLarkToken, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region 微信公众号

    [ObservableProperty] private bool _wxPusherEnabled;

    [ObservableProperty] private string _wxPusherToken = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationWxPusherToken, string.Empty);

    partial void OnWxPusherTokenChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationWxPusherToken, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _wxPusherUid = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationWxPusherUID, string.Empty);
    partial void OnWxPusherUidChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationWxPusherUID, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region Telegram

    [ObservableProperty] private bool _telegramEnabled;

    [ObservableProperty] private string _telegramBotToken = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationTelegramBotToken, string.Empty);

    partial void OnTelegramBotTokenChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationTelegramBotToken, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _telegramChatId = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationTelegramChatId, string.Empty);
    partial void OnTelegramChatIdChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationTelegramChatId, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region Discord

    [ObservableProperty] private bool _discordEnabled;

    [ObservableProperty] private string _discordBotToken = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationDiscordBotToken, string.Empty);
    partial void OnDiscordBotTokenChanged(string value)
        => ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationDiscordBotToken, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _discordUserId = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationDiscordUserId, string.Empty);
    partial void OnDiscordUserIdChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationDiscordUserId, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region SMTP

    [ObservableProperty] private bool _smtpEnabled;


    [ObservableProperty] private string _smtpServer = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpServer, string.Empty);

    partial void OnSmtpServerChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationSmtpServer, SimpleEncryptionHelper.Encrypt(value));
    }


    [ObservableProperty] private string _smtpPort = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpPort, string.Empty);

    partial void OnSmtpPortChanged(string value) =>
        ConfigurationHelper.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpPort, value);

    [ObservableProperty] private string _smtpUser = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpUser, string.Empty);

    partial void OnSmtpUserChanged(string value) =>
        ConfigurationHelper.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpUser, value);

    [ObservableProperty] private string _smtpPassword = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpPassword, string.Empty);

    partial void OnSmtpPasswordChanged(string value) =>
        ConfigurationHelper.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpPassword, value);

    [ObservableProperty] private string _smtpFrom = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpFrom, string.Empty);

    partial void OnSmtpFromChanged(string value) =>
        ConfigurationHelper.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpFrom, value);

    [ObservableProperty] private string _smtpTo = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationSmtpTo, string.Empty);

    partial void OnSmtpToChanged(string value) =>
        ConfigurationHelper.SetEncrypted(ConfigurationKeys.ExternalNotificationSmtpTo, value);

    [ObservableProperty] private bool _smtpUseSsl = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationSmtpUseSsl, false);

    partial void OnSmtpUseSslChanged(bool value) =>
        ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationSmtpUseSsl, value);

    [ObservableProperty] private bool _smtpRequireAuthentication = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationSmtpRequiresAuthentication, false);
    partial void OnSmtpRequireAuthenticationChanged(bool value) =>
        ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationSmtpRequiresAuthentication, value);

    #endregion

    #region QMsg

    [ObservableProperty] private bool _qmsgEnabled;

    [ObservableProperty] private string _qmsgServer = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgServer, string.Empty);

    partial void OnQmsgServerChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationQmsgServer, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _qmsgKey = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgKey, string.Empty);

    partial void OnQmsgKeyChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationQmsgKey, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _qmsgUser = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgUser, string.Empty);
    partial void OnQmsgUserChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationQmsgUser, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _qmsgBot = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationQmsgBot, string.Empty);
    partial void OnQmsgBotChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationQmsgBot, SimpleEncryptionHelper.Encrypt(value));

    #endregion

    #region OneBot

    [ObservableProperty] private bool _onebotEnabled;

    [ObservableProperty] private string _onebotServer = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationOneBotServer, string.Empty);

    partial void OnOnebotServerChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationOneBotServer, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _onebotKey = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationOneBotKey, string.Empty);

    partial void OnOnebotKeyChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationOneBotKey, SimpleEncryptionHelper.Encrypt(value));


    [ObservableProperty] private string _onebotUser = ConfigurationHelper.GetDecrypt(ConfigurationKeys.ExternalNotificationOneBotUser, string.Empty);
    partial void OnOnebotUserChanged(string value)
        =>
            ConfigurationHelper.SetValue(ConfigurationKeys.ExternalNotificationOneBotUser, SimpleEncryptionHelper.Encrypt(value));

    #endregion
}
