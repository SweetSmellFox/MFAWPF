using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Themes;
using HandyControl.Tools.Extension;
using MFAWPF.Data;
using MFAWPF.Helper;
using MFAWPF.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MFAWPF.ViewModels;

public partial class SettingViewModel : ViewModel
{
    [ObservableProperty] private string _maaFwVersion = MaaProcessor.MaaUtility.Version;
    [ObservableProperty] private string _mfaVersion = MainWindow.Version;

    [ObservableProperty] private string _resourceVersion = MaaInterface.Instance?.Version ?? string.Empty;

    public bool ShowResourceVersion => !string.IsNullOrWhiteSpace(ResourceVersion);

    private enum NotifyType
    {
        None,
        SelectedIndex,
        ScrollOffset,
    }

    private NotifyType _notifySource = NotifyType.None;

    private System.Timers.Timer? _resetNotifyTimer;

    public SettingViewModel()
    {
        UpdateExternalNotificationProvider();
    }

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

    [ObservableProperty] private List<LocalizationViewModel> _listTitle =
    [
        new("SwitchConfiguration"),
        new("ScheduleSettings"),
        new("UiSettings"),
        new("ConnectionSettings"),
        new("PerformanceSettings"),
        new("StartupSettings"),
        new("ExternalNotificationSettings"),
        new("RunningSettings"),
        new("SoftwareUpdate"),
        new("About"),
    ];


    private int _languageIndex = 0;

