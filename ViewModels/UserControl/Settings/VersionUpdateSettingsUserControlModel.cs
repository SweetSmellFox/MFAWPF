using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Data;
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

    [ObservableProperty] private int _downloadSourceIndex = MFAConfiguration.GetConfiguration("DownloadSourceIndex", 0);

    partial void OnDownloadSourceIndexChanged(int value)
    {
        MFAConfiguration.SetConfiguration("DownloadSourceIndex", value);
    }

    [ObservableProperty] private string _cdkPassword = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("DownloadCDK", string.Empty));

    partial void OnCdkPasswordChanged(string value)
    {
        MFAConfiguration.SetConfiguration("DownloadCDK", SimpleEncryptionHelper.Encrypt(value));
    }

    [ObservableProperty] private bool _enableCheckVersion = MFAConfiguration.GetConfiguration("EnableCheckVersion", true);

    [ObservableProperty] private bool _enableAutoUpdateResource = MFAConfiguration.GetConfiguration("EnableAutoUpdateResource", false);

    [ObservableProperty] private bool _enableAutoUpdateMFA = MFAConfiguration.GetConfiguration("EnableAutoUpdateMFA", false);

    partial void OnEnableCheckVersionChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("EnableCheckVersion", value);
    }

    partial void OnEnableAutoUpdateResourceChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("EnableAutoUpdateResource", value);
    }

    partial void OnEnableAutoUpdateMFAChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("EnableAutoUpdateMFA", value);
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
