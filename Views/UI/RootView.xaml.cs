using HandyControl.Controls;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using MFAWPF.Views.UI.Dialog;
using MFATask = MFAWPF.Helper.ValueType.MFATask;
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
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
        Loaded += (_, _) => LoadUI();
        Instances.TaskQueueView.InitializeData();
        OCRHelper.Initialize();
        LanguageHelper.ChangeLanguage(LanguageHelper.SupportedLanguages[ConfigurationHelper.GetValue(ConfigurationKeys.LangIndex, 0)]);

        ThemeHelper.UpdateThemeIndexChanged(ConfigurationHelper.GetValue(ConfigurationKeys.ThemeIndex, 0));
        StateChanged += (_, _) =>
        {
            if (ConfigurationHelper.GetValue(ConfigurationKeys.ShouldMinimizeToTray, false))
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
        ConfigurationHelper.SetValue(ConfigurationKeys.TaskItems, Instances.TaskQueueViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        MaaProcessor.Instance.SetCurrentTasker();
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
            Instances.ConnectingViewModel.CurrentController = (MaaInterface.Instance?.Controller?.FirstOrDefault()?.Type).ToMaaControllerTypes(Instances.ConnectingViewModel.CurrentController);
            Console.WriteLine((MaaInterface.Instance?.Controller?.FirstOrDefault()?.Type).ToMaaControllerTypes(Instances.ConnectingViewModel.CurrentController));
            if (!Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.NoAutoStart, bool.FalseString)) && ConfigurationHelper.GetValue(ConfigurationKeys.BeforeTask, "None").Contains("Startup", StringComparison.OrdinalIgnoreCase))
            {
                MaaProcessor.Instance.TaskQueue.Push(new MFATask
                {
                    Name = "启动前",
                    Type = MFATask.MFATaskType.MFA,
                    Action = async () => await WaitSoftware(),
                });
                Instances.TaskQueueView.Start(!ConfigurationHelper.GetValue(ConfigurationKeys.BeforeTask, "None").Contains("And", StringComparison.OrdinalIgnoreCase), checkUpdate: true);
            }
            else
            {
                Instances.ConnectingViewModel.TryReadAdbDeviceFromConfig();
                MaaProcessor.Instance.TestConnecting();
                VersionChecker.Check();
            }

            GlobalConfiguration.SetValue("NoAutoStart", bool.FalseString);

            ViewModel.LockController = (MaaInterface.Instance?.Controller?.Count ?? 0) < 2;
            Console.WriteLine((MaaInterface.Instance?.Controller?.Count ?? 0) < 2);
            ConfigurationHelper.SetValue(ConfigurationKeys.EnableEdit, ConfigurationHelper.GetValue(ConfigurationKeys.EnableEdit, false));
            if (!string.IsNullOrWhiteSpace(MaaInterface.Instance?.Message))
            {
                GrowlHelper.Info(MaaInterface.Instance.Message);
            }
        });
        TaskManager.RunTaskAsync(async () =>
        {
            await Task.Delay(1000);
            DispatcherHelper.RunOnMainThread(() =>
            {
                Instances.AnnouncementViewModel.CheckAnnouncement();
                if (ConfigurationHelper.GetValue(ConfigurationKeys.AutoMinimize, false))
                {
                    Collapse();
                }
                if (ConfigurationHelper.GetValue(ConfigurationKeys.AutoHide, false))
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
            + $"Interface Version: [data.version=v{resourceVersion.Replace("v", "")}] ";
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
        string brush = "Gray",
        string weight = "Regular",
        bool showTime = true)
        =>
            Instances.TaskQueueViewModel.AddLog(content, brush, weight, showTime);


    public static void AddLog(string content,
        Brush? brush = null,
        string weight = "Regular",
        bool showTime = true)
        =>
            Instances.TaskQueueViewModel.AddLog(content, brush, weight, showTime);

    public static void AddLogByKey(string key, Brush? brush = null, bool transformKey = true, params string[] formatArgsKeys)
        => Instances.TaskQueueViewModel.AddLogByKey(key, brush, transformKey, formatArgsKeys);
    public void RunScript(string str = "Prescript")
    {
        bool enable = str switch
        {
            "Prescript" => !string.IsNullOrWhiteSpace(ConfigurationHelper.GetValue(ConfigurationKeys.Prescript, string.Empty)),
            "Post-script" => !string.IsNullOrWhiteSpace(ConfigurationHelper.GetValue(ConfigurationKeys.Postscript, string.Empty)),
            _ => false,
        };
        if (!enable)
        {
            return;
        }

        Func<bool> func = str switch
        {
            "Prescript" => () => ExecuteScript(ConfigurationHelper.GetValue(ConfigurationKeys.Prescript, string.Empty)),
            "Post-script" => () => ExecuteScript(ConfigurationHelper.GetValue(ConfigurationKeys.Postscript, string.Empty)),
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

    public async Task WaitSoftware()
    {
        if (ConfigurationHelper.GetValue(ConfigurationKeys.BeforeTask, "None").Contains("Startup", StringComparison.OrdinalIgnoreCase))
        {
            await MaaProcessor.Instance.StartSoftware();
        }

        Instances.ConnectingViewModel.TryReadAdbDeviceFromConfig();
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
