using HandyControl.Controls;
using HandyControl.Data;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFLocalizeExtension.Extensions;
using ComboBox = HandyControl.Controls.ComboBox;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TextBox = HandyControl.Controls.TextBox;
using MFAWPF.Views.UI.Dialog;
using Notifications.Wpf.Annotations;
using RootViewModel = MFAWPF.ViewModels.UI.RootViewModel;

namespace MFAWPF.Views.UI;

public partial class RootView
{
    public RootViewModel ViewModel { get; set; }

    public static string Version =>
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";


    public RootView(RootViewModel viewModel)
    {
        // Instances.RootView = this;
        LanguageHelper.Initialize();
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = this;
        Loaded += (_, _) => LoadUI();
        Instances.TaskQueueView.InitializeData();
        OCRHelper.Initialize();
        LanguageHelper.ChangeLanguage(LanguageHelper.SupportedLanguages[MFAConfiguration.GetConfiguration("LangIndex", 0)]);

        ThemeHelper.UpdateThemeIndexChanged(MFAConfiguration.GetConfiguration("ThemeIndex", 0));
        StateChanged += (_, _) =>
        {
            if (MFAConfiguration.GetConfiguration("ShouldMinimizeToTray", false))
            {
                ChangeVisibility(WindowState != WindowState.Minimized);
            }
        };
        MaaProcessor.Instance.TaskStackChanged += OnTaskStackChanged;
    }


    private void OnTaskStackChanged(object sender, EventArgs e)
    {
        ToggleTaskButtonsVisibility(isRunning: MaaProcessor.Instance.TaskQueue.Count > 0);
    }

