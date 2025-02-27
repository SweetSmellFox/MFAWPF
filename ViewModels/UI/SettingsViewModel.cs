using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Helper.ValueType;
using MFAWPF.Views.UI;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace MFAWPF.ViewModels.UI;

public partial class SettingsViewModel : ViewModel
{
    #region Init

    public SettingsViewModel()
    {
        HotKeyShowGui = MFAHotKey.Parse(GlobalConfiguration.GetConfiguration("ShowGui", ""));
        HotKeyLinkStart = MFAHotKey.Parse(GlobalConfiguration.GetConfiguration("LinkStart", ""));
    }

    [ObservableProperty] ObservableCollection<Tool.LocalizationViewModel> _listTitle =
    [
        new("SwitchConfiguration"),
        new("ScheduleSettings"),
        new("PerformanceSettings"),
        new("RunningSettings"),
        new("ConnectionSettings"),
        new("StartupSettings"),
        new("UiSettings"),
        new("ExternalNotificationSettings"),
        new("HotKeySettings"),
        new("UpdateSettings"),
        new("About"),
    ];

    #endregion Init

    #region 设置页面列表和滚动视图联动绑定

    public enum NotifyType
    {
        None,
        SelectedIndex,
        ScrollOffset,
    }

    [ObservableProperty] private NotifyType _notifySource = NotifyType.None;

    private System.Timers.Timer? _resetNotifyTimer;

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

    #endregion 设置页面列表和滚动视图联动绑定

    #region 配置

    public ObservableCollection<MFAConfig> ConfigurationList { get; set; } = MFAConfiguration.Configs;

    [ObservableProperty] private string? _currentConfiguration = MFAConfiguration.GetCurrentConfiguration();

    partial void OnCurrentConfigurationChanged(string? value)
    {
        MFAConfiguration.SetDefaultConfig(value);
        MaaProcessor.RestartMFA();
    }

    [ObservableProperty] private string _newConfigurationName = string.Empty;

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

    #endregion 配置

    #region HotKey

    private MFAHotKey _hotKeyShowGui = MFAHotKey.NOTSET;

    public MFAHotKey HotKeyShowGui
    {
        get => _hotKeyShowGui;
        set => SetHotKey(ref _hotKeyShowGui, value, "ShowGui", Instances.RootViewModel.ToggleVisible);
    }

    private MFAHotKey _hotKeyLinkStart = MFAHotKey.NOTSET;
    public MFAHotKey HotKeyLinkStart
    {
        get => _hotKeyLinkStart;
        set => SetHotKey(ref _hotKeyLinkStart, value, "LinkStart", Instances.TaskQueueView.Toggle);
    }
    
    public void SetHotKey(ref MFAHotKey value, MFAHotKey? newValue, string type, Action action)
    {
        if (newValue != null)
        {
            if (Application.Current.MainWindow?.IsHotKeyRegistered(newValue) == true)
            {
                newValue = MFAHotKey.ERROR;
            }
            else
            {
                HotKeyHelper.RegisterHotKey(Application.Current.MainWindow, newValue, action);
            }
            GlobalConfiguration.SetConfiguration(type, newValue.ToString());
            SetProperty(ref value, newValue);
        }
    }

    #endregion HotKey
}
