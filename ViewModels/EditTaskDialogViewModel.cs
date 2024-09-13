using System.Collections.ObjectModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MFAWPF.Controls;
using MFAWPF.Utils;
using MFAWPF.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Tools.Command;
using HandyControl.Tools.Extension;
using Newtonsoft.Json;
using Attribute = MFAWPF.Utils.Attribute;

namespace MFAWPF.ViewModels;

public class EditTaskDialogViewModel : ObservableObject
{
    private ObservableCollection<TaskItemViewModel>? dataList;

    public ObservableCollection<TaskItemViewModel>? DataList
    {
        get => dataList;
        set => SetProperty(ref dataList, value);
    }

    private ObservableCollection<TaskItemViewModel>? _colors;

    public ObservableCollection<TaskItemViewModel> Colors
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

    private int selectedIndex = 0;

    public int SelectedIndex
    {
        get => selectedIndex;
        set =>
            SetProperty(ref selectedIndex, value);
    }

    public EditTaskDialog? Dialog;
    public readonly Stack<ICommand> UndoStack = new();
    public readonly Stack<ICommand> UndoTaskStack = new();

    public ICommand CopyCommand { get; set; }
    public ICommand PasteCommand { get; set; }
    public ICommand UndoCommand { get; set; }

    public ICommand SaveCommand { get; set; }
    public ICommand DeleteCommand { get; set; }

    public ICommand SaveTaskCommand { get; set; }
    public ICommand CopyTaskCommand { get; }
    public ICommand PasteTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand UndoTaskCommand { get; }

    public EditTaskDialogViewModel()
    {
        SelectedIndex = -1;
        DataList = GetDataList();
        CopyCommand = new RelayCommand(Copy, CanExecute);
        PasteCommand = new RelayCommand(Paste, CanExecute);
        SaveCommand = new RelayCommand(Save, CanExecute);
        UndoCommand = new RelayCommand(Undo, CanExecute);
        DeleteCommand = new RelayCommand(Delete, CanExecute);
        SaveTaskCommand = new RelayCommand(SaveTask, CanExecuteTask);
        CopyTaskCommand = new RelayCommand(CopyTask, CanExecuteTask);
        PasteTaskCommand = new RelayCommand(PasteTask, CanExecutePaste);
        DeleteTaskCommand = new RelayCommand(DeleteTask, CanExecuteTask);
        UndoTaskCommand = new RelayCommand(UndoTask, CanExecute);
    }

    public ObservableCollection<TaskItemViewModel> GetDataList()
    {
        ObservableCollection<TaskItemViewModel> operators = new ObservableCollection<TaskItemViewModel>();

        return operators;
    }

    private TaskItemViewModel? _currentTask;

    public TaskItemViewModel? CurrentTask
    {
        get => _currentTask;
        set
        {
            if (value?.Task != null && Dialog != null)
            {
                Dialog.TaskName.Text = value.Task.name;
                Dialog.PropertyGrid.SelectedObject = value.Task;
                // Dialog.Parts.Children.Clear();
                // foreach (var VARIABLE in value.Task.ToAttributeList())
                //     Dialog.AddAttribute(VARIABLE);
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
            IDataObject? iData = Clipboard.GetDataObject();
            if (iData?.GetDataPresent(DataFormats.Text) == true)
            {
                try
                {
                    Dictionary<string, TaskModel>? taskModels =
                        JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(
                            (string)iData.GetData(DataFormats.Text));
                    if (taskModels == null || taskModels.Count == 0)
                        return;
                    foreach (var VARIABLE in taskModels)
                    {
                        VARIABLE.Value.name = VARIABLE.Key;
                        var newItem = new TaskItemViewModel()
                        {
                            Name = VARIABLE.Key, Task = VARIABLE.Value
                        };
                        DataList?.Insert(index + 1, newItem);
                        UndoStack?.Push(new RelayCommand(_ => DataList?.Remove(newItem)));
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            }
            else
            {
                Growls.ErrorGlobal("目前剪贴板中数据不可转换为文本");
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

    private AttributeButton? _selectedAttribute;

    public AttributeButton? SelectedAttribute
    {
        get => _selectedAttribute;
        set => SetProperty(ref _selectedAttribute, value);
    }

    private void SaveTask(object parameter)
    {
        Dialog?.Save(null, null);
    }

    private void CopyTask(object parameter)
    {
        if (SelectedAttribute != null && SelectedAttribute.Attribute != null)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(SelectedAttribute.Attribute, settings);
            Clipboard.SetDataObject(json);
        }
    }

    private bool CanExecuteTask(object parameter)
    {
        return SelectedAttribute != null;
    }

    private void PasteTask(object parameter)
    {
        IDataObject iData = Clipboard.GetDataObject();
        if (iData.GetDataPresent(DataFormats.Text))
        {
            try
            {
                var attribute =
                    JsonConvert.DeserializeObject<Attribute>(
                        (string)iData.GetData(DataFormats.Text));
                // AttributeButton? button = Dialog?.AddAttribute(attribute);
                // if (button != null)
                //     UndoTaskStack.Push(new RelayCommand(_ => Dialog?.Parts.Children.Remove(button)));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        else
        {
            Growls.ErrorGlobal("目前剪贴板中数据不可转换为文本");
        }
    }

    private bool CanExecutePaste(object parameter)
    {
        return Clipboard.GetDataObject() != null;
    }

    private void DeleteTask(object parameter)
    {
        if (SelectedAttribute != null)
        {
            var itemToDelete = SelectedAttribute.Attribute;

            if (SelectedAttribute.Parent is Panel itemPanel)
            {
                int index = itemPanel.Children.IndexOf(SelectedAttribute);
                itemPanel.Children.Remove(SelectedAttribute);
                // UndoTaskStack.Push(new RelayCommand(_ => Dialog?.AddAttribute(itemToDelete, index)));
            }
        }
    }

    private void UndoTask(object parameter)
    {
        if (UndoTaskStack.Count > 0)
        {
            var undoCommand = UndoTaskStack.Pop();
            undoCommand.Execute(null);
        }
    }
}