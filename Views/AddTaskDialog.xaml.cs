using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HandyControl.Data;
using MFAWPF.Utils;
using MFAWPF.ViewModels;

namespace MFAWPF.Views;

public partial class AddTaskDialog
{
    private DragItemViewModel? _outputContent;
    public AddTaskDialogViewModel? Data;

    public DragItemViewModel? OutputContent
    {
        get => _outputContent;
        set => _outputContent = value;
    }

    private readonly ObservableCollection<DragItemViewModel> _source = new();

    public AddTaskDialog(IList<DragItemViewModel>? dragItemViewModels)
    {
        InitializeComponent();
        Data = DataContext as AddTaskDialogViewModel;
        _source.AddRange(dragItemViewModels);
        if (Data != null)
        {
            Data.DataList.Clear();
            Data.DataList.AddRange(_source);
        }
    }

    private void Add(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        OutputContent = ListBoxDemo.SelectedValue as DragItemViewModel;
        Close();
    }

    protected override void Close(object? sender = null, RoutedEventArgs? e = null)
    {
        if (MainWindow.Data != null)
            MainWindow.Data.Idle = true;

        base.Close();
    }

    private void SearchBar_OnSearchStarted(object sender, FunctionEventArgs<string> e)
    {
        string key = e.Info;

        if (string.IsNullOrEmpty(key))
        {
            if (Data != null)
            {
                Data.DataList.Clear();
                Data.DataList.AddRange(_source);
            }
        }
        else
        {
            key = key.ToLower();
            Data?.DataList.Clear();
            foreach (DragItemViewModel item in _source)
            {
                string name = item.Name.ToLower();
                if (name.Contains(key))
                    Data?.DataList.Add(item);
            }
        }
    }
}