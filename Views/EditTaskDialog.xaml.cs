using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MFAWPF.Actions;
using MFAWPF.Controls;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Command;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using Microsoft.Win32;
using Newtonsoft.Json;
using Attribute = MFAWPF.Utils.Attribute;
using Path = System.Windows.Shapes.Path;

namespace MFAWPF.Views;

public partial class EditTaskDialog 
{
    private List<TaskModel> tasks;
    public EditTaskDialogViewModel? Data;

    public EditTaskDialog() 
    {
        InitializeComponent();
        tasks = new List<TaskModel>();
        Data = DataContext as EditTaskDialogViewModel;
        if (Data != null)
            Data.Dialog = this;
    }

    protected override void Close(object? sender, RoutedEventArgs? e)
    {
        if (MainWindow.Data != null)
            MainWindow.Data.Idle = true;
        base.Close(sender, e);
    }

    private void List_KeyDown(object sender, KeyEventArgs e)
    {
        if (Data is { CurrentTask: not null, DataList: not null })
        {
            if (e.Key == Key.Delete)
            {
                var itemToDelete = Data.CurrentTask;
                int index = Data.DataList?.IndexOf(itemToDelete) ?? -1;
                if (index != -1)
                {
                    Data?.DataList?.Remove(itemToDelete);
                    Data?.UndoStack?.Push(new RelayCommand(o => Data?.DataList?.Insert(index, itemToDelete)));
                }
            }
        }
    }

    public void Save(object? sender, RoutedEventArgs? e)
    {
        if (Data?.DataList != null && Data.DataList
                .Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.Name.Equals(TaskName.Text))
                .ToList()
                .Count > 1)
        {
            Growls.Error(string.Format("DuplicateTaskNameError".GetLocalizationString(), TaskName.Text));
            return;
        }

