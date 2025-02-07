using System.Collections.ObjectModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using MFAWPF.Controls;
using MFAWPF.Helper;
using MFAWPF.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Tools.Command;
using Newtonsoft.Json;

namespace MFAWPF.ViewModels;

public partial class EditTaskDialogViewModel : ViewModel
{
    [ObservableProperty]
    private ObservableCollection<TaskItemViewModel> _dataList;

    private ObservableCollection<TaskItemViewModel>? _colors;

    public ObservableCollection<TaskItemViewModel>? Colors
    {
        get
        {
            if (_colors == null)
            {
                // 获取 Brushes 类中的所有静态属性
                var brushesType = typeof(Brushes);
                var properties = brushesType.GetProperties(BindingFlags.Public | BindingFlags.Static);

                // 获取每个属性的名称（颜色名称），并存入 CustomValue<string> 列表
                _colors = new ObservableCollection<TaskItemViewModel>(properties
                    .Select(p => new TaskItemViewModel()
                    {
                        Name = p.Name
                    }).ToList());
            }

            return _colors;
        }
        set => SetProperty(ref _colors, value);
    }
    [ObservableProperty]
    private int _selectedIndex;
    

    public EditTaskDialog Dialog;
    public readonly Stack<ICommand> UndoStack = new();
    public readonly Stack<ICommand> UndoTaskStack = new();

    public ICommand CopyCommand { get; set; }
    public ICommand PasteCommand { get; set; }
    public ICommand UndoCommand { get; set; }

    public ICommand SaveCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
    

    public EditTaskDialogViewModel()
    {
        SelectedIndex = -1;
        DataList = GetDataList();
        CopyCommand = new RelayCommand(Copy, CanExecute);
        PasteCommand = new RelayCommand(Paste, CanExecute);
        SaveCommand = new RelayCommand(Save, CanExecute);
        UndoCommand = new RelayCommand(Undo, CanExecute);
        DeleteCommand = new RelayCommand(Delete, CanExecute);
    }

    public ObservableCollection<TaskItemViewModel> GetDataList()
    {
        ObservableCollection<TaskItemViewModel> operators = new ObservableCollection<TaskItemViewModel>();

        return operators;
    }

    private TaskItemViewModel _currentTask;

    public TaskItemViewModel CurrentTask
    {
        get => _currentTask;
        set
        {
            if (value?.Task != null && Dialog != null)
            {
                Dialog.TaskName.Text = value.Task.Name;
                Dialog.PropertyGrid.SelectedObject = value.Task;
            }

            SetProperty(ref _currentTask, value);
        }
    }


    private bool CanExecute(object parameter)
    {
        return true;
    }

    private void Copy(object parameter)
    {
        if (CurrentTask != null)
        {
            Clipboard.SetDataObject(CurrentTask.ToString());
        }
    }

    private void Paste(object parameter)
    {
        if (CurrentTask != null && Dialog != null)
        {
            int index = Dialog.ListBoxDemo.Items.IndexOf(CurrentTask);
            IDataObject iData = Clipboard.GetDataObject();
            if (iData?.GetDataPresent(DataFormats.Text) == true)
            {
                try
                {
                   var taskModels =
                        JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(
                            iData.GetData(DataFormats.Text) as string ?? string.Empty);
                    if (taskModels == null || taskModels.Count == 0)
                        return;
                    foreach (var pair in taskModels)
                    {
                        pair.Value.Name = pair.Key;
                        var newItem = new TaskItemViewModel()
                        {
                            Name = pair.Key, Task = pair.Value
                        };
                        DataList?.Insert(index + 1, newItem);
                        UndoStack?.Push(new RelayCommand(_ => DataList?.Remove(newItem)));
                    }
                }
                catch (Exception exception)
                {
                    LoggerService.LogError(exception);
                    throw;
                }
            }
            else
            {
                GrowlHelper.ErrorGlobal("目前剪贴板中数据不可转换为文本");
            }
        }
    }

    private void Undo(object parameter)
    {
        if (UndoStack.Count > 0)
        {
            var undoCommand = UndoStack.Pop();
            undoCommand.Execute(null);
        }
    }

    private void Save(object parameter)
    {
        Dialog?.Save_Pipeline(null, null);
    }

    private void Delete(object parameter)
    {
        if (CurrentTask != null && DataList != null)
        {
            var itemToDelete = CurrentTask;
            int index = DataList.IndexOf(itemToDelete);
            DataList.Remove(itemToDelete);
            UndoStack.Push(new RelayCommand(_ => DataList.Insert(index, itemToDelete)));
        }
    }

    private AttributeButton _selectedAttribute;

    public AttributeButton SelectedAttribute
    {
        get => _selectedAttribute;
        set => SetProperty(ref _selectedAttribute, value);
    }

    // private void SaveTask(object parameter)
    // {
    //     Dialog?.Save(null, null);
    // }
    //
    // private void CopyTask(object parameter)
    // {
    //     if (SelectedAttribute != null && SelectedAttribute.Attribute != null)
    //     {
    //         var settings = new JsonSerializerSettings
    //         {
    //             Formatting = Formatting.Indented,
    //             NullValueHandling = NullValueHandling.Ignore,
    //             DefaultValueHandling = DefaultValueHandling.Ignore
    //         };
    //
    //         string json = JsonConvert.SerializeObject(SelectedAttribute.Attribute, settings);
    //         Clipboard.SetDataObject(json);
    //     }
    // }
    //
    // private bool CanExecuteTask(object parameter)
    // {
    //     return SelectedAttribute != null;
    // }
    //
    // private void PasteTask(object parameter)
    // {
    //     IDataObject iData = Clipboard.GetDataObject();
    //     if (iData.GetDataPresent(DataFormats.Text))
    //     {
    //         try
    //         {
    //             var attribute =
    //                 JsonConvert.DeserializeObject<Attribute>(
    //                     (string)iData.GetData(DataFormats.Text));
    //             // AttributeButton button = Dialog?.AddAttribute(attribute);
    //             // if (button != null)
    //             //     UndoTaskStack.Push(new RelayCommand(_ => Dialog?.Parts.Children.Remove(button)));
    //         }
    //         catch (Exception exception)
    //         {
    //             Console.WriteLine(exception);
    //             throw;
    //         }
    //     }
    //     else
    //     {
    //         Growls.ErrorGlobal("目前剪贴板中数据不可转换为文本");
    //     }
    // }
    //
    // private bool CanExecutePaste(object parameter)
    // {
    //     return Clipboard.GetDataObject() != null;
    // }
    //
    // private void DeleteTask(object parameter)
    // {
    //     if (SelectedAttribute != null)
    //     {
    //         var itemToDelete = SelectedAttribute.Attribute;
    //
    //         if (SelectedAttribute.Parent is Panel itemPanel)
    //         {
    //             int index = itemPanel.Children.IndexOf(SelectedAttribute);
    //             itemPanel.Children.Remove(SelectedAttribute);
    //             // UndoTaskStack.Push(new RelayCommand(_ => Dialog?.AddAttribute(itemToDelete, index)));
    //         }
    //     }
    // }
    //
    // private void UndoTask(object parameter)
    // {
    //     if (UndoTaskStack.Count > 0)
    //     {
    //         var undoCommand = UndoTaskStack.Pop();
    //         undoCommand.Execute(null);
    //     }
    // }
}