    public int LanguageIndex
    {
        set => SetProperty(ref _languageIndex, value);
        get
        {
            _languageIndex = DataSet.GetData("LangIndex", 0);
            return _languageIndex;
        }
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
            _downloadSourceIndex = DataSet.GetData("DownloadSourceIndex", 0);
            return _downloadSourceIndex;
        }
    }


    [ObservableProperty] private List<LocalizationViewModel> _downloadSourceList =
    [
        new("GitHub"),
        new("MirrorChyan"),
    ];
    [ObservableProperty] private bool _retryOnDisconnected = DataSet.GetData("RetryOnDisconnected", false);

    partial void OnRetryOnDisconnectedChanged(bool value)
    {
        DataSet.SetData("RetryOnDisconnected", value);
    }
    [ObservableProperty] private bool _allowAdbRestart = DataSet.GetData("AllowAdbRestart", true);

    partial void OnAllowAdbRestartChanged(bool value)
    {
        DataSet.SetData("AllowAdbRestart", value);
    }

    [ObservableProperty] private bool _allowAdbHardRestart = DataSet.GetData("AllowAdbHardRestart", true);

    partial void OnAllowAdbHardRestartChanged(bool value)
    {
        DataSet.SetData("AllowAdbHardRestart", value);
    }


    public static readonly List<LocalizationViewModel> ExternalNotificationProviders =
    [
        new("DingTalk"),
    ];

    public static List<LocalizationViewModel> ExternalNotificationProvidersShow => ExternalNotificationProviders;

    private static object[] _enabledExternalNotificationProviders = ExternalNotificationProviders.Where(s => DataSet.GetData("ExternalNotificationEnabled", string.Empty).Split(',').Contains(s.ResourceKey))
        .Distinct()
        .ToArray();

    public object[] EnabledExternalNotificationProviders
    {
        get => _enabledExternalNotificationProviders;

        set
        {
            try
            {
                var settingViewModels = value.Cast<LocalizationViewModel>();
                SetProperty(ref _enabledExternalNotificationProviders, value);
                var validProviders = settingViewModels
                    .Where(provider => ExternalNotificationProviders.ContainsKey(provider.ResourceKey ?? string.Empty))
                    .Select(provider => provider.ResourceKey)
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


    private bool _dingTalkEnabled;

    public bool DingTalkEnabled
    {
        get => _dingTalkEnabled;
        set => SetProperty(ref _dingTalkEnabled, value);
    }

    private string _cdkPassword = SimpleEncryptionHelper.Decrypt(DataSet.GetData("DownloadCDK", string.Empty));

    public string CdkPassword
    {
        get => _cdkPassword;
        set
        {
            SetProperty(ref _cdkPassword, value);
            value = SimpleEncryptionHelper.Encrypt(value);
            DataSet.SetData("DownloadCDK", value);
        }
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
        DingTalkEnabled = EnabledExternalNotificationProviderList.Contains("DingTalk");
    }

    [ObservableProperty] private int _themeIndex = DataSet.GetData("ThemeIndex", 0);

    [ObservableProperty] private ObservableCollection<LocalizationViewModel> _themes =
    [
        new("LightColor"),
        new("DarkColor"),
        new("FollowingSystem"),
    ];

    partial void OnThemeIndexChanged(int value)
    {
        ThemeHelper.UpdateThemeIndexChanged(value);
        DataSet.SetData("ThemeIndex", value);
    }

    public TimerModel TimerModels { get; set; } = new();

    public partial class TimerModel
    {
        public partial class TimerProperties : ObservableObject
        {
            public TimerProperties(int timeId, bool isOn, int hour, int min)
            {
                TimerId = timeId;
                _isOn = isOn;
                _hour = hour;
                _min = min;
                // if (timerConfig == null || !DataSet.GetConfigurationList().Contains(timerConfig))
                // {
                //     _timerConfig = DataSet.GetCurrentConfiguration();
                // }
                // else
                // {
                //     _timerConfig = timerConfig;
                // }
                LanguageHelper.LanguageChanged += OnLanguageChanged;
            }

            public int TimerId { get; set; }

            [ObservableProperty] private string _timerName;

            private void OnLanguageChanged(object sender, EventArgs e)
            {
                TimerName = $"{"Timer".ToLocalization()} {TimerId + 1}";
            }

            private bool _isOn;

            /// <summary>
            /// Gets or sets a value indicating whether the timer is set.
            /// </summary>
            public bool IsOn
            {
                get => _isOn;
                set
                {
                    SetProperty(ref _isOn, value);
                    DataSet.SetTimer(TimerId, value);
                }
            }

            private int _hour;

            /// <summary>
            /// Gets or sets the hour of the timer.
            /// </summary>
            public int Hour
            {
                get => _hour;
                set
                {
                    value = Math.Clamp(value, 0, 23);
                    SetProperty(ref _hour, value);
                    DataSet.SetTimerHour(TimerId, _hour.ToString());
                }
            }

            private int _min;

            /// <summary>
            /// Gets or sets the minute of the timer.
            /// </summary>
            public int Min
            {
                get => _min;
                set
                {
                    value = Math.Clamp(value, 0, 59);
                    SetProperty(ref _min, value);
                    DataSet.SetTimerMin(TimerId, _min.ToString());
                }
            }

            // private string? _timerConfig;
            //
            // /// <summary>
            // /// Gets or sets the config of the timer.
            // /// </summary>
            // public string? TimerConfig
            // {
            //     get => _timerConfig;
            //     set
            //     {
            //         SetProperty(ref _timerConfig, value ?? DataSet.GetCurrentConfiguration());
            //         DataSet.SetTimerConfig(TimerId, _timerConfig);
            //     }
            // }
        }

        public TimerProperties[] Timers { get; set; } = new TimerProperties[8];
        private readonly DispatcherTimer _dispatcherTimer;
        public TimerModel()
        {
            for (var i = 0; i < 8; i++)
            {
                Timers[i] = new TimerProperties(
                    i,
                    DataSet.GetTimer(i, false),
                    int.Parse(DataSet.GetTimerHour(i, $"{i * 3}")),
                    int.Parse(DataSet.GetTimerMin(i, "0")));
            }
            _dispatcherTimer = new();
            _dispatcherTimer.Interval = TimeSpan.FromMinutes(1);
            _dispatcherTimer.Tick += CheckTimerElapsed;
            _dispatcherTimer.Start();
        }

        private void CheckTimerElapsed(object sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            foreach (var timer in Timers)
            {
                if (timer.IsOn && timer.Hour == currentTime.Hour && timer.Min == currentTime.Minute)
                {
                    ExecuteTimerTask(timer.TimerId);
                }
            }
        }

        private void ExecuteTimerTask(int timerId)
        {
            var timer = Timers.FirstOrDefault(t => t.TimerId == timerId, null);
            if (timer != null)
            {
                MainWindow.Instance.Start();
            }
        }
    }

    [ObservableProperty] private string _newConfigurationName;

    [RelayCommand]
    private void AddConfiguration()
    {
        if (string.IsNullOrWhiteSpace(NewConfigurationName))
        {
            GrowlHelper.Error("ConfigNameCannotBeEmpty".ToLocalizationFormatted(NewConfigurationName));
            return;
        }
        var configDPath = Path.Combine(AppContext.BaseDirectory, "config");
        var configPath = Path.Combine(configDPath, $"{DataSet.ConfigName}.json");
        var newConfigPath = Path.Combine(configDPath, $"{NewConfigurationName}.json");
        bool configExists = Directory.GetFiles(configDPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Any(name => name.Equals(NewConfigurationName, StringComparison.OrdinalIgnoreCase));

        if (configExists)
        { 
            GrowlHelper.Error("ConfigNameAlreadyExists".ToLocalizationFormatted(NewConfigurationName));
            return;
        }

        if (File.Exists(configPath))
        {
            var content = File.ReadAllText(configPath);
            File.WriteAllText(newConfigPath, content);
        }
        GrowlHelper.Success("ConfigAddedSuccessfully".ToLocalizationFormatted(NewConfigurationName));
    }
}
