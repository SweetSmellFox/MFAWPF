using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAWPF.Configuration;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper.Converters;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class ConnectSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private bool _rememberAdb = ConfigurationHelper.GetValue(ConfigurationKeys.RememberAdb, true);

    partial void OnRememberAdbChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.RememberAdb, value);
    }

    public static ObservableCollection<AdbScreencapMethods> AdbControlScreenCapTypes =>
    [
        AdbScreencapMethods.Default, AdbScreencapMethods.RawWithGzip, AdbScreencapMethods.RawByNetcat, AdbScreencapMethods.Encode, AdbScreencapMethods.EncodeToFileAndPull, AdbScreencapMethods.MinicapDirect,
        AdbScreencapMethods.MinicapStream, AdbScreencapMethods.EmulatorExtras
    ];

    public static ObservableCollection<Tool.LocalizationViewModel> AdbControlInputTypes =>
    [
        new("MiniTouch")
        {
            Other = AdbInputMethods.MinitouchAndAdbKey
        },
        new("MaaTouch")
        {
            Other = AdbInputMethods.Maatouch
        },
        new("AdbInput")
        {
            Other = AdbInputMethods.AdbShell
        },
        new("EmulatorExtras")
        {
            Other = AdbInputMethods.EmulatorExtras
        },
        new("AutoDetect")
        {
            Other = AdbInputMethods.All
        }
    ];
    public static ObservableCollection<Win32ScreencapMethod> Win32ControlScreenCapTypes => [Win32ScreencapMethod.FramePool, Win32ScreencapMethod.DXGIDesktopDup, Win32ScreencapMethod.GDI];
    public static ObservableCollection<Win32InputMethod> Win32ControlInputTypes => [Win32InputMethod.SendMessage, Win32InputMethod.Seize];

    [ObservableProperty] private AdbScreencapMethods _adbControlScreenCapType =
        ConfigurationHelper.GetValue(ConfigurationKeys.AdbControlScreenCapType, AdbScreencapMethods.Default, AdbScreencapMethods.None, new UniversalEnumConverter<AdbScreencapMethods>());
    [ObservableProperty] private AdbInputMethods _adbControlInputType =
        ConfigurationHelper.GetValue(ConfigurationKeys.AdbControlInputType, AdbInputMethods.MinitouchAndAdbKey, AdbInputMethods.None, new UniversalEnumConverter<AdbInputMethods>());
    [ObservableProperty] private Win32ScreencapMethod _win32ControlScreenCapType =
        ConfigurationHelper.GetValue(ConfigurationKeys.Win32ControlScreenCapType, Win32ScreencapMethod.FramePool, Win32ScreencapMethod.None, new UniversalEnumConverter<Win32ScreencapMethod>());
    [ObservableProperty] private Win32InputMethod _win32ControlInputType =
        ConfigurationHelper.GetValue(ConfigurationKeys.Win32ControlInputType, Win32InputMethod.SendMessage, Win32InputMethod.None, new UniversalEnumConverter<Win32InputMethod>());

    partial void OnAdbControlScreenCapTypeChanged(AdbScreencapMethods value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AdbControlScreenCapType, value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    partial void OnAdbControlInputTypeChanged(AdbInputMethods value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AdbControlInputType, value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    partial void OnWin32ControlScreenCapTypeChanged(Win32ScreencapMethod value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.Win32ControlScreenCapType, value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    partial void OnWin32ControlInputTypeChanged(Win32InputMethod value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.Win32ControlInputType, value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    [ObservableProperty] private bool _retryOnDisconnected = ConfigurationHelper.GetValue(ConfigurationKeys.RetryOnDisconnected, false);

    partial void OnRetryOnDisconnectedChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.RetryOnDisconnected, value);
    }
    [ObservableProperty] private bool _allowAdbRestart = ConfigurationHelper.GetValue(ConfigurationKeys.AllowAdbRestart, true);

    partial void OnAllowAdbRestartChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AllowAdbRestart, value);
    }

    [ObservableProperty] private bool _allowAdbHardRestart = ConfigurationHelper.GetValue(ConfigurationKeys.AllowAdbHardRestart, true);

    partial void OnAllowAdbHardRestartChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AllowAdbHardRestart, value);
    }
}
