using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaFramework.Binding;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels.Tool;
using MFAWPF.Views.UI.Dialog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using WPFLocalizeExtension.Extensions;

namespace MFAWPF.ViewModels.UI;

public partial class ConnectingViewModel : ViewModel
{
    [ObservableProperty] private ObservableCollection<object> _devices = [];
    [ObservableProperty] private int _devicesIndex = 0;
    [ObservableProperty] private object? _currentDevice;

    partial void OnCurrentDeviceChanged(object? value)
    {
        if (value is DesktopWindowInfo window)
        {
            GrowlHelper.Info(string.Format(LocExtension.GetLocalizedValue<string>("WindowSelectionMessage"),
                window.Name));
            MaaProcessor.MaaFwConfiguration.DesktopWindow.Name = window.Name;
            MaaProcessor.MaaFwConfiguration.DesktopWindow.HWnd = window.Handle;
            MaaProcessor.Instance.SetCurrentTasker();
        }
        else if (value is AdbDeviceInfo device)
        {
            GrowlHelper.Info(string.Format(LocExtension.GetLocalizedValue<string>("EmulatorSelectionMessage"),
                device.Name));
            MaaProcessor.MaaFwConfiguration.AdbDevice.Name = device.Name;
            MaaProcessor.MaaFwConfiguration.AdbDevice.AdbPath = device.AdbPath;
            MaaProcessor.MaaFwConfiguration.AdbDevice.AdbSerial = device.AdbSerial;
            MaaProcessor.MaaFwConfiguration.AdbDevice.Config = device.Config;
            MaaProcessor.Instance.SetCurrentTasker();
            ConfigurationHelper.SetValue(ConfigurationKeys.AdbDevice, device);
        }
    }

    public ObservableCollection<LocalizationViewModel> Controllers =>
    [
        new("TabADB")
        {
            Other = MaaControllerTypes.Adb,
        },
        new("TabWin32")
        {
            Other = MaaControllerTypes.Win32,
        },
    ];

    [ObservableProperty] private MaaControllerTypes _currentController = ConfigurationHelper.GetValue(ConfigurationKeys.CurrentController, MaaControllerTypes.Adb, MaaControllerTypes.None, new UniversalEnumConverter<MaaControllerTypes>());

