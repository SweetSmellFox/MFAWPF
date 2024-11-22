using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Command;
using MFAWPF.Utils;
using System.Text.RegularExpressions;
using System.Windows.Threading;


namespace MFAWPF.ViewModels;

public class MainViewModel : ObservableObject
{
    public ObservableCollection<LogItemViewModel> LogItemViewModels { get; } = new();

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

    public RelayCommand<FunctionEventArgs<object>> SwitchItemCmd => new Lazy<RelayCommand<FunctionEventArgs<object>>>(
        () =>
            new RelayCommand<FunctionEventArgs<object>>(SwitchItem)).Value;

    private void SwitchItem(FunctionEventArgs<object> info)
    {
        Growl.Info((info.Info as SideMenuItem)?.Header.ToString(), "InfoMessage");
    }
}