        if (Data?.CurrentTask?.Task != null)
        {
            Data.CurrentTask.Task.Reset();
            Data.CurrentTask.Task.name = TaskName.Text;
            foreach (var VARIABLE in Parts.Children)
            {
                if (VARIABLE is AttributeButton button)
                {
                    if (button.Attribute != null)
                        Data.CurrentTask.Task.Set(button.Attribute);
                }
            }

            Data.CurrentTask.Task = Data.CurrentTask.Task;
            _chartDialog?.UpdateGraph();
            Growl.SuccessGlobal("SaveSuccessMessage".GetLocalizationString());
        }
        else
        {
            Growls.ErrorGlobal("SaveFailureMessage".GetLocalizationString());
        }
    }

    private TaskFlowChartDialog? _chartDialog;

    private void ShowChart(object sender, RoutedEventArgs e)
    {
        _chartDialog = new TaskFlowChartDialog(this);
        _chartDialog.Show();
    }

    private string PipelineFilePath = MaaProcessor.ResourcePipelineFilePath;

    private void Load(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog()
        {
            Title = "LoadPipelineTitle".GetLocalizationString()
        };
        openFileDialog.Filter = "JSON 文件 (*.json)|*.json|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            string fileName = System.IO.Path.GetFileName(filePath);
            PipelineFilePath = filePath.Replace(fileName, "");
            PipelineFileName.Text = fileName;
            try
            {
                string? jsonText = File.ReadAllText(filePath);
                Dictionary<string, TaskModel>? taskDictionary =
                    JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(jsonText);
                Data?.DataList?.Clear();
                if (taskDictionary == null || taskDictionary.Count == 0)
                    return;
                foreach (var VARIABLE in taskDictionary)
                {
                    VARIABLE.Value.name = VARIABLE.Key;
                    Data?.DataList?.Add(new TaskItemViewModel()
                    {
                        Task = VARIABLE.Value
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Growls.ErrorGlobal(string.Format("LoadPipelineErrorMessage".GetLocalizationString(), ex.Message));
            }
        }
    }

    public AttributeButton? GetAttribute(string key)
    {
        foreach (var VARIABLE in Parts.Children)
        {
            if (VARIABLE is AttributeButton button)
            {
                if (button.Attribute != null && button.Attribute.Key?.Equals(key) == true)
                    return button;
            }
        }

        return null;
    }

    public void RemoveAttribute(string key)
    {
        foreach (var VARIABLE in Parts.Children)
        {
            if (VARIABLE is AttributeButton button)
            {
                if (button.Attribute != null && button.Attribute.Key?.Equals(key) == true)
                    Parts.Children.Remove(button);
            }
        }
    }

    private void CopyAttribute(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is AttributeButton attributeButton)
        {
            if (attributeButton != null && attributeButton.Attribute != null)
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                string json = JsonConvert.SerializeObject(attributeButton.Attribute, settings);
                Clipboard.SetDataObject(json);
            }
        }
    }

    private void DeleteAttribute(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is AttributeButton attributeButton)
        {
            var parentPanel = attributeButton.Parent as Panel;
            if (parentPanel != null)
            {
                var itemToDelete = attributeButton.Attribute;
                int index = parentPanel.Children.IndexOf(attributeButton);
                parentPanel.Children.Remove(attributeButton);
                Data?.UndoTaskStack?.Push(new RelayCommand(o => AddAttribute(itemToDelete, index)));
            }
        }
    }


    private void EditAttribute(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent != null)
        {
            ContextMenu? contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu?.PlacementTarget is AttributeButton attributeButton)
            {
                var editDialog = new EditAttributeDialog(attributeButton.WindowParent, attributeButton.Attribute, true);
                if (editDialog.ShowDialog() == true)
                {
                    attributeButton.Attribute = editDialog.Attribute;
                }
            }
        }
        else if (sender is AttributeButton attributeButton)
        {
            var editDialog = new EditAttributeDialog(attributeButton.WindowParent, attributeButton.Attribute, true);
            if (editDialog.ShowDialog() == true)
            {
                attributeButton.Attribute = editDialog.Attribute;
            }
        }
    }

    public AttributeButton AddAttribute(Attribute? attribute, int index = -1)
    {
        if (attribute == null)
            attribute = new Attribute()
            {
                Key = "OCR", Value = ""
            };
        AttributeButton newButton = new AttributeButton()
        {
            Margin = new Thickness(4),
            Attribute = attribute, WindowParent = this
        };
        newButton.Click += (sender, args) =>
        {
            AttributeButton? button = sender as AttributeButton;
            if (Data == null)
                return;
            if (Data.SelectedAttribute != null)
                Data.SelectedAttribute.IsSelected = false;
            if (button != null && button.IsSelected)
                Data.SelectedAttribute = button;
            else
                Data.SelectedAttribute = null;
        };
        newButton.MouseDoubleClick += EditAttribute;
        // 创建右键菜单
        ContextMenu contextMenu = new ContextMenu();
        MenuItem copyItem = new MenuItem { Header = "复制" };
        copyItem.Click += CopyAttribute;
        MenuItem editItem = new MenuItem { Header = "编辑" };
        editItem.Click += EditAttribute;
        MenuItem deleteItem = new MenuItem { Header = "删除" };
        deleteItem.Click += DeleteAttribute;
        contextMenu.Items.Add(copyItem);
        contextMenu.Items.Add(editItem);
        contextMenu.Items.Add(deleteItem);
        newButton.ContextMenu = contextMenu;
        if (index == -1)
            Parts.Children.Add(newButton);
        else
            Parts.Children.Insert(index, newButton);

        return newButton;
    }

    private void AddAttribute(object sender, RoutedEventArgs e)
    {
        Attribute attribute = new Attribute("recognition", "OCR");

        var editDialog = new EditAttributeDialog(this, attribute, false);
        if (editDialog.ShowDialog() == true)
        {
            attribute = editDialog.Attribute;
            AddAttribute(attribute);
        }
    }

    private void ScrollListBoxToBottom()
    {
        if (ListBoxDemo.Items.Count > 0)
        {
            var lastItem = ListBoxDemo.Items[ListBoxDemo.Items.Count - 1];
            ListBoxDemo.ScrollIntoView(lastItem);
        }
    }

    private void AddTask(object sender, RoutedEventArgs e)
    {
        Data?.DataList?.Add(new TaskItemViewModel()
        {
            Task = new TaskModel(), IsNew = true
        });
        ScrollListBoxToBottom();
        _chartDialog?.UpdateGraph();
    }

    private void TaskSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TaskItemViewModel? taskItemViewModel = ListBoxDemo.SelectedValue as TaskItemViewModel;
        if (taskItemViewModel != null)
            taskItemViewModel.IsNew = false;
        if (Data != null)
            Data.CurrentTask = taskItemViewModel;
    }

    public void Save_Pipeline(object? sender, RoutedEventArgs? e)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        Dictionary<string, TaskModel> taskModels = new();
        foreach (var VARIABLE in ListBoxDemo.ItemsSource)
        {
            if (VARIABLE is TaskItemViewModel taskItemViewModel)
            {
                if (taskItemViewModel.Task != null &&
                    !taskModels.TryAdd(taskItemViewModel.Name, taskItemViewModel.Task))
                {
                    Growls.WarningGlobal("SavePipelineWarning".GetLocalizationString());
                    return;
                }
            }
        }

        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = ".json",
            AddExtension = true
        };

        bool? result = saveFileDialog.ShowDialog();

        if (result == true)
        {
            string filePath = saveFileDialog.FileName;
            string jsonString = JsonConvert.SerializeObject(taskModels, settings);

            File.WriteAllText(filePath, jsonString);
            Growl.SuccessGlobal("SavePipelineSuccess".GetLocalizationString());
        }
    }

    private void OnSearchTask(object? sender, FunctionEventArgs<string> e)
    {
        var searchText = e.Info?.ToLower() ?? string.Empty;
        var filteredTasks = Data?.DataList?.Where(t =>
            t.Task?.name != null && t.Task.name.ToLower().Contains(searchText) ||
            t.Task?.recognition != null && t.Task.recognition.ToLower().Contains(searchText) ||
            t.Task?.action != null && t.Task.action.ToLower().Contains(searchText) ||
            t.Task?.next != null && t.Task.next.Any(n => n.ToLower().Contains(searchText))
        ).ToList();

        ListBoxDemo.ItemsSource = filteredTasks;
    }

    private void ClearTask(object sender, RoutedEventArgs e)
    {
        Data?.DataList?.Clear();
    }

    private void ClearAttribute(object sender, RoutedEventArgs e)
    {
        Parts.Children.Clear();
    }

    private void Cut(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is ListBoxItem item)
        {
            if (item.DataContext is TaskItemViewModel taskItemViewModel)
            {
                Clipboard.SetDataObject(taskItemViewModel.ToString());
                ListBox? listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
                if (listBox != null)
                {
                    // 获取选中项的索引
                    var index = listBox.Items.IndexOf(item.DataContext);
                    Data?.DataList?.RemoveAt(index);
                    Data?.UndoStack?.Push(new RelayCommand(o => Data?.DataList?.Insert(index, taskItemViewModel)));
                }
            }
        }
    }

    private void Copy(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is ListBoxItem item)
        {
            if (item.DataContext is TaskItemViewModel taskItemViewModel)
            {
                Clipboard.SetDataObject(taskItemViewModel.ToString());
            }
        }
    }

    private void PasteAbove(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is ListBoxItem item)
        {
            if (item.DataContext is TaskItemViewModel taskItemViewModel)
            {
                ListBox? listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
                if (listBox != null)
                {
                    // 获取选中项的索引
                    int index = listBox.Items.IndexOf(item.DataContext);
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Text))
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
                                Data?.DataList?.Insert(index, newItem);
                                Data?.UndoStack?.Push(new RelayCommand(o => Data?.DataList?.Remove(newItem)));
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
                        Growls.ErrorGlobal("ClipboardDataError".GetLocalizationString());
                    }
                }
            }
        }
    }

    private void PasteBelow(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is ListBoxItem item)
        {
            if (item.DataContext is TaskItemViewModel taskItemViewModel)
            {
                ListBox? listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
                if (listBox != null)
                {
                    // 获取选中项的索引
                    int index = listBox.Items.IndexOf(item.DataContext);
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Text))
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
                                Data?.DataList?.Insert(index + 1, newItem);
                                Data?.UndoStack?.Push(new RelayCommand(o => Data?.DataList?.Remove(newItem)));
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
                        Growls.ErrorGlobal("ClipboardDataError".GetLocalizationString());
                    }
                }
            }
        }
    }

    private void Delete(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is ListBoxItem item)
        {
            if (item.DataContext is TaskItemViewModel taskItemViewModel)
            {
                ListBox? listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
                if (listBox != null)
                {
                    // 获取选中项的索引
                    int index = listBox.Items.IndexOf(item.DataContext);
                    Data?.DataList?.RemoveAt(index);
                    Data?.UndoStack?.Push(new RelayCommand(o => Data?.DataList?.Insert(index, taskItemViewModel)));
                }
            }
        }
    }

    private void Paste(object sender, RoutedEventArgs e)
    {
        IDataObject iData = Clipboard.GetDataObject();
        if (iData.GetDataPresent(DataFormats.Text))
        {
            try
            {
                var attribute =
                    JsonConvert.DeserializeObject<Attribute>(
                        (string)iData.GetData(DataFormats.Text));
                AttributeButton button = AddAttribute(attribute);
                Data?.UndoTaskStack?.Push(new RelayCommand(o => Parts.Children.Remove(button)));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        else
        {
            Growls.ErrorGlobal("ClipboardDataError".GetLocalizationString());
        }
    }

    private void SelectionRegion(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ConnectToMAA();
        var image = MaaProcessor.Instance.GetBitmapImage();
        if (image != null)
        {
            SelectionRegionDialog imageDialog = new SelectionRegionDialog(image);
            if (imageDialog.ShowDialog() == true)
            {
                if (Data?.CurrentTask != null)
                {
                    if (imageDialog.IsRoi)
                    {
                        var attribute = GetAttribute("roi");
                        if (attribute == null)
                        {
                            AddAttribute(new Attribute()
                            {
                                Key = "roi", Value = imageDialog.Output
                            });
                        }
                        else
                        {
                            if (attribute.Attribute.Value is List<int> li)
                            {
                                if (imageDialog.Output == null)
                                {
                                    attribute.Attribute = new Attribute()
                                    {
                                        Key = "roi", Value = new List<List<int>>() { li }
                                    };
                                }
                                else
                                {
                                    attribute.Attribute = new Attribute()
                                    {
                                        Key = "roi", Value = new List<List<int>>() { li, imageDialog.Output }
                                    };
                                }
                            }
                            else if (attribute.Attribute.Value is List<List<int>> lli)
                            {
                                if (imageDialog.Output != null)
                                    lli.Add(imageDialog.Output);
                                
                                attribute.Attribute = new Attribute()
                                {
                                    Key = "roi", Value = lli
                                };
                            }
                        }
                    }
                    else
                    {
                        var attribute = GetAttribute("target");
                        if (attribute != null)
                        {
                            attribute.Attribute = new Attribute()
                            {
                                Key = "target", Value = imageDialog.Output
                            };
                        }
                        else
                        {
                            AddAttribute(new Attribute()
                            {
                                Key = "target", Value = imageDialog.Output
                            });
                        }
                    }
                }
            }
        }
    }

    private void Screenshot(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ConnectToMAA();
        var image = MaaProcessor.Instance.GetBitmapImage();
        if (image != null)
        {
            CropImageDialog imageDialog = new CropImageDialog(image);
            if (imageDialog.ShowDialog() == true)
            {
                Console.WriteLine(imageDialog.Output);
                if (Data?.CurrentTask != null)
                {
                    var attribute = GetAttribute("template");
                    if (attribute == null && imageDialog.Output != null)
                    {
                        AddAttribute(new Attribute()
                        {
                            Key = "template", Value = new List<string>() { imageDialog.Output }
                        });
                    }
                    else
                    {
                        if (attribute?.Attribute.Value is string s)
                        {
                            if (imageDialog.Output == null)
                            {
                                attribute.Attribute = new Attribute()
                                {
                                    Key = "template", Value = new List<string>() { s }
                                };
                            }
                            else
                            {
                                attribute.Attribute = new Attribute()
                                {
                                    Key = "template", Value = new List<string>() { s, imageDialog.Output }
                                };
                            }
                        }
                        else if (attribute?.Attribute.Value is List<string> ls)
                        {
                            if (imageDialog.Output != null)
                                ls.Add(imageDialog.Output);
                            attribute.Attribute = new Attribute()
                            {
                                Key = "template", Value = ls
                            };
                        }
                    }
                }
            }
        }
    }

    private void Swipe(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ConnectToMAA();
        var image = MaaProcessor.Instance.GetBitmapImage();
        if (image != null)
        {
            SwipeDialog imageDialog = new SwipeDialog(image);
            if (imageDialog.ShowDialog() == true)
            {
                if (Data?.CurrentTask != null)
                {
                    var begin = GetAttribute("begin");
                    if (begin == null)
                    {
                        AddAttribute(new Attribute()
                        {
                            Key = "begin", Value = imageDialog.OutputBegin
                        });
                    }
                    else
                    {
                        begin.Attribute = new Attribute()
                        {
                            Key = "begin", Value = imageDialog.OutputBegin
                        };
                    }

                    var end = GetAttribute("end");
                    if (end == null)
                    {
                        AddAttribute(new Attribute()
                        {
                            Key = "end", Value = imageDialog.OutputEnd
                        });
                    }
                    else
                    {
                        end.Attribute = new Attribute()
                        {
                            Key = "end", Value = imageDialog.OutputEnd
                        };
                    }
                }
            }
        }
    }

    private void ColorExtraction(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ConnectToMAA();
        var image = MaaProcessor.Instance.GetBitmapImage();
        if (image != null)
        {
            ColorExtractionDialog imageDialog = new ColorExtractionDialog(image);
            if (imageDialog.ShowDialog() == true)
            {
                if (Data?.CurrentTask != null)
                {
                    var upper = GetAttribute("upper");
                    if (upper == null)
                    {
                        AddAttribute(new Attribute()
                        {
                            Key = "upper", Value = imageDialog.OutputUpper
                        });
                    }
                    else
                    {
                        upper.Attribute = new Attribute()
                        {
                            Key = "upper", Value = imageDialog.OutputUpper
                        };
                    }

                    var lower = GetAttribute("lower");
                    if (lower == null)
                    {
                        AddAttribute(new Attribute()
                        {
                            Key = "lower", Value = imageDialog.OutputLower
                        });
                    }
                    else
                    {
                        lower.Attribute = new Attribute()
                        {
                            Key = "lower", Value = imageDialog.OutputLower
                        };
                    }
                }
            }
        }
    }

    private void RecognitionText(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ConnectToMAA();
        var image = MaaProcessor.Instance.GetBitmapImage();
        if (image != null)
        {
            RecognitionTextDialog imageDialog = new RecognitionTextDialog(image);
            if (imageDialog.ShowDialog() == true)
            {
                if (Data?.CurrentTask != null && imageDialog.Output != null)
                {
                    var attribute = GetAttribute("expected");
                    string text = OCRHelper.ReadTextFromMAARecognition(
                        imageDialog.Output[0], imageDialog.Output[1],
                        imageDialog.Output[2], imageDialog.Output[3]);
                    if (attribute == null)
                    {
                        AddAttribute(new Attribute()
                        {
                            Key = "expected", Value = new List<string>() { text }
                        });
                    }
                    else
                    {
                        if (attribute.Attribute.Value is string s)
                        {
                            attribute.Attribute = new Attribute()
                            {
                                Key = "expected", Value = new List<string>() { s, text }
                            };
                        }
                        else if (attribute.Attribute.Value is List<string> ls)
                        {
                            ls.Add(text);
                            attribute.Attribute = new Attribute()
                            {
                                Key = "expected", Value = ls
                            };
                        }
                    }
                }
            }
        }
    }
}