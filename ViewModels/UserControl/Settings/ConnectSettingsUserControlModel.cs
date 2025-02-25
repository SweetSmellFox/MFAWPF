using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using MFAWPF.Extensions.Maa;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class ConnectSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private bool _rememberAdb = MFAConfiguration.GetConfiguration("RememberAdb", true);

    partial void OnRememberAdbChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("RememberAdb", value);
    }

    public static ObservableCollection<string> AdbControlScreenCapTypes =>
    [
        "Default", "RawWithGzip", "RawByNetcat",
        "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
        "EmulatorExtras"
    ];
    public static ObservableCollection<Tool.LocalizationViewModel> AdbControlInputTypes => [new("MiniTouch"), new("MaaTouch"), new("AdbInput"), new("AutoDetect")];
    public static ObservableCollection<string> Win32ControlScreenCapTypes => ["FramePool", "DXGIDesktopDup", "GDI"];
    public static ObservableCollection<string> Win32ControlInputTypes => ["Seize", "SendMessage"];

    [ObservableProperty] private string _adbControlScreenCapType = MFAConfiguration.GetConfiguration("AdbControlScreenCapType", AdbControlScreenCapTypes[0]);
    [ObservableProperty] private string _adbControlInputType = MFAConfiguration.GetConfiguration("AdbControlInputType", AdbControlInputTypes[0].ResourceKey);
    [ObservableProperty] private string _win32ControlScreenCapType = MFAConfiguration.GetConfiguration("Win32ControlScreenCapType", Win32ControlScreenCapTypes[0]);
    [ObservableProperty] private string _win32ControlInputType = MFAConfiguration.GetConfiguration("Win32ControlInputType", Win32ControlInputTypes[0]);
    partial void OnAdbControlScreenCapTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("AdbControlScreenCapType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }
    
    partial void OnAdbControlInputTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("AdbControlInputType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }
    
    partial void OnWin32ControlScreenCapTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Win32ControlScreenCapType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }
    
    partial void OnWin32ControlInputTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Win32ControlInputType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }

    [ObservableProperty] private bool _retryOnDisconnected = MFAConfiguration.GetConfiguration("RetryOnDisconnected", false);

    partial void OnRetryOnDisconnectedChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("RetryOnDisconnected", value);
    }
    [ObservableProperty] private bool _allowAdbRestart = MFAConfiguration.GetConfiguration("AllowAdbRestart", true);

    partial void OnAllowAdbRestartChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("AllowAdbRestart", value);
    }

    [ObservableProperty] private bool _allowAdbHardRestart = MFAConfiguration.GetConfiguration("AllowAdbHardRestart", true);

    partial void OnAllowAdbHardRestartChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("AllowAdbHardRestart", value);
    }
}
