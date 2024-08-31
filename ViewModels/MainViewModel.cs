using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Collections;
using MFAWPF.Utils;
using MFAWPF.Views;


namespace MFAWPF.ViewModels;

public class MainViewModel : ObservableObject
{
    public ObservableCollection<LogItemViewModel> LogItemViewModels { get; } = new();

    public void AddLog(string content, Brush? color = null, string weight = "Regular",
        bool showTime = true)
    {
        if (color == null)
            color = Brushes.Gray;
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
        if (color == null)
            color = Brushes.Gray;
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(key, color, "Regular", true, "HH':'mm':'ss",
                    true, formatArgsKeys));
            });
        });
    }

    public ManualObservableCollection<TaskItemViewModel> SourceItems { get; set; } =
        new();

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


    private Brush _windowTopMostButtonForeground =
        (Brush)Application.Current.FindResource("MainBackgroundBrush");

    public Brush WindowTopMostButtonForeground
    {
        get => _windowTopMostButtonForeground;
        set => SetProperty(ref _windowTopMostButtonForeground, value);
    }
}