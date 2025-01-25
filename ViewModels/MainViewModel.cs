using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Command;
using MFAWPF.Utils;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using DataSet = MFAWPF.Data.DataSet;


namespace MFAWPF.ViewModels;

public class MainViewModel : ObservableObject
{
    public ObservableCollection<LogItemViewModel> LogItemViewModels { get; } = new();
    private bool SetCurrentProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanging(propertyName);
        field = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void AddLog(string content,
        string? color = "",
        string weight = "Regular",
        bool showTime = true)
    {

        var brush = new BrushConverter().ConvertFromString(color ?? "Gray") as SolidColorBrush;
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

    public void AddLogByKey(string key, Brush? color = null, params string[]? formatArgsKeys)
    {
        color ??= Brushes.Gray;
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(key, color, "Regular", true, "HH':'mm':'ss",
                    true, formatArgsKeys));

                string Content = string.Empty;
                if (formatArgsKeys == null || formatArgsKeys.Length == 0)
                    Content = key.GetLocalizationString();
                else
                {
                    // 获取每个格式化参数的本地化字符串
                    var formatArgs = formatArgsKeys.Select(key => key.GetLocalizedFormattedString()).ToArray();

                    // 使用本地化字符串更新内容
                    try
                    {
                        Content = Regex.Unescape(
                            key.GetLocalizedFormattedString(formatArgs.Cast<object>().ToArray()));
                    }
                    catch
                    {
                        Content = key.GetLocalizedFormattedString(formatArgs.Cast<object>().ToArray());
                    }
                }
                LoggerService.LogInfo(Content);
            });
        });
    }

    public ObservableCollection<DragItemViewModel> TaskItemViewModels { get; set; } =
        new();

    public ObservableCollection<DragItemViewModel> TasksSource { get; private set; } =
        new();

    private bool _idle = true;

    /// <summary>
    /// Gets or sets a value indicating whether it is idle.
    /// </summary>
    public bool Idle
    {
        get => _idle;
        set => SetProperty(ref _idle, value);
    }

    private bool _notLock = true;

    public bool NotLock
    {
        get => _notLock;
        set => SetProperty(ref _notLock, value);
    }

    private bool _isRunning = false;

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public void SetIdle(bool value)
    {
        Idle = value;
    }

    public GongSolutions.Wpf.DragDrop.IDropTarget DropHandler { get; } = new DragDropHandler();


    private bool _isAdb = true;

    public bool IsAdb
    {
        get => _isAdb;
        set => SetProperty(ref _isAdb, value);
    }

    private bool _isConnected;

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private bool _isUpdating;

    public bool IsUpdating
    {
        get => _isUpdating;
        set => SetProperty(ref _isUpdating, value);
    }

    private bool _isVisible = true;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value)
            {
                Application.Current.MainWindow.Show();
            }
            else
            {
                Application.Current.MainWindow.Hide();
            }
            SetProperty(ref _isVisible, value);
        }
    }

    public RelayCommand<FunctionEventArgs<object>> SwitchItemCmd => new Lazy<RelayCommand<FunctionEventArgs<object>>>(
        () =>
            new RelayCommand<FunctionEventArgs<object>>(SwitchItem)).Value;

    private void SwitchItem(FunctionEventArgs<object> info)
    {
        Growl.Info((info.Info as SideMenuItem)?.Header.ToString(), "InfoMessage");
    }

    private enum NotifyType
    {
        None,
        SelectedIndex,
        ScrollOffset,
    }

    private NotifyType _notifySource = NotifyType.None;

    private System.Timers.Timer _resetNotifyTimer;

    private void ResetNotifySource()
    {
        if (_resetNotifyTimer != null)
        {
            _resetNotifyTimer.Stop();
            _resetNotifyTimer.Close();
        }

        _resetNotifyTimer = new(20);
        _resetNotifyTimer.Elapsed += (_, _) =>
        {
            _notifySource = NotifyType.None;
        };
        _resetNotifyTimer.AutoReset = false;
        _resetNotifyTimer.Enabled = true;
        _resetNotifyTimer.Start();
    }

    /// <summary>
    /// Gets or sets the height of scroll viewport.
    /// </summary>
    public double ScrollViewportHeight { get; set; }

    /// <summary>
    /// Gets or sets the extent height of scroll.
    /// </summary>
    public double ScrollExtentHeight { get; set; }

    public List<double> DividerVerticalOffsetList { get; set; } = new();

    private int _selectedIndex;

    /// <summary>
    /// Gets or sets the index selected.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            switch (_notifySource)
            {
                case NotifyType.None:
                    _notifySource = NotifyType.SelectedIndex;
                    SetProperty(ref _selectedIndex, value);

                    if (DividerVerticalOffsetList?.Count > 0 && value < DividerVerticalOffsetList.Count)
                    {
                        ScrollOffset = DividerVerticalOffsetList[value];
                    }

                    ResetNotifySource();
                    break;

                case NotifyType.ScrollOffset:
                    SetProperty(ref _selectedIndex, value);
                    break;

                case NotifyType.SelectedIndex:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private double _scrollOffset;

    /// <summary>
    /// Gets or sets the scroll offset.
    /// </summary>
    public double ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            switch (_notifySource)
            {
                case NotifyType.None:
                    _notifySource = NotifyType.ScrollOffset;
                    SetProperty(ref _scrollOffset, value);

                    // 设置 ListBox SelectedIndex 为当前 ScrollOffset 索引
                    if (DividerVerticalOffsetList?.Count > 0)
                    {
                        // 滚动条滚动到底部，返回最后一个 Divider 索引
                        if (value + ScrollViewportHeight >= ScrollExtentHeight)
                        {
                            SelectedIndex = DividerVerticalOffsetList.Count - 1;
                            ResetNotifySource();
                            break;
                        }

                        // 根据出当前 ScrollOffset 选出最后一个在可视范围的 Divider 索引
                        var dividerSelect = DividerVerticalOffsetList.Select((n, i) => (
                            dividerAppeared: value >= n,
                            index: i));

                        var index = dividerSelect.LastOrDefault(n => n.dividerAppeared).index;
                        SelectedIndex = index;
                    }

                    ResetNotifySource();
                    break;

                case NotifyType.SelectedIndex:
                    SetProperty(ref _scrollOffset, value);
                    break;

                case NotifyType.ScrollOffset:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private List<SettingViewModel> _listTitle =
    [
        new("SwitchConfiguration"),
        new("UiSettings"),
        new("ConnectionSettings"),
        new("PerformanceSettings"),
        new("StartupSettings"),
        new("ExternalNotificationSettings"),
        new("RunningSettings"),
        new("SoftwareUpdate"),
        new("About"),
    ];

    /// <summary>
    /// Gets or sets the list title.
    /// </summary>
    public List<SettingViewModel> ListTitle
    {
        get => _listTitle;
        set => SetProperty(ref _listTitle, value);
    }

    private int _languageIndex = 0;

    public int LanguageIndex
    {
        set
        {
            SetProperty(ref _languageIndex, value);
        }
        get
        {
            if (_languageIndex != DataSet.GetData("LangIndex", 0))
                _languageIndex = DataSet.GetData("LangIndex", 0);
            return _languageIndex;
        }
    }


    private string _beforeTask = "None".GetLocalizationString();

    public string BeforeTask
    {
        get
        {
            if (_beforeTask != BeforeTaskList[DataSet.GetData("AutoStartIndex", 0)].ResourceKey)
                _beforeTask = BeforeTaskList[DataSet.GetData("AutoStartIndex", 0)].ResourceKey;
            return _beforeTask;
        }
        set => SetProperty(ref _beforeTask, value);
    }

    private string _afterTask = "None".GetLocalizationString();

    public string AfterTask
    {
        get
        {
            if (_afterTask != AfterTaskList[DataSet.GetData("AfterTaskIndex", 0)].ResourceKey)
                _afterTask = AfterTaskList[DataSet.GetData("AfterTaskIndex", 0)].ResourceKey;
            return _afterTask;
        }
        set => SetProperty(ref _afterTask, value);
    }

    private List<SettingViewModel> _beforeTaskList =
    [
        new("None"),
        new("StartupSoftware"),
        new("StartupSoftwareAndScript"),
    ];


    public List<SettingViewModel> BeforeTaskList
    {
        get => _beforeTaskList;
        set => SetProperty(ref _beforeTaskList, value);
    }

    private List<SettingViewModel> _afterTaskList =
    [
        new("None"),
        new("CloseMFA"),
        new("CloseEmulator"),
        new("CloseEmulatorAndMFA"),
        new("ShutDown"),
        new("CloseEmulatorAndRestartMFA"),
        new("RestartPC"),
    ];

    public List<SettingViewModel> AfterTaskList
    {
        get => _afterTaskList;
        set => SetProperty(ref _listTitle, value);
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
        Growls.Process(() =>
        {

            if (LogItemViewModels.Count > 0 && LogItemViewModels[0].IsDownloading)
            {
                LogItemViewModels.RemoveAt(0);
            }
        });
    }

    public void OutputDownloadProgress(string output, bool downloading = true)
    {
        if (LogItemViewModels == null)
        {
            return;
        }

        Growls.Process(() =>
        {
            var log = new LogItemViewModel(downloading ? "NewVersionFoundDescDownloading".GetLocalizationString() + "\n" + output : output, Application.Current.MainWindow.FindResource("DownloadLogBrush") as Brush,
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
    private int _downloadSourceIndex;

    public int DownloadSourceIndex
    {
        set
        {
            DataSet.SetData("DownloadSourceIndex", value);
            SetCurrentProperty(ref _downloadSourceIndex, value);
        }
        get
        {
            if (_downloadSourceIndex != DataSet.GetData("DownloadSourceIndex", 0))
                _downloadSourceIndex = DataSet.GetData("DownloadSourceIndex", 0);
            if (string.IsNullOrWhiteSpace(MaaInterface.Instance.RID))
                _downloadSourceIndex = 0;
            return _downloadSourceIndex;
        }
    }


    public List<SettingViewModel> DownloadSourceList
    {
        get => _downloadSourceList;
        set => SetProperty(ref _downloadSourceList, value);
    }

    private List<SettingViewModel> _downloadSourceList =
    [
        new("GitHub"),
    ];

    private bool _retryOnDisconnected = DataSet.GetData("RetryOnDisconnected", false);

    /// <summary>
    /// Gets or sets a value indicating whether to retry task after ADB disconnected.
    /// </summary>
    public bool RetryOnDisconnected
    {
        get => _retryOnDisconnected;
        set
        {
            SetProperty(ref _retryOnDisconnected, value);
            DataSet.SetData("RetryOnDisconnected", value);
        }
    }

    private bool _allowAdbRestart = DataSet.GetData("AllowAdbRestart", true);

    /// <summary>
    /// Gets or sets a value indicating whether to retry task after ADB disconnected.
    /// </summary>
    public bool AllowAdbRestart
    {
        get => _allowAdbRestart;
        set
        {
            SetProperty(ref _allowAdbRestart, value);
            DataSet.SetData("AllowAdbRestart", value);
        }
    }

    private bool _allowAdbHardRestart = DataSet.GetData("AllowAdbHardRestart", true);

    /// <summary>
    /// Gets or sets a value indicating whether to allow for killing ADB process.
    /// </summary>
    public bool AllowAdbHardRestart
    {
        get => _allowAdbHardRestart;
        set
        {
            SetProperty(ref _allowAdbHardRestart, value);
            DataSet.SetData("AllowAdbHardRestart", value);
        }
    }

    private bool _shouldTip = true;
    private bool _isDebugMode;

    public bool IsDebugMode
    {
        set => SetProperty(ref _isDebugMode, value);

        get
        {
            if (_isDebugMode != MFAExtensions.IsDebugMode())
                _isDebugMode = MFAExtensions.IsDebugMode();
            if (_isDebugMode && _shouldTip)
            {
                MessageBoxHelper.Show("DebugModeWarning".GetLocalizationString(), "Tip".GetLocalizationString(), MessageBoxButton.OK, MessageBoxImage.Warning);
                _shouldTip = false;
            }
            return _isDebugMode;
        }
    }

    public static readonly List<SettingViewModel> ExternalNotificationProviders =
    [
        new("DingTalk"),
    ];

    public static List<SettingViewModel> ExternalNotificationProvidersShow => ExternalNotificationProviders;

    private static object[] _enabledExternalNotificationProviders =
        ExternalNotificationProviders.Where(s => DataSet.GetData("ExternalNotificationEnabled", string.Empty).Split(',').Contains(s.ResourceKey))
            .Distinct()
            .ToArray();

    public object[] EnabledExternalNotificationProviders
    {
        get
        {
            return _enabledExternalNotificationProviders;
        }
        set
        {
            try
            {
                var settingViewModels = value.Cast<SettingViewModel>();
                SetProperty(ref _enabledExternalNotificationProviders, value);
                var validProviders = settingViewModels
                    .Where(provider => ExternalNotificationProviders.ContainsKey(provider.ToString() ?? string.Empty))
                    .Select(provider => provider.ToString())
                    .Distinct();

                var config = string.Join(",", validProviders);
                DataSet.SetData("ExternalNotificationEnabled", config);
                UpdateExternalNotificationProvider();
                EnabledExternalNotificationProviderCount = _enabledExternalNotificationProviders.Length;
            }
            catch (Exception e)
            {
                LoggerService.LogError(e);
            }
        }
    }

    private int _enabledExternalNotificationProviderCount = _enabledExternalNotificationProviders.Length;

    public int EnabledExternalNotificationProviderCount
    {
        get => _enabledExternalNotificationProviderCount;
        set => SetProperty(ref _enabledExternalNotificationProviderCount, value);
    }

    public string[] EnabledExternalNotificationProviderList => EnabledExternalNotificationProviders
        .Select(s => s.ToString() ?? string.Empty)
        .ToArray();


    private bool _dingTalkEnabled = false;

    public bool DingTalkEnabled
    {
        get => _dingTalkEnabled;
        set => SetProperty(ref _dingTalkEnabled, value);
    }

    private string _dingTalkToken = SimpleEncryptionHelper.Decrypt(DataSet.GetData("ExternalNotificationDingTalkToken", string.Empty));

    public string DingTalkToken
    {
        get => _dingTalkToken;
        set
        {
            SetProperty(ref _dingTalkToken, value);
            value = SimpleEncryptionHelper.Encrypt(value);
            DataSet.SetData("ExternalNotificationDingTalkToken", value);
        }
    }

    private string _dingTalkSecret = SimpleEncryptionHelper.Decrypt(DataSet.GetData("ExternalNotificationDingTalkSecret", string.Empty));

    public string DingTalkSecret
    {
        get => _dingTalkSecret;
        set
        {
            SetProperty(ref _dingTalkSecret, value);
            value = SimpleEncryptionHelper.Encrypt(value);
            DataSet.SetData("ExternalNotificationDingTalkSecret", value);
        }
    }

    public void UpdateExternalNotificationProvider()
    {
        DingTalkEnabled = _enabledExternalNotificationProviders.Contains("DingTalk");
    }
}