    private void ChangeVisibility(bool visible)
    {
        Visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    public void ToggleTaskButtonsVisibility(bool isRunning)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            if (ViewModel is not null)
                ViewModel.IsRunning = isRunning;
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = !ConfirmExit();
        MFAConfiguration.SetConfiguration("TaskItems", Instances.TaskQueueViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        base.OnClosed(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Application.Current.Shutdown();
    }


    public void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void Collapse()
    {
        WindowState = WindowState.Minimized;
    }

    public void SwitchWindowState()
    {
        if (WindowState == WindowState.Minimized)
        {
            ShowWindow();
        }
        else
        {
            Collapse();
        }
    }


    public void RestartMFA()
    {
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        DispatcherHelper.RunOnMainThread(Application.Current.Shutdown);
    }

    private static EditTaskDialog? _taskDialog;

    public static EditTaskDialog? TaskDialog
    {
        get
        {

            _taskDialog ??= new EditTaskDialog();
            return _taskDialog;
        }
        set => _taskDialog = value;
    }


    public void LoadUI()
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            InitializationSettings();
            Instances.ConnectingView.ConnectionTabControl.SelectedIndex = MaaInterface.Instance?.DefaultController == "win32" ? 1 : 0;
            if (!Convert.ToBoolean(GlobalConfiguration.GetConfiguration("NoAutoStart", bool.FalseString)) && MFAConfiguration.GetConfiguration("BeforeTask", "None").Contains("Startup", StringComparison.OrdinalIgnoreCase))
            {
                MaaProcessor.Instance.TaskQueue.Push(new MFATask
                {
                    Name = "启动前",
                    Type = MFATask.MFATaskType.MFA,
                    Action = WaitSoftware,
                });
                Instances.TaskQueueView.Start(!MFAConfiguration.GetConfiguration("BeforeTask", "None").Contains("And", StringComparison.OrdinalIgnoreCase), checkUpdate: true);
            }
            else
            {
                if (ViewModel.IsAdb && MFAConfiguration.GetConfiguration("RememberAdb", true) && "adb".Equals(MaaProcessor.MaaFwConfig.AdbDevice.AdbPath) && MFAConfiguration.TryGetData<JObject>("AdbDevice", out var jObject))
                {
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new AdbInputMethodsConverter());
                    settings.Converters.Add(new AdbScreencapMethodsConverter());

                    var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
                    if (device != null)
                    {
                        DispatcherHelper.RunOnMainThread(() =>
                        {
                            Instances.ConnectingView.DeviceComboBox.ItemsSource = new List<AdbDeviceInfo>
                            {
                                device
                            };
                            Instances.ConnectingView.DeviceComboBox.SelectedIndex = 0;
                            ViewModel.SetConnected(true);
                        });
                    }
                }
                VersionChecker.Check();
            }

            GlobalConfiguration.SetConfiguration("NoAutoStart", bool.FalseString);

            ViewModel.NotLock = MaaInterface.Instance?.LockController != true;
            // TaskQueueView.ConnectSettingButton.IsChecked = true;
            MFAConfiguration.SetConfiguration("EnableEdit", MFAConfiguration.GetConfiguration("EnableEdit", false));
            if (!string.IsNullOrWhiteSpace(MaaInterface.Instance?.Message))
            {
                Growl.Info(MaaInterface.Instance.Message);
            }

        });
        TaskManager.RunTaskAsync(async () =>
        {
            await Task.Delay(1000);
            DispatcherHelper.RunOnMainThread(() =>
            {
                Instances.AnnouncementViewModel.CheckAnnouncement();
                if (MFAConfiguration.GetConfiguration("AutoMinimize", false))
                {
                    Collapse();
                }
                if (MFAConfiguration.GetConfiguration("AutoHide", false))
                {
                    Hide();
                }
            });
        });
    }

    public static void AppendVersionLog(string? resourceVersion)
    {
        if (resourceVersion is null)
        {
            return;
        }
        string debugFolderPath = Path.Combine(AppContext.BaseDirectory, "debug");
        if (!Directory.Exists(debugFolderPath))
        {
            Directory.CreateDirectory(debugFolderPath);
        }

        string logFilePath = Path.Combine(debugFolderPath, "maa.log");
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formattedLogMessage =
            $"[{timeStamp}][INF][Px14600][Tx16498][Parser.cpp][L56][MaaNS::ProjectInterfaceNS::Parser::parse_interface] ";
        var logMessage = $"MFAWPF Version: [mfa.version={Version}] "
            + (resourceVersion is null
                ? ""
                : $"Interface Version: [data.version=v{resourceVersion.Replace("v", "")}] ");
        LoggerService.LogInfo(logMessage);

        try
        {
            File.AppendAllText(logFilePath, formattedLogMessage + logMessage);
        }
        catch (Exception)
        {
            Console.WriteLine("尝试写入失败！");
        }
    }

    private void ToggleWindowTopMost(object sender, RoutedPropertyChangedEventArgs<bool> e)
    {
        Topmost = e.NewValue;
    }

    public static void AddLogByColor(string content,
        string color = "Gray",
        string weight = "Regular",
        bool showTime = true)
        =>
            Instances.TaskQueueViewModel.AddLog(content, color, weight, showTime);


    public static void AddLog(string content,
        Brush? color = null,
        string weight = "Regular",
        bool showTime = true)
        =>
            Instances.TaskQueueViewModel.AddLog(content, color, weight, showTime);


    public static void AddLogByKey(string key, Brush? color = null, params string[] formatArgsKeys)
        =>
            Instances.TaskQueueViewModel.AddLogByKey(key, color, formatArgsKeys);


    public void RunScript(string str = "Prescript")
    {
        bool enable = str switch
        {
            "Prescript" => !string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("Prescript", string.Empty)),
            "Post-script" => !string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("Post-script", string.Empty)),
            _ => false,
        };
        if (!enable)
        {
            return;
        }

        Func<bool> func = str switch
        {
            "Prescript" => () => ExecuteScript(MFAConfiguration.GetConfiguration("Prescript", string.Empty)),
            "Post-script" => () => ExecuteScript(MFAConfiguration.GetConfiguration("Post-script", string.Empty)),
            _ => () => false,
        };

        if (!func())
        {
            LoggerService.LogError($"Failed to execute the {str}.");
        }
    }

    private static bool ExecuteScript(string scriptPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return false;
            }

            string fileName;
            string arguments;

            if (scriptPath.StartsWith('\"'))
            {
                var parts = scriptPath.Split("\"", 3);
                fileName = parts[1];
                arguments = parts.Length > 2 ? parts[2] : string.Empty;
            }
            else
            {
                fileName = scriptPath;
                arguments = string.Empty;
            }

            bool createNoWindow = arguments.Contains("-noWindow");
            bool minimized = arguments.Contains("-minimized");

            if (createNoWindow)
            {
                arguments = arguments.Replace("-noWindow", string.Empty).Trim();
            }

            if (minimized)
            {
                arguments = arguments.Replace("-minimized", string.Empty).Trim();
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WindowStyle = minimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal,
                    CreateNoWindow = createNoWindow,
                    UseShellExecute = !createNoWindow,
                },
            };
            process.Start();
            process.WaitForExit();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void InitializationSettings()
    {

    }

    public async void WaitSoftware()
    {
        if (MFAConfiguration.GetConfiguration("BeforeTask", "None").Contains("Startup", StringComparison.OrdinalIgnoreCase))
        {
            MaaProcessor.Instance.StartSoftware();
        }

        if ((ViewModel?.IsAdb).IsTrue() && MFAConfiguration.GetConfiguration("RememberAdb", true) && "adb".Equals(MaaProcessor.MaaFwConfig.AdbDevice.AdbPath) && MFAConfiguration.TryGetData<JObject>("AdbDevice", out var jObject))
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new AdbInputMethodsConverter());
            settings.Converters.Add(new AdbScreencapMethodsConverter());

            var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
            if (device != null)
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    Instances.ConnectingView.DeviceComboBox.ItemsSource = new List<AdbDeviceInfo>
                    {
                        device
                    };
                    Instances.ConnectingView.DeviceComboBox.SelectedIndex = 0;
                    ViewModel.SetConnected(true);
                });
            }
        }
        else
            DispatcherHelper.RunOnMainThread(Instances.ConnectingView.AutoDetectDevice);
    }

    public bool IsConnected()
    {
        return ViewModel.IsConnected;
    }

    public bool ConfirmExit()
    {
        if (!ViewModel.IsRunning)
            return true;
        var result = MessageBoxHelper.Show("ConfirmExitText".ToLocalization(),
            "ConfirmExitTitle".ToLocalization(), buttons: MessageBoxButton.YesNo, icon: MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public void ClearTasks(Action? action = null)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            Instances.TaskQueueViewModel.TaskItemViewModels = new();
            action?.Invoke();
        });
    }
}
