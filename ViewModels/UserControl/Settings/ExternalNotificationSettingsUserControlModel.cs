using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class ExternalNotificationSettingsUserControlModel : ViewModel
{
    public ExternalNotificationSettingsUserControlModel()
    {
        UpdateExternalNotificationProvider();
    }
    
    public static readonly List<Tool.LocalizationViewModel> ExternalNotificationProviders =
    [
        new("DingTalk"), new("Email"),
    ];

    public static List<Tool.LocalizationViewModel> ExternalNotificationProvidersShow => ExternalNotificationProviders;

    private static object[] _enabledExternalNotificationProviders = ExternalNotificationProviders.Where(s => MFAConfiguration.GetConfiguration("ExternalNotificationEnabled", string.Empty).Split(',').Contains(s.ResourceKey))
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
                MFAConfiguration.SetConfiguration("ExternalNotificationEnabled", config);
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


    [ObservableProperty] private bool _dingTalkEnabled;
    [ObservableProperty] private bool _emailEnabled;
    public string[] EnabledExternalNotificationProviderList => EnabledExternalNotificationProviders
        .Select(s => s.ToString() ?? string.Empty)
        .ToArray();

    [ObservableProperty] private string _dingTalkToken = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationDingTalkToken", string.Empty));
    partial void OnDingTalkTokenChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationDingTalkToken", SimpleEncryptionHelper.Encrypt(value));
    }


    [ObservableProperty] private string _dingTalkSecret = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationDingTalkSecret", string.Empty));
    partial void OnDingTalkSecretChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationDingTalkSecret", SimpleEncryptionHelper.Encrypt(value));
    }

    [ObservableProperty] private string _emailAccount = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationEmailAccount", string.Empty));
    partial void OnEmailAccountChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationEmailAccount", SimpleEncryptionHelper.Encrypt(value));
    }


    [ObservableProperty] private string _emailSecret = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationEmailSecret", string.Empty));
    partial void OnEmailSecretChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationEmailSecret", SimpleEncryptionHelper.Encrypt(value));
    }

    public void UpdateExternalNotificationProvider()
    {
        DingTalkEnabled = EnabledExternalNotificationProviderList.Contains("DingTalk");
        EmailEnabled = EnabledExternalNotificationProviderList.Contains("Email");
    }


    [RelayCommand]
    private void ExternalNotificationSendTest()
    {
        MaaProcessor.ExternalNotificationAsync();
    }
}
