using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Command;
using MFAWPF.Helper;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.RegularExpressions;
using DataSet = MFAWPF.Data.DataSet;


namespace MFAWPF.ViewModels;

public partial class MainViewModel : ViewModel
{
    public ObservableCollection<LogItemViewModel> LogItemViewModels { get; } = new();

    public void AddLog(string content,
        string color = "",
        string weight = "Regular",
        bool showTime = true)
    {

        var brush = new BrushConverter().ConvertFromString(color) as SolidColorBrush;
        brush ??= Brushes.Gray;
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(content, brush, weight, "HH':'mm':'ss",
                    showTime: showTime));
                LoggerService.LogInfo(content);
            });
        });
    }

    public void AddLog(string content,
        Brush? color = null,
        string weight = "Regular",
        bool showTime = true)
    {
        color ??= Brushes.Gray;
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(content, color, weight, "HH':'mm':'ss",
                    showTime: showTime));
                LoggerService.LogInfo(content);
            });
        });
    }

    public void AddLogByKey(string key, Brush? color = null, params string[] formatArgsKeys)
    {
        color ??= Brushes.Gray;
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(key, color, "Regular", true, "HH':'mm':'ss",
                    true, formatArgsKeys));

                var content = string.Empty;
                if (formatArgsKeys.Length == 0)
                    content = key.ToLocalization();
                else
                {
                    // 获取每个格式化参数的本地化字符串
                    var formatArgs = formatArgsKeys.Select(k => k.ToLocalizationFormatted()).ToArray();

                    // 使用本地化字符串更新内容
                    try
                    {
                        content = Regex.Unescape(
                            key.ToLocalizationFormatted(formatArgs.Cast<object>().ToArray()));
                    }
                    catch
                    {
                        content = key.ToLocalizationFormatted(formatArgs.Cast<object>().ToArray());
                    }
                }
                LoggerService.LogInfo(content);
            });
        });
    }

    [ObservableProperty] private ObservableCollection<DragItemViewModel> _taskItemViewModels = new();
    partial void OnTaskItemViewModelsChanged(ObservableCollection<DragItemViewModel>? oldValue, ObservableCollection<DragItemViewModel>? newValue)
    {
        if (newValue != null)
        {
            DataSet.SetData("TaskItems", newValue.ToList().Select(model => model.InterfaceItem));
        }
        else
        {
            DataSet.SetData("TaskItems", new ObservableCollection<DragItemViewModel>());
        }
    }

    public ObservableCollection<DragItemViewModel> TasksSource { get; private set; } =
        [];

    [ObservableProperty] private bool _idle = true;

    [ObservableProperty] private bool _notLock = true;

    [ObservableProperty] private bool _isRunning = false;

    public void SetIdle(bool value)
    {
        Idle = value;
    }

    public GongSolutions.Wpf.DragDrop.IDropTarget DropHandler { get; } = new DragDropHandler();

    [ObservableProperty] private bool _isAdb = true;

    [ObservableProperty] private bool _isConnected;

    [ObservableProperty] private bool _isUpdating;

    [ObservableProperty] private bool _isVisible = true;

    partial void OnIsVisibleChanged(bool value)
    {
        if (value)
        {
            Application.Current.MainWindow?.Show();
        }
        else
        {
            Application.Current.MainWindow?.Hide();
        }
    }

    public RelayCommand<FunctionEventArgs<object>> SwitchItemCmd => new Lazy<RelayCommand<FunctionEventArgs<object>>>(
        () =>
            new RelayCommand<FunctionEventArgs<object>>(SwitchItem)).Value;

    private void SwitchItem(FunctionEventArgs<object> info)
    {
        Growl.Info((info.Info as SideMenuItem)?.Header.ToString(), "InfoMessage");
    }

    public static string FormatFileSize(long size)
    {
        string unit;
        double value;
        if (size >= 1024L * 1024 * 1024 * 1024)
        {
            value = (double)size / (1024L * 1024 * 1024 * 1024);
            unit = "TB";
        }
        else if (size >= 1024 * 1024 * 1024)
        {
            value = (double)size / (1024 * 1024 * 1024);
            unit = "GB";
        }
        else if (size >= 1024 * 1024)
        {
            value = (double)size / (1024 * 1024);
            unit = "MB";
        }
        else if (size >= 1024)
        {
            value = (double)size / 1024;
            unit = "KB";
        }
        else
        {
            value = size;
            unit = "B";
        }

        return $"{value:F} {unit}";
    }

    public static string FormatDownloadSpeed(double speed)
    {
        string unit;
        double value = speed;
        if (value >= 1024L * 1024 * 1024 * 1024)
        {
            value /= 1024L * 1024 * 1024 * 1024;
            unit = "TB/s";
        }
        else if (value >= 1024L * 1024 * 1024)
        {
            value /= 1024L * 1024 * 1024;
            unit = "GB/s";
        }
        else if (value >= 1024 * 1024)
        {
            value /= 1024 * 1024;
            unit = "MB/s";
        }
        else if (value >= 1024)
        {
            value /= 1024;
            unit = "KB/s";
        }
        else
        {
            unit = "B/s";
        }

        return $"{value:F} {unit}";
    }
    public void OutputDownloadProgress(long value = 0, long maximum = 1, int len = 0, double ts = 1)
    {
        string sizeValueStr = FormatFileSize(value);
        string maxSizeValueStr = FormatFileSize(maximum);
        string speedValueStr = FormatDownloadSpeed(len / ts);

        string progressInfo = $"[{sizeValueStr}/{maxSizeValueStr}({100 * value / maximum}%) {speedValueStr}]";
        OutputDownloadProgress(progressInfo);
    }

    public void ClearDownloadProgress()
    {
        DispatcherHelper.RunOnMainThread(() =>
        {

            if (LogItemViewModels.Count > 0 && LogItemViewModels[0].IsDownloading)
            {
                LogItemViewModels.RemoveAt(0);
            }
        });
    }

    public void OutputDownloadProgress(string output, bool downloading = true)
    {

        DispatcherHelper.RunOnMainThread(() =>
        {
            var log = new LogItemViewModel(downloading ? "NewVersionFoundDescDownloading".ToLocalization() + "\n" + output : output, Application.Current.MainWindow.FindResource("DownloadLogBrush") as Brush,
                dateFormat: "HH':'mm':'ss")
            {
                IsDownloading = true,
            };
            if (LogItemViewModels.Count > 0 && LogItemViewModels[0].IsDownloading)
            {
                if (!string.IsNullOrEmpty(output))
                {
                    LogItemViewModels[0] = log;
                }
                else
                {
                    LogItemViewModels.RemoveAt(0);
                }
            }
            else if (!string.IsNullOrEmpty(output))
            {
                LogItemViewModels.Insert(0, log);
            }
        });
    }

    private string? _beforeTask = "None".ToLocalization();

    public string? BeforeTask
    {
        get
        {
            _beforeTask = BeforeTaskList[DataSet.GetData("AutoStartIndex", 0)].ResourceKey;
            return _beforeTask;
        }
        set => SetProperty(ref _beforeTask, value);
    }

    private string? _afterTask = "None".ToLocalization();

    public string? AfterTask
    {
        get
        {
            _afterTask = AfterTaskList[DataSet.GetData("AfterTaskIndex", 0)].ResourceKey;
            return _afterTask;
        }
        set => SetProperty(ref _afterTask, value);
    }

    [ObservableProperty] private List<LocalizationViewModel> _beforeTaskList =
    [
        new("None"),
        new("StartupSoftware"),
        new("StartupSoftwareAndScript"),
    ];


    [ObservableProperty] private List<LocalizationViewModel> _afterTaskList =
    [
        new("None"),
        new("CloseMFA"),
        new("CloseEmulator"),
        new("CloseEmulatorAndMFA"),
        new("ShutDown"),
        new("CloseEmulatorAndRestartMFA"),
        new("RestartPC"),
    ];

    private bool _shouldTip = true;
    private bool _isDebugMode;

    public bool IsDebugMode
    {
        set => SetProperty(ref _isDebugMode, value);

        get
        {

            _isDebugMode = MFAExtensions.IsDebugMode();
            if (_isDebugMode && _shouldTip)
            {
                MessageBoxHelper.Show("DebugModeWarning".ToLocalization(), "Tip".ToLocalization(), MessageBoxButton.OK, MessageBoxImage.Warning);
                _shouldTip = false;
            }
            return _isDebugMode;
        }
    }
}
