using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper.Converters;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class ConnectSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private bool _rememberAdb = MFAConfiguration.GetConfiguration("RememberAdb", true);

    partial void OnRememberAdbChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("RememberAdb", value);
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
        new("AutoDetect")
        {
            Other = AdbInputMethods.All
        }
    ];
    public static ObservableCollection<Win32ScreencapMethod> Win32ControlScreenCapTypes => [Win32ScreencapMethod.FramePool, Win32ScreencapMethod.DXGIDesktopDup, Win32ScreencapMethod.GDI];
    public static ObservableCollection<Win32InputMethod> Win32ControlInputTypes => [Win32InputMethod.SendMessage, Win32InputMethod.Seize];

    [ObservableProperty] private AdbScreencapMethods _adbControlScreenCapType = MFAConfiguration.GetConfiguration("AdbControlScreenCapType", AdbScreencapMethods.Default, AdbScreencapMethods.None,new UniversalEnumConverter<AdbScreencapMethods>());
    [ObservableProperty] private AdbInputMethods _adbControlInputType = MFAConfiguration.GetConfiguration("AdbControlInputType", AdbInputMethods.MinitouchAndAdbKey, AdbInputMethods.None,new UniversalEnumConverter<AdbInputMethods>());
    [ObservableProperty] private Win32ScreencapMethod _win32ControlScreenCapType = MFAConfiguration.GetConfiguration("Win32ControlScreenCapType", Win32ScreencapMethod.FramePool, Win32ScreencapMethod.None,new UniversalEnumConverter<Win32ScreencapMethod>());
    [ObservableProperty] private Win32InputMethod _win32ControlInputType = MFAConfiguration.GetConfiguration("Win32ControlInputType", Win32InputMethod.SendMessage, Win32InputMethod.None,new UniversalEnumConverter<Win32InputMethod>());

    partial void OnAdbControlScreenCapTypeChanged(AdbScreencapMethods value)
    {
        MFAConfiguration.SetConfiguration("AdbControlScreenCapType", value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    partial void OnAdbControlInputTypeChanged(AdbInputMethods value)
    {
        MFAConfiguration.SetConfiguration("AdbControlInputType", value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    partial void OnWin32ControlScreenCapTypeChanged(Win32ScreencapMethod value)
    {
        MFAConfiguration.SetConfiguration("Win32ControlScreenCapType", value.ToString());
        MaaProcessor.Instance.SetCurrentTasker();
    }

    partial void OnWin32ControlInputTypeChanged(Win32InputMethod value)
    {
        MFAConfiguration.SetConfiguration("Win32ControlInputType", value.ToString());
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
