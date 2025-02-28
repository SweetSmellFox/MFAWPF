using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaaFramework.Binding;
using MFAWPF.Data;
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
            MaaProcessor.MaaFwConfig.DesktopWindow.Name = window.Name;
            MaaProcessor.MaaFwConfig.DesktopWindow.HWnd = window.Handle;
            MaaProcessor.Instance.SetCurrentTasker();
        }
        else if (value is AdbDeviceInfo device)
        {
            GrowlHelper.Info(string.Format(LocExtension.GetLocalizedValue<string>("EmulatorSelectionMessage"),
                device.Name));
            MaaProcessor.MaaFwConfig.AdbDevice.Name = device.Name;
            MaaProcessor.MaaFwConfig.AdbDevice.AdbPath = device.AdbPath;
            MaaProcessor.MaaFwConfig.AdbDevice.AdbSerial = device.AdbSerial;
            MaaProcessor.MaaFwConfig.AdbDevice.Config = device.Config;
            MaaProcessor.Instance.SetCurrentTasker();
            MFAConfiguration.SetConfiguration("AdbDevice", device);
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

    [ObservableProperty] private MaaControllerTypes _currentController = MFAConfiguration.GetConfiguration("CurrentController", MaaControllerTypes.Adb, MaaControllerTypes.None, new UniversalEnumConverter<MaaControllerTypes>());

    partial void OnCurrentControllerChanged(MaaControllerTypes value)
    {
        MFAConfiguration.SetConfiguration("CurrentController", value.ToString());
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
            SetConnected(true);
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
        var config = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);
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

        var configType = isAdb ? "AdbControl" : "Win32Control";
        HandleInputSettings(controller, configType);
        HandleScreenCapSettings(controller, configType);
    }

    private void HandleInputSettings(MaaInterface.MaaResourceController controller, string configPrefix)
    {
        var input = controller.Adb?.Input ?? controller.Win32?.Input;
        if (input == null) return;

        var mapping = configPrefix == "AdbControl"
            ? new Dictionary<long, int>
            {
                [1] = 2,
                [2] = 0,
                [4] = 1
            }
            : new Dictionary<long, int>
            {
                [1] = 1,
                [2] = 0
            };

        if (mapping.TryGetValue(input.Value, out var result))
        {
            MFAConfiguration.SetConfiguration($"{configPrefix}InputType", result);
        }
    }

    private void HandleScreenCapSettings(MaaInterface.MaaResourceController controller, string configPrefix)
    {
        var screenCap = controller.Adb?.ScreenCap ?? controller.Win32?.ScreenCap;
        if (screenCap == null) return;

        var mapping = configPrefix == "AdbControl"
            ? new Dictionary<long, int>
            {
                [1] = 4,
                [2] = 3,
                [4] = 1,
                [8] = 2,
                [16] = 5,
                [32] = 6,
                [64] = 7
            }
            : new Dictionary<long, int>
            {
                [1] = 2,
                [2] = 0,
                [4] = 1
            };

        if (mapping.TryGetValue(screenCap.Value, out var result))
        {
            MFAConfiguration.SetConfiguration($"{configPrefix}ScreenCapType", result);
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
            || !MFAConfiguration.GetConfiguration("RememberAdb", true)
            || MaaProcessor.MaaFwConfig.AdbDevice.AdbPath != "adb"
            || !MFAConfiguration.TryGetConfiguration("AdbDevice", out AdbDeviceInfo device,
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
