using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Utils;


namespace MFAWPF.ViewModels;

public class MainViewModel : ObservableObject
{
    public ObservableCollection<LogItemViewModel> LogItemViewModels { get; } = new();

    public void AddLog(string content, Brush? color = null, string weight = "Regular",
        bool showTime = true)
    {
        color ??= Brushes.Gray;
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(content, color, weight, "HH':'mm':'ss",
                    showTime: showTime));
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

    public GongSolutions.Wpf.DragDrop.IDropTarget DropHandler { get; } = new DragDropHandler();
    

    private bool _isAdb = true;

    public bool IsAdb
    {
        get => _isAdb;
        set => SetProperty(ref _isAdb, value);
    }
}