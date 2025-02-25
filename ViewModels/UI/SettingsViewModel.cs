using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Views.UI;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MFAWPF.ViewModels.UI;

public partial class SettingsViewModel : ViewModel
{
    [ObservableProperty] private string _maaFwVersion = MaaProcessor.MaaUtility.Version;
    [ObservableProperty] private string _mfaVersion = RootView.Version;

    [ObservableProperty] private string _resourceVersion = MaaInterface.Instance?.Version ?? string.Empty;

    public bool ShowResourceVersion => !string.IsNullOrWhiteSpace(ResourceVersion);

    public enum NotifyType
    {
        None,
        SelectedIndex,
        ScrollOffset,
    }

    [ObservableProperty] private NotifyType _notifySource = NotifyType.None;

    private System.Timers.Timer? _resetNotifyTimer;

    public SettingsViewModel()
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
            NotifySource = NotifyType.None;
        };
        _resetNotifyTimer.AutoReset = false;
        _resetNotifyTimer.Enabled = true;
        _resetNotifyTimer.Start();
    }

    [ObservableProperty] private int _selectedIndex;

    [ObservableProperty] private double _scrollOffset;

    [ObservableProperty] private double _scrollViewportHeight;

    [ObservableProperty] private double _scrollExtentHeight;

    [ObservableProperty] private List<double> _dividerVerticalOffsetList = new();
    partial void OnSelectedIndexChanged(int value)
    {
        if (NotifySource == NotifyType.None && DividerVerticalOffsetList.Count > value && value >= 0)
        {
            NotifySource = NotifyType.SelectedIndex;
            ScrollOffset = DividerVerticalOffsetList[value];
            ResetNotifySource();
        }
    }

    partial void OnScrollOffsetChanged(double value)
    {
        if (NotifySource == NotifyType.None)
        {
            NotifySource = NotifyType.ScrollOffset;
            SelectedIndex = FindClosestDividerIndex(value);
            ResetNotifySource();
        }
    }

    private int FindClosestDividerIndex(double scrollOffset)
    {
        if (DividerVerticalOffsetList.Count == 0) return -1;

        double viewportBottom = scrollOffset + ScrollViewportHeight;
        const double TOLERANCE = 2;

        if (viewportBottom >= ScrollExtentHeight - TOLERANCE)
            return DividerVerticalOffsetList.Count - 1;

        if (scrollOffset <= DividerVerticalOffsetList[0] + TOLERANCE)
            return 0;

        int left = 0, right = DividerVerticalOffsetList.Count - 1;
        while (left < right)
        {
            int mid = (left + right + 1) / 2;
            if (DividerVerticalOffsetList[mid] > scrollOffset)
                right = mid - 1;
            else
                left = mid;
        }

        var shouldSelectNext = left < DividerVerticalOffsetList.Count - 1
            && (DividerVerticalOffsetList[left + 1] < viewportBottom || Math.Abs(DividerVerticalOffsetList[left + 1] - scrollOffset) < Math.Abs(DividerVerticalOffsetList[left] - scrollOffset));

        return shouldSelectNext ? left + 1 : left;
    }


    [ObservableProperty] private List<Tool.LocalizationViewModel> _listTitle =
    [
        new("SwitchConfiguration"),
        new("ScheduleSettings"),
        new("PerformanceSettings"),
        new("RunningSettings"),
        new("ConnectionSettings"),
        new("StartupSettings"),
        new("ExternalNotificationSettings"),
        new("UiSettings"),
        new("SoftwareUpdate"),
        new("About"),
    ];


    [ObservableProperty] private int _languageIndex = MFAConfiguration.GetConfiguration("LangIndex", 0);

    partial void OnLanguageIndexChanged(int value)
    {
        LanguageHelper.ChangeLanguage(SupportedLanguages[value]);
        MFAConfiguration.SetConfiguration("LangIndex", value);
    }

    [ObservableProperty] private int _themeIndex = MFAConfiguration.GetConfiguration("ThemeIndex", 0);

    [ObservableProperty] private ObservableCollection<Tool.LocalizationViewModel> _themes =
    [
        new("LightColor"),
        new("DarkColor"),
        new("FollowingSystem"),
    ];

    partial void OnThemeIndexChanged(int value)
    {
        ThemeHelper.UpdateThemeIndexChanged(value);
        MFAConfiguration.SetConfiguration("ThemeIndex", value);
    }

    private bool _shouldMinimizeToTray = MFAConfiguration.GetConfiguration("ShouldMinimizeToTray", false);

    public bool ShouldMinimizeToTray
    {
        set
        {
            SetProperty(ref _shouldMinimizeToTray, value);
            MFAConfiguration.SetConfiguration("ShouldMinimizeToTray", value);
        }
        get => _shouldMinimizeToTray;
    }

    public ObservableCollection<LanguageHelper.SupportedLanguage> SupportedLanguages => LanguageHelper.SupportedLanguages;
    private int _downloadSourceIndex;

    public int DownloadSourceIndex
    {
        set
        {
            MFAConfiguration.SetConfiguration("DownloadSourceIndex", value);
            SetCurrentProperty(ref _downloadSourceIndex, value);
        }
        get
        {
            _downloadSourceIndex = MFAConfiguration.GetConfiguration("DownloadSourceIndex", 0);
            return _downloadSourceIndex;
        }
    }

    [ObservableProperty] private bool _rememberAdb = MFAConfiguration.GetConfiguration("RememberAdb", true);

    partial void OnRememberAdbChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("RememberAdb", value);
    }

    public static ObservableCollection<string> AdbControlScreenCapTypes =>
    [
        "Default", "RawWithGzip", "RawByNetcat",
        "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
        "EmulatorExtras"
    ];
    public static ObservableCollection<Tool.LocalizationViewModel> AdbControlInputTypes => [new("MiniTouch"), new("MaaTouch"), new("AdbInput"), new("AutoDetect")];
    public static ObservableCollection<string> Win32ControlScreenCapTypes => ["FramePool", "DXGIDesktopDup", "GDI"];
    public static ObservableCollection<string> Win32ControlInputTypes => ["Seize", "SendMessage"];

    [ObservableProperty] private string _adbControlScreenCapType = MFAConfiguration.GetConfiguration("AdbControlScreenCapType", AdbControlScreenCapTypes[0]);
    [ObservableProperty] private string _adbControlInputType = MFAConfiguration.GetConfiguration("AdbControlInputType", AdbControlInputTypes[0].ResourceKey);
    [ObservableProperty] private string _win32ControlScreenCapType = MFAConfiguration.GetConfiguration("Win32ControlScreenCapType", Win32ControlScreenCapTypes[0]);
    [ObservableProperty] private string _win32ControlInputType = MFAConfiguration.GetConfiguration("Win32ControlInputType", Win32ControlInputTypes[0]);
    partial void OnAdbControlScreenCapTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("AdbControlScreenCapType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }
    partial void OnAdbControlInputTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("AdbControlInputType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }
    partial void OnWin32ControlScreenCapTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Win32ControlScreenCapType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }
    partial void OnWin32ControlInputTypeChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Win32ControlInputType", value);
        MaaProcessor.Instance.SetCurrentTasker();
    }

    public static ObservableCollection<Tool.LocalizationViewModel> GpuOptions => [new("GpuOptionAuto"), new("GpuOptionDisable")];

    [ObservableProperty] private int _gpuIndex = MFAConfiguration.GetConfiguration("EnableGPU", false) ? 0 : 1;

    partial void OnGpuIndexChanged(int value)
    {
        MFAConfiguration.SetConfiguration("EnableGPU", value == 0);
    }

    [ObservableProperty] private bool _enableRecording = MFAConfiguration.MaaConfig.GetConfig("recording", false);

    [ObservableProperty] private bool _enableSaveDraw = MFAConfiguration.MaaConfig.GetConfig("save_draw", false);

    [ObservableProperty] private bool _showHitDraw = MFAConfiguration.MaaConfig.GetConfig("show_hit_draw", false);

    [ObservableProperty] private string _prescript = MFAConfiguration.GetConfiguration("Prescript", string.Empty);

    [ObservableProperty] private string _postScript = MFAConfiguration.GetConfiguration("Post-script", string.Empty);

    [ObservableProperty] private bool _isDebugMode = MFAConfiguration.MaaConfig.GetConfig("recording", false) || MFAConfiguration.MaaConfig.GetConfig("save_draw", false) || MFAConfiguration.MaaConfig.GetConfig("show_hit_draw", false);
    private bool _shouldTip = true;
    
    partial void OnIsDebugModeChanged(bool value)
    {
        if (value && _shouldTip)
        {
            MessageBoxHelper.Show("DebugModeWarning".ToLocalization(), "Tip".ToLocalization(), MessageBoxButton.OK, MessageBoxImage.Warning);
            _shouldTip = false;
        }
    }
    
    partial void OnEnableRecordingChanged(bool value)
    {
        MFAConfiguration.MaaConfig.SetConfig("recording", value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnEnableSaveDrawChanged(bool value)
    {
        MFAConfiguration.MaaConfig.SetConfig("save_draw", value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnShowHitDrawChanged(bool value)
    {
        MFAConfiguration.MaaConfig.SetConfig("show_hit_draw", value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnPrescriptChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Prescript", value);
    }

    partial void OnPostScriptChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Post-script", value);
    }

    [ObservableProperty] private bool _autoMinimize = MFAConfiguration.GetConfiguration("AutoMinimize", false);

    [ObservableProperty] private bool _autoHide = MFAConfiguration.GetConfiguration("AutoHide", false);

    [ObservableProperty] private string _softwarePath = MFAConfiguration.GetConfiguration("SoftwarePath", string.Empty);
    [ObservableProperty] private string _emulatorConfig = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);

    [ObservableProperty] private double _waitSoftwareTime = MFAConfiguration.GetConfiguration("WaitSoftwareTime", 60.0);


    partial void OnAutoMinimizeChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("AutoMinimize", value);
    }

    partial void OnAutoHideChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("AutoHide", value);
    }

    partial void OnSoftwarePathChanged(string value)
    {
        MFAConfiguration.SetConfiguration("SoftwarePath", value);
    }

    partial void OnEmulatorConfigChanged(string value)
    {
        MFAConfiguration.SetConfiguration("EmulatorConfig", value);
    }

    partial void OnWaitSoftwareTimeChanged(double value)
    {
        MFAConfiguration.SetConfiguration("WaitSoftwareTime", value);
    }

    [RelayCommand]
    private void ExternalNotificationSendTest()
    {
        MaaProcessor.ExternalNotificationAsync();
    }

    [RelayCommand]
    private void SelectSoft()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "SelectExecutableFile".ToLocalization(),
            Filter = "ExeFilter".ToLocalization()
        };

        if (openFileDialog.ShowDialog().IsTrue())
        {
            SoftwarePath = openFileDialog.FileName;
        }
    }

    [ObservableProperty] private List<Tool.LocalizationViewModel> _downloadSourceList =
    [
        new()
        {
            Name = "GitHub"
        },
        new("MirrorChyan"),
    ];

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


    public static readonly List<Tool.LocalizationViewModel> ExternalNotificationProviders =
    [
        new("DingTalk"), new("Email"),
    ];

    public static List<Tool.LocalizationViewModel> ExternalNotificationProvidersShow => ExternalNotificationProviders;

    private static object[] _enabledExternalNotificationProviders = ExternalNotificationProviders.Where(s => MFAConfiguration.GetConfiguration("ExternalNotificationEnabled", string.Empty).Split(',').Contains(s.ResourceKey))
        .Distinct()
        .ToArray();

    public object[] EnabledExternalNotificationProviders
    {
        get => _enabledExternalNotificationProviders;

        set
        {
            try
            {
                var settingViewModels = value.Cast<Tool.LocalizationViewModel>();
                SetProperty(ref _enabledExternalNotificationProviders, value);
                var validProviders = settingViewModels
                    .Where(provider => ExternalNotificationProviders.ContainsKey(provider.ResourceKey ?? string.Empty))
                    .Select(provider => provider.ResourceKey)
                    .Distinct();

                var config = string.Join(",", validProviders);
                MFAConfiguration.SetConfiguration("ExternalNotificationEnabled", config);
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


    [ObservableProperty] private bool _dingTalkEnabled;
    [ObservableProperty] private bool _emailEnabled;

    private string _cdkPassword = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("DownloadCDK", string.Empty));

    public string CdkPassword
    {
        get => _cdkPassword;
        set
        {
            SetProperty(ref _cdkPassword, value);
            value = SimpleEncryptionHelper.Encrypt(value);
            MFAConfiguration.SetConfiguration("DownloadCDK", value);
        }
    }

    [ObservableProperty] private string _dingTalkToken = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationDingTalkToken", string.Empty));
    partial void OnDingTalkTokenChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationDingTalkToken", SimpleEncryptionHelper.Encrypt(value));
    }


    [ObservableProperty] private string _dingTalkSecret = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationDingTalkSecret", string.Empty));
    partial void OnDingTalkSecretChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationDingTalkSecret", SimpleEncryptionHelper.Encrypt(value));
    }

    [ObservableProperty] private string _emailAccount = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationEmailAccount", string.Empty));
    partial void OnEmailAccountChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationEmailAccount", SimpleEncryptionHelper.Encrypt(value));
    }


    [ObservableProperty] private string _emailSecret = SimpleEncryptionHelper.Decrypt(MFAConfiguration.GetConfiguration("ExternalNotificationEmailSecret", string.Empty));
    partial void OnEmailSecretChanged(string value)
    {
        MFAConfiguration.SetConfiguration("ExternalNotificationEmailSecret", SimpleEncryptionHelper.Encrypt(value));
    }

    public void UpdateExternalNotificationProvider()
    {
        DingTalkEnabled = EnabledExternalNotificationProviderList.Contains("DingTalk");
        EmailEnabled = EnabledExternalNotificationProviderList.Contains("Email");
    }

    [ObservableProperty] private bool _enableCheckVersion = MFAConfiguration.GetConfiguration("EnableCheckVersion", true);

    [ObservableProperty] private bool _enableAutoUpdateResource = MFAConfiguration.GetConfiguration("EnableAutoUpdateResource", false);

    [ObservableProperty] private bool _enableAutoUpdateMFA = MFAConfiguration.GetConfiguration("EnableAutoUpdateMFA", false);

    partial void OnEnableCheckVersionChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("EnableCheckVersion", value);
    }

    partial void OnEnableAutoUpdateResourceChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("EnableAutoUpdateResource", value);
    }

    partial void OnEnableAutoUpdateMFAChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("EnableAutoUpdateMFA", value);
    }

    [RelayCommand]
    private void UpdateResource()
    {
        VersionChecker.UpdateResourceAsync();
    }
    [RelayCommand]
    private void CheckResourceUpdate()
    {
        VersionChecker.CheckResourceVersionAsync();
    }
    [RelayCommand]
    private void UpdateMFA()
    {
        VersionChecker.UpdateMFAAsync();
    }
    [RelayCommand]
    private void CheckMFAUpdate()
    {
        VersionChecker.CheckMFAVersionAsync();
    }

    [RelayCommand]
    private void UpdateMaaFW()
    {
        VersionChecker.UpdateMaaFwAsync();
    }

    public TimerModel TimerModels { get; set; } = new();
    [ObservableProperty] private bool _customConfig = Convert.ToBoolean(GlobalConfiguration.GetConfiguration("CustomConfig", bool.FalseString));
    [ObservableProperty] private bool _forceScheduledStart = Convert.ToBoolean(GlobalConfiguration.GetConfiguration("ForceScheduledStart", bool.FalseString));


    partial void OnCustomConfigChanged(bool value)
    {
        GlobalConfiguration.SetConfiguration("CustomConfig", value.ToString());
    }

    partial void OnForceScheduledStartChanged(bool value)
    {
        GlobalConfiguration.SetConfiguration("ForceScheduledStart", value.ToString());
    }

    public partial class TimerModel
    {
        public partial class TimerProperties : ObservableObject
        {
            public TimerProperties(int timeId, bool isOn, int hour, int min, string? timerConfig)
            {
                TimerId = timeId;
                _isOn = isOn;
                _hour = hour;
                _min = min;
                TimerName = $"{"Timer".ToLocalization()} {TimerId + 1}";
                if (timerConfig == null || !MFAConfiguration.Configs.Any(c => c.Name.Equals(timerConfig)))
                {
                    _timerConfig = MFAConfiguration.GetCurrentConfiguration();
                }
                else
                {
                    _timerConfig = timerConfig;
                }
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
                    GlobalConfiguration.SetTimer(TimerId, value.ToString());
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
                    GlobalConfiguration.SetTimerHour(TimerId, _hour.ToString());
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
                    GlobalConfiguration.SetTimerMin(TimerId, _min.ToString());
                }
            }

            private string? _timerConfig;

            /// <summary>
            /// Gets or sets the config of the timer.
            /// </summary>
            public string? TimerConfig
            {
                get => _timerConfig;
                set
                {
                    SetProperty(ref _timerConfig, value ?? MFAConfiguration.GetCurrentConfiguration());
                    GlobalConfiguration.SetTimerConfig(TimerId, _timerConfig);
                }
            }
        }

        public TimerProperties[] Timers { get; set; } = new TimerProperties[8];
        private readonly DispatcherTimer _dispatcherTimer;
        public TimerModel()
        {
            for (var i = 0; i < 8; i++)
            {
                Timers[i] = new TimerProperties(
                    i,
                    GlobalConfiguration.GetTimer(i, bool.FalseString) == bool.TrueString,
                    int.Parse(GlobalConfiguration.GetTimerHour(i, $"{i * 3}")),
                    int.Parse(GlobalConfiguration.GetTimerMin(i, "0")), GlobalConfiguration.GetTimerConfig(i, MFAConfiguration.GetCurrentConfiguration()));
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
                if (timer.IsOn && timer.Hour == currentTime.Hour && timer.Min == currentTime.Minute + 2)
                {
                    SwitchConfiguration(timer.TimerId);
                }
            }
        }

        private void SwitchConfiguration(int timerId)
        {
            var timer = Timers.FirstOrDefault(t => t.TimerId == timerId, null);
            if (timer != null)
            {
                var config = timer.TimerConfig ?? MFAConfiguration.GetCurrentConfiguration();
                if (config != MFAConfiguration.GetCurrentConfiguration())
                {
                    MFAConfiguration.SetDefaultConfig(config);
                    MaaProcessor.RestartMFA(true);
                }
            }
        }

        private void ExecuteTimerTask(int timerId)
        {
            var timer = Timers.FirstOrDefault(t => t.TimerId == timerId, null);
            if (timer != null)
            {
                if (Convert.ToBoolean(GlobalConfiguration.GetConfiguration("ForceScheduledStart", bool.FalseString)) && Instances.RootViewModel.IsRunning)
                    Instances.TaskQueueView.Stop();
                Instances.TaskQueueView.Start();
            }
        }
    }

    public ObservableCollection<MFAConfig> ConfigurationList { get; set; } = MFAConfiguration.Configs;

    [ObservableProperty] private string? _currentConfiguration = MFAConfiguration.GetCurrentConfiguration();

    partial void OnCurrentConfigurationChanged(string? value)
    {
        MFAConfiguration.SetDefaultConfig(value);
        MaaProcessor.RestartMFA();
    }

    [ObservableProperty] private string _newConfigurationName;

    [RelayCommand]
    private void AddConfiguration()
    {
        if (string.IsNullOrWhiteSpace(NewConfigurationName))
        {
            NewConfigurationName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        var configDPath = Path.Combine(AppContext.BaseDirectory, "config");
        var configPath = Path.Combine(configDPath, $"{MFAConfiguration.GetActualConfiguration()}.json");
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

            MFAConfiguration.AddNewConfig(NewConfigurationName);
            GrowlHelper.Success("ConfigAddedSuccessfully".ToLocalizationFormatted(NewConfigurationName));
        }

    }
}