    partial void OnCurrentControllerChanged(MaaControllerTypes value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.CurrentController, value.ToString());
        AutoDetectDevice();
        MaaProcessor.Instance.SetCurrentTasker();
    }

    [ObservableProperty] private bool _isConnected;
    public void SetConnected(bool isConnected)
    {
        IsConnected = isConnected;
    }


    [RelayCommand]
    public void CustomAdb()
    {
        var deviceInfo = CurrentDevice as AdbDeviceInfo;
        var dialog = new AdbEditorDialog(deviceInfo);
        if (dialog.ShowDialog().IsTrue())
        {
            Devices = [dialog.Output];
            CurrentDevice = dialog.Output;
        }
    }

    public static int ExtractNumberFromEmulatorConfig(string emulatorConfig)
    {
        var match = Regex.Match(emulatorConfig, @"\d+");

        if (match.Success)
        {
            return int.Parse(match.Value);
        }

        return 0;
    }

    public bool TryGetIndexFromConfig(string config, out int index)
    {
        try
        {
            using var doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("extras", out var extras) && extras.TryGetProperty("mumu", out var mumu) && mumu.TryGetProperty("index", out var indexElement))
            {
                index = indexElement.GetInt32();
                return true;
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError(ex);
        }

        index = 0;
        return false;
    }
    
    [RelayCommand]
    private void Refresh()
    {
        TaskManager.RunTaskAsync(AutoDetectDevice);
    }

    public void AutoDetectDevice()
    {
        var isAdb = CurrentController == MaaControllerTypes.Adb;
        try
        {
            GrowlHelper.Info(GetDetectionMessage(isAdb));
            SetConnected(false);

            var (devices, index) = isAdb ? DetectAdbDevices() : DetectWin32Windows();

            UpdateDeviceList(devices, index);
            HandleControllerSettings(isAdb);
            UpdateConnectionStatus(devices.Count > 0, isAdb);
        }
        catch (Exception ex)
        {
            HandleDetectionError(ex, isAdb);
        }
    }

    private string GetDetectionMessage(bool isAdb) =>
        LocExtension.GetLocalizedValue<string>(isAdb ? "EmulatorDetectionStarted" : "WindowDetectionStarted");

    private (ObservableCollection<object> devices, int index) DetectAdbDevices()
    {
        var devices = MaaProcessor.MaaToolkit.AdbDevice.Find();
        var index = CalculateAdbDeviceIndex(devices);
        return (new(devices), index);
    }

    private int CalculateAdbDeviceIndex(IList<AdbDeviceInfo> devices)
    {
        var config = ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty);
        if (string.IsNullOrWhiteSpace(config)) return 0;

        var targetNumber = ExtractNumberFromEmulatorConfig(config);
        return devices.Select((d, i) =>
                TryGetIndexFromConfig(d.Config, out var index) && index == targetNumber ? i : -1)
            .FirstOrDefault(i => i >= 0);
    }

    private (ObservableCollection<object> devices, int index) DetectWin32Windows()
    {
        var windows = MaaProcessor.MaaToolkit.Desktop.Window.Find().ToList();
        var index = CalculateWindowIndex(windows);
        return (new(windows), index);
    }

    private int CalculateWindowIndex(List<DesktopWindowInfo> windows)
    {
        var controller = MaaInterface.Instance?.Controller?
            .FirstOrDefault(c => c.Type?.Equals("win32", StringComparison.OrdinalIgnoreCase) == true);

        if (controller?.Win32 == null)
            return windows.FindIndex(win => !string.IsNullOrWhiteSpace(win.Name));

        var filtered = windows.Where(win =>
            !string.IsNullOrWhiteSpace(win.Name) || !string.IsNullOrWhiteSpace(win.ClassName)).ToList();

        filtered = ApplyRegexFilters(filtered, controller.Win32);
        return filtered.Count > 0 ? windows.IndexOf(filtered.First()) : 0;
    }

    private List<DesktopWindowInfo> ApplyRegexFilters(List<DesktopWindowInfo> windows, MaaInterface.MaaResourceControllerWin32 win32)
    {
        var filtered = windows;
        if (!string.IsNullOrWhiteSpace(win32.WindowRegex))
        {
            var regex = new Regex(win32.WindowRegex);
            filtered = filtered.Where(w => regex.IsMatch(w.Name)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(win32.ClassRegex))
        {
            var regex = new Regex(win32.ClassRegex);
            filtered = filtered.Where(w => regex.IsMatch(w.ClassName)).ToList();
        }
        return filtered;
    }

    private void UpdateDeviceList(ObservableCollection<object> devices, int index)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            Devices = devices;
            DevicesIndex = index;
        });
    }

    private void HandleControllerSettings(bool isAdb)
    {
        var controller = MaaInterface.Instance?.Controller?
            .FirstOrDefault(c => c.Type?.Equals(isAdb ? "adb" : "win32", StringComparison.OrdinalIgnoreCase) == true);

        if (controller == null) return;

        HandleInputSettings(controller, isAdb);
        HandleScreenCapSettings(controller, isAdb);
    }

    private void HandleInputSettings(MaaInterface.MaaResourceController controller, bool isAdb)
    {
        var input = isAdb ? controller.Adb?.Input : controller.Win32?.Input;
        if (input == null) return;

        if (isAdb)
        {
            Instances.ConnectSettingsUserControlModel.AdbControlInputType = input switch
            {
                1 => AdbInputMethods.AdbShell,
                2 => AdbInputMethods.MinitouchAndAdbKey,
                4 => AdbInputMethods.Maatouch,
                8 => AdbInputMethods.EmulatorExtras,
                _ => Instances.ConnectSettingsUserControlModel.AdbControlInputType
            };
        }
        else
        {
            Instances.ConnectSettingsUserControlModel.Win32ControlInputType = input switch
            {
                1 => Win32InputMethod.Seize,
                2 => Win32InputMethod.SendMessage,
                _ => Instances.ConnectSettingsUserControlModel.Win32ControlInputType
            };
        }
    }

    private void HandleScreenCapSettings(MaaInterface.MaaResourceController controller, bool isAdb)
    {
        var screenCap = isAdb ? controller.Adb?.ScreenCap : controller.Win32?.ScreenCap;
        if (screenCap == null) return;
        if (isAdb)
        {
            Instances.ConnectSettingsUserControlModel.AdbControlScreenCapType = screenCap switch
            {
                1 => AdbScreencapMethods.EncodeToFileAndPull,
                2 => AdbScreencapMethods.Encode,
                4 => AdbScreencapMethods.RawWithGzip,
                8 => AdbScreencapMethods.RawByNetcat,
                16 => AdbScreencapMethods.MinicapDirect,
                32 => AdbScreencapMethods.MinicapStream,
                64 => AdbScreencapMethods.EmulatorExtras,
                _ => Instances.ConnectSettingsUserControlModel.AdbControlScreenCapType
            };
        }
        else
        {
            Instances.ConnectSettingsUserControlModel.Win32ControlScreenCapType = screenCap switch
            {
                1 => Win32ScreencapMethod.GDI,
                2 => Win32ScreencapMethod.FramePool,
                4 => Win32ScreencapMethod.DXGIDesktopDup,
                _ => Instances.ConnectSettingsUserControlModel.Win32ControlScreenCapType
            };
        }
    }

    private void UpdateConnectionStatus(bool hasDevices, bool isAdb)
    {
        if (!hasDevices)
        {
            GrowlHelper.Info(LocExtension.GetLocalizedValue<string>(
                isAdb ? "NoEmulatorFound" : "NoWindowFound"));
        }
    }

    private void HandleDetectionError(Exception ex, bool isAdb)
    {
        var targetType = isAdb ? "Emulator" : "Window";
        GrowlHelper.Warning(string.Format(
            LocExtension.GetLocalizedValue<string>("TaskStackError"),
            targetType.ToLocalization(),
            ex.Message));

        LoggerService.LogError(ex);
    }

    public void TryReadAdbDeviceFromConfig()
    {
        if (CurrentController != MaaControllerTypes.Adb
            || !ConfigurationHelper.GetValue(ConfigurationKeys.RememberAdb, true)
            || MaaProcessor.MaaFwConfiguration.AdbDevice.AdbPath != "adb"
            || !ConfigurationHelper.TryGetValue(ConfigurationKeys.AdbDevice, out AdbDeviceInfo device,
                new UniversalEnumConverter<AdbInputMethods>(), new UniversalEnumConverter<AdbScreencapMethods>()))
        {
            DispatcherHelper.RunOnMainThread(AutoDetectDevice);
            return;
        }

        DispatcherHelper.RunOnMainThread(() =>
        {
            Devices = [device];
            CurrentDevice = device;
        });
    }
}
