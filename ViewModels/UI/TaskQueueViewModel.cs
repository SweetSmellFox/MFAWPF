using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.ViewModels.Tool;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;


namespace MFAWPF.ViewModels.UI;

public partial class TaskQueueViewModel : ViewModel
{
    [ObservableProperty] private ObservableCollection<Tool.DragItemViewModel> _taskItemViewModels = new();

    partial void OnTaskItemViewModelsChanged(ObservableCollection<Tool.DragItemViewModel>? oldValue, ObservableCollection<Tool.DragItemViewModel> newValue)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.TaskItems, newValue.ToList().Select(model => model.InterfaceItem));
    }

    [ObservableProperty] private bool _connectSettingChecked = true;


    public ObservableCollection<Tool.DragItemViewModel> TasksSource { get; private set; } =
        [];

    public ObservableCollection<Tool.LocalizationViewModel> BeforeTaskList =>
    [
        new("None"),
        new("StartupSoftware"),
        new("StartupSoftwareAndScript"),
    ];


    public ObservableCollection<Tool.LocalizationViewModel> AfterTaskList =>
    [
        new("None"),
        new("CloseMFA"),
        new("CloseEmulator"),
        new("CloseEmulatorAndMFA"),
        new("ShutDown"),
        new("CloseEmulatorAndRestartMFA"),
        new("RestartPC"),
    ];


    [ObservableProperty] private string? _beforeTask = ConfigurationHelper.GetValue(ConfigurationKeys.BeforeTask, "None");

    partial void OnBeforeTaskChanged(string? value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.BeforeTask, value);
    }

    [ObservableProperty] private string? _afterTask = ConfigurationHelper.GetValue(ConfigurationKeys.AfterTask, "None");

    partial void OnAfterTaskChanged(string? value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AfterTask, value);
    }

    public GongSolutions.Wpf.DragDrop.IDropTarget DropHandler { get; } = new DragDropHandler();

    public ObservableCollection<LogItemViewModel> LogItemViewModels { get; } = new();

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
            var log = new LogItemViewModel(downloading ? "NewVersionFoundDescDownloading".ToLocalization() + "\n" + output : output, Instances.RootView.FindResource("DownloadLogBrush") as Brush,
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

    public void AddLog(string content,
        string color = "",
        string weight = "Regular",
        bool showTime = true)
    {

        var brush = new BrushConverter().ConvertFromString(color) as SolidColorBrush;
        brush ??= Brushes.Gray;
        Task.Run(() =>
        {
            DispatcherHelper.RunOnMainThread(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(content, brush, weight, "HH':'mm':'ss",
                    showTime: showTime));
                LoggerService.LogInfo(content);
            });
        });
    }

    public void AddLog(string content,
        Brush? brush = null,
        string weight = "Regular",
        bool showTime = true)
    {
        brush ??= Brushes.Gray;
        Task.Run(() =>
        {
            DispatcherHelper.RunOnMainThread(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(content, brush, weight, "HH':'mm':'ss",
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
            DispatcherHelper.RunOnMainThread(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(key, color, "Regular", true, "HH':'mm':'ss",
                    true, formatArgsKeys));

                var content = string.Empty;
                if (formatArgsKeys.Length == 0)
                    content = key.ToLocalization();
                else
                {
                    try
                    {
                        content = Regex.Unescape(
                            key.ToLocalizationFormatted(true, formatArgsKeys));
                    }
                    catch
                    {
                        content = key.ToLocalizationFormatted(true, formatArgsKeys);
                    }
                }
                LoggerService.LogInfo(content);
            });
        });
    }

    public void AddLogByKey(string key, Brush? color = null, bool transformKey = true, params string[] formatArgsKeys)
    {
        color ??= Brushes.Gray;
        Task.Run(() =>
        {
            DispatcherHelper.RunOnMainThread(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(key, color, "Regular", true, "HH':'mm':'ss",
                    true, transformKey, formatArgsKeys));

                var content = string.Empty;
                if (formatArgsKeys.Length == 0)
                    content = key.ToLocalization();
                else
                {
                    try
                    {
                        content = Regex.Unescape(
                            key.ToLocalizationFormatted(false, formatArgsKeys));
                    }
                    catch
                    {
                        content = key.ToLocalizationFormatted(false, formatArgsKeys);
                    }
                }
                LoggerService.LogInfo(content);
            });
        });
    }
}
