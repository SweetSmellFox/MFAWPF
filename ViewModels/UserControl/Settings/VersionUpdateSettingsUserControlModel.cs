using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Configuration;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Views.UI;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class VersionUpdateSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private string _maaFwVersion = MaaProcessor.MaaUtility.Version;
    [ObservableProperty] private string _mfaVersion = RootView.Version;
    [ObservableProperty] private string _resourceVersion = string.Empty;
    [ObservableProperty] private bool _showResourceVersion;
    partial void OnResourceVersionChanged(string value)
    {
        ShowResourceVersion = !string.IsNullOrWhiteSpace(value);
    }

    public ObservableCollection<Tool.LocalizationViewModel> DownloadSourceList =>
    [
        new()
        {
            Name = "GitHub"
        },
        new("MirrorChyan"),
    ];

    [ObservableProperty] private int _downloadSourceIndex = ConfigurationHelper.GetValue(ConfigurationKeys.DownloadSourceIndex, 0);

    partial void OnDownloadSourceIndexChanged(int value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.DownloadSourceIndex, value);
    }

    [ObservableProperty] private string _cdkPassword = SimpleEncryptionHelper.Decrypt(ConfigurationHelper.GetValue(ConfigurationKeys.DownloadCDK, string.Empty));

    partial void OnCdkPasswordChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.DownloadCDK, SimpleEncryptionHelper.Encrypt(value));
    }

    [ObservableProperty] private bool _enableCheckVersion = ConfigurationHelper.GetValue(ConfigurationKeys.EnableCheckVersion, true);

    [ObservableProperty] private bool _enableAutoUpdateResource = ConfigurationHelper.GetValue(ConfigurationKeys.EnableAutoUpdateResource, false);

    [ObservableProperty] private bool _enableAutoUpdateMFA = ConfigurationHelper.GetValue(ConfigurationKeys.EnableAutoUpdateMFA, false);

    partial void OnEnableCheckVersionChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.EnableCheckVersion, value);
    }

    partial void OnEnableAutoUpdateResourceChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.EnableAutoUpdateResource, value);
    }

    partial void OnEnableAutoUpdateMFAChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.EnableAutoUpdateMFA, value);
    }

    [RelayCommand]
    private void UpdateResource()
    {
        VersionChecker.UpdateResourceAsync();
    }
    [RelayCommand]
    private void CheckResourceUpdate()
    {
        VersionChecker.CheckResourceVersionAsync();
    }
    [RelayCommand]
    private void UpdateMFA()
    {
        VersionChecker.UpdateMFAAsync();
    }
    [RelayCommand]
    private void CheckMFAUpdate()
    {
        VersionChecker.CheckMFAVersionAsync();
    }
    [RelayCommand]
    private void UpdateMaaFW()
    {
        VersionChecker.UpdateMaaFwAsync();
    }
}
