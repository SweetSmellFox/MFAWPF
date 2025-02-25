using HandyControl.Controls;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Views.UI.Dialog;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using WPFLocalizeExtension.Extensions;


namespace MFAWPF.Views.UI;

public partial class ConnectingView
{
    public ConnectingView()
    {
        InitializeComponent();
        ConnectionTabControl.SelectionChanged += ConnectionTabControlOnSelectionChanged;
    }

    private void DeviceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => DeviceComboBox_OnSelectionChanged();

    public void DeviceComboBox_OnSelectionChanged()
    {
        if (DeviceComboBox.SelectedItem is DesktopWindowInfo window)
        {
            GrowlHelper.Info(string.Format(LocExtension.GetLocalizedValue<string>("WindowSelectionMessage"),
                window.Name));
            MaaProcessor.MaaFwConfig.DesktopWindow.Name = window.Name;
            MaaProcessor.MaaFwConfig.DesktopWindow.HWnd = window.Handle;
            MaaProcessor.Instance.SetCurrentTasker();
        }
        else if (DeviceComboBox.SelectedItem is AdbDeviceInfo device)
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
    private void Refresh(object sender, RoutedEventArgs e) => AutoDetectDevice();

    private void CustomAdb(object sender, RoutedEventArgs e)
        => CustomAdb();

    private async void ConnectionTabControlOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {


        Instances.RootViewModel.IsAdb = AdbTab.IsSelected;
        ButtonCustom.Visibility = AdbTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;

        AutoDetectDevice();

        MaaProcessor.Instance.SetCurrentTasker();

    }


    public void CustomAdb()
    {
        var deviceInfo =
            DeviceComboBox.Items.Count > 0 && DeviceComboBox.SelectedItem is AdbDeviceInfo device
                ? device
                : null;
        var dialog = new AdbEditorDialog(deviceInfo);
        if (dialog.ShowDialog().IsTrue())
        {
            DeviceComboBox.ItemsSource = new List<AdbDeviceInfo>
            {
                dialog.Output
            };
            DeviceComboBox.SelectedIndex = 0;
            Instances.RootViewModel.SetConnected(true);
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
            using JsonDocument doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("extras", out JsonElement extras) && extras.TryGetProperty("mumu", out JsonElement mumu) && mumu.TryGetProperty("index", out JsonElement indexElement))
            {
                index = indexElement.GetInt32();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析 Config 时出错: {ex.Message}");
            LoggerService.LogError(ex);
        }

        index = 0;
        return false;
    }

    public void AutoDetectDevice()
    {
        try
        {
            GrowlHelper.Info(Instances.RootViewModel.IsAdb
                ? LocExtension.GetLocalizedValue<string>("EmulatorDetectionStarted")
                : LocExtension.GetLocalizedValue<string>("WindowDetectionStarted"));
            Instances.RootViewModel.SetConnected(false);
            if (Instances.RootViewModel.IsAdb)
            {
                var devices = MaaProcessor.MaaToolkit.AdbDevice.Find();
                DispatcherHelper.RunOnMainThread(() => Instances.ConnectingView.DeviceComboBox.ItemsSource = devices);
                Instances.RootViewModel.SetConnected(devices.Count > 0);
                var emulatorConfig = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);
                var resultIndex = 0;
                if (!string.IsNullOrWhiteSpace(emulatorConfig))
                {
                    var extractedNumber = ExtractNumberFromEmulatorConfig(emulatorConfig);

                    foreach (var device in devices)
                    {
                        if (TryGetIndexFromConfig(device.Config, out int index))
                        {
                            if (index == extractedNumber)
                            {
                                resultIndex = devices.IndexOf(device);
                                break;
                            }
                        }
                        else resultIndex = 0;
                    }
                }
                else
                    resultIndex = 0;
                DispatcherHelper.RunOnMainThread(() => Instances.ConnectingView.DeviceComboBox.SelectedIndex = resultIndex);
                if (MaaInterface.Instance?.Controller != null)
                {
                    if (MaaInterface.Instance.Controller.Any(controller => controller.Type != null && controller.Type.ToLower().Equals("adb")))
                    {
                        var adbController = MaaInterface.Instance.Controller.FirstOrDefault(controller => controller.Type != null && controller.Type.ToLower().Equals("adb"));
                        if (adbController?.Adb != null)
                        {
                            if (adbController.Adb.Input != null)
                            {
                                int result = 3;
                                switch (adbController.Adb.Input)
                                {
                                    case 1:
                                        result = 2;
                                        break;
                                    case 2:
                                        result = 0;
                                        break;
                                    case 4:
                                        result = 1;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("AdbControlInputType", result);
                            }
                            if (adbController.Adb.ScreenCap != null)
                            {
                                int result = 0;
                                switch (adbController.Adb.ScreenCap)
                                {
                                    case 1:
                                        result = 4;
                                        break;
                                    case 2:
                                        result = 3;
                                        break;
                                    case 4:
                                        result = 1;
                                        break;
                                    case 8:
                                        result = 2;
                                        break;
                                    case 16:
                                        result = 5;
                                        break;
                                    case 32:
                                        result = 6;
                                        break;
                                    case 64:
                                        result = 7;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("AdbControlScreenCapType", result);
                            }
                        }
                    }
                }
            }
            else
            {
                var windows = MaaProcessor.MaaToolkit.Desktop.Window.Find().ToList();
                DispatcherHelper.RunOnMainThread(() => Instances.ConnectingView.DeviceComboBox.ItemsSource = windows);
                Instances.RootViewModel.SetConnected(windows.Count > 0);
                var resultIndex = windows.Count > 0
                    ? windows.ToList().FindIndex(win => !string.IsNullOrWhiteSpace(win.Name))
                    : 0;
                if (MaaInterface.Instance?.Controller != null)
                {
                    if (MaaInterface.Instance.Controller.Any(controller => controller.Type != null && controller.Type.ToLower().Equals("win32")))
                    {
                        var win32Controller = MaaInterface.Instance.Controller.FirstOrDefault(controller => controller.Type != null && controller.Type.ToLower().Equals("win32"));
                        if (win32Controller?.Win32 != null)
                        {
                            var filteredWindows = windows.Where(win => !string.IsNullOrWhiteSpace(win.Name) || !string.IsNullOrWhiteSpace(win.ClassName)).ToList();

                            if (!string.IsNullOrWhiteSpace(win32Controller.Win32.WindowRegex))
                            {
                                var windowRegex = new Regex(win32Controller.Win32.WindowRegex);
                                filteredWindows = filteredWindows.Where(win => windowRegex.IsMatch(win.Name)).ToList();
                                resultIndex = filteredWindows.Count > 0
                                    ? windows.FindIndex(win => win.Name.Equals(filteredWindows.First().Name))
                                    : 0;
                            }

                            if (!string.IsNullOrWhiteSpace(win32Controller.Win32.ClassRegex))
                            {
                                var classRegex = new Regex(win32Controller.Win32.ClassRegex);
                                var filteredWindowsByClass = filteredWindows.Where(win => classRegex.IsMatch(win.ClassName)).ToList();
                                resultIndex = filteredWindowsByClass.Count > 0
                                    ? windows.FindIndex(win => win.ClassName.Equals(filteredWindowsByClass.First().ClassName))
                                    : 0;
                            }

                            if (win32Controller.Win32.Input != null)
                            {
                                int result = 0;
                                switch (win32Controller.Win32.Input)
                                {
                                    case 1:
                                        result = 0;
                                        break;
                                    case 2:
                                        result = 1;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("Win32ControlInputType", result);
                            }
                            if (win32Controller.Win32.ScreenCap != null)
                            {
                                int result = 0;
                                switch (win32Controller.Win32.ScreenCap)
                                {
                                    case 1:
                                        result = 2;
                                        break;
                                    case 2:
                                        result = 0;
                                        break;
                                    case 4:
                                        result = 1;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("Win32ControlScreenCapType", result);
                            }
                        }
                    }
                }
                DispatcherHelper.RunOnMainThread(() => Instances.ConnectingView.DeviceComboBox.SelectedIndex = resultIndex);
            }

            if (!Instances.RootViewModel.IsConnected)
            {
                Growl.Info(Instances.RootViewModel.IsAdb
                    ? LocExtension.GetLocalizedValue<string>("NoEmulatorFound")
                    : LocExtension.GetLocalizedValue<string>("NoWindowFound"));
            }
        }
        catch (Exception ex)
        {
            GrowlHelper.Warning(string.Format(LocExtension.GetLocalizedValue<string>("TaskStackError"),
                Instances.RootViewModel.IsAdb ? "Emulator".ToLocalization() : "Window".ToLocalization(),
                ex.Message));
            Instances.RootViewModel.SetConnected(false);
            LoggerService.LogError(ex);
            Console.WriteLine(ex);
        }
    }
}
