using MaaFramework.Binding;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Helper.ValueType;
using MFAWPF.ViewModels.UI;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFLocalizeExtension.Extensions;
using static MFAWPF.Views.UI.RootView;
using DragItemViewModel = MFAWPF.ViewModels.Tool.DragItemViewModel;
using ScrollViewer = HandyControl.Controls.ScrollViewer;


namespace MFAWPF.Views.UI;

public partial class TaskQueueView
{
    public TaskQueueViewModel ViewModel { get; set; }

    public TaskQueueView()
    {
        DataContext = this;
        ViewModel = Instances.TaskQueueViewModel;
        InitializeComponent();
    }

    public bool InitializeData(Collection<DragItemViewModel>? dragItem = null)
    {
        var (name, version, customTitle) = MaaInterface.Check();
        if (!string.IsNullOrWhiteSpace(name))
            Instances.RootViewModel.ShowResourceName(name);
        if (!string.IsNullOrWhiteSpace(version))
        {
            Instances.RootViewModel.ShowResourceVersion(version);
            Instances.VersionUpdateSettingsUserControlModel.ResourceVersion = version;
        }
        if (!string.IsNullOrWhiteSpace(customTitle))
            Instances.RootViewModel.ShowCustomTitle(customTitle);

        if (MaaInterface.Instance != null)
        {
            AppendVersionLog(MaaInterface.Instance.Version);
            Instances.TaskQueueViewModel.TasksSource.Clear();
            LoadTasks(MaaInterface.Instance.Task ?? new List<TaskInterfaceItem>(), dragItem);
        }

        ConnectToMAA();

        return LoadTask();
    }

    private bool LoadTask()
    {
        try
        {
            int filesCount = 0;
            if (Instances.GameSettingsUserControlModel.CurrentResources.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(Instances.GameSettingsUserControlModel.CurrentResource) && !string.IsNullOrWhiteSpace(Instances.GameSettingsUserControlModel.CurrentResources[0].Name))
                    Instances.GameSettingsUserControlModel.CurrentResource = Instances.GameSettingsUserControlModel.CurrentResources[0].Name;
            }
            if (Instances.GameSettingsUserControlModel.CurrentResources.Any(r => r.Name == Instances.GameSettingsUserControlModel.CurrentResource))
            {
                var resources = Instances.GameSettingsUserControlModel.CurrentResources.FirstOrDefault(r => r.Name == Instances.GameSettingsUserControlModel.CurrentResource);
                if (resources?.Path != null)
                {
                    foreach (var resourcePath in resources.Path)
                    {
                        if (!Path.Exists($"{resourcePath}/pipeline/"))
                            break;
                        filesCount = Directory.GetFiles($"{resourcePath}/pipeline", "*.json*", SearchOption.AllDirectories).Length;
                    }
                }
            }

            if (filesCount == 0)
            {
                if (!string.IsNullOrWhiteSpace($"{Instances.GameSettingsUserControlModel.CurrentResource}/pipeline") && !Directory.Exists($"{Instances.GameSettingsUserControlModel.CurrentResource}/pipeline"))
                {
                    try
                    {
                        Directory.CreateDirectory($"{Instances.GameSettingsUserControlModel.CurrentResource}/pipeline");
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError(ex);
                    }
                }

                if (!File.Exists($"{Instances.GameSettingsUserControlModel.CurrentResource}/pipeline/sample.json"))
                {
                    try
                    {
                        File.WriteAllText($"{Instances.GameSettingsUserControlModel.CurrentResource}/pipeline/sample.json",
                            JsonConvert.SerializeObject(new Dictionary<string, TaskModel>
                            {
                                {
                                    "MFAWPF", new TaskModel
                                    {
                                        Action = "DoNothing"
                                    }
                                }
                            }, new JsonSerializerSettings()
                            {
                                Formatting = Formatting.Indented,
                                NullValueHandling = NullValueHandling.Ignore,
                                DefaultValueHandling = DefaultValueHandling.Ignore
                            }));
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError(ex);
                    }
                }
            }

            // PopulateTasks();

            return true;
        }
        catch (Exception ex)
        {
            GrowlHelper.Error(string.Format(LocExtension.GetLocalizedValue<string>("PipelineLoadError"),
                ex.Message));
            Console.WriteLine(ex);
            LoggerService.LogError(ex);
            return false;
        }
    }

    private void PopulateTasks(Dictionary<string, TaskModel> taskDictionary)
    {
        // BaseTasks = taskDictionary;
        // foreach (var task in taskDictionary)
        // {
        //     task.Value.Name = task.Key;
        //     ValidateTaskLinks(taskDictionary, task);
        // }
    }

    private void ValidateTaskLinks(Dictionary<string, TaskModel> taskDictionary,
        KeyValuePair<string, TaskModel> task)
    {
        ValidateNextTasks(taskDictionary, task.Value.Next);
        ValidateNextTasks(taskDictionary, task.Value.OnError, "on_error");
        ValidateNextTasks(taskDictionary, task.Value.Interrupt, "interrupt");
    }

    private void ValidateNextTasks(Dictionary<string, TaskModel> taskDictionary,
        object? nextTasks,
        string name = "next")
    {
        if (nextTasks is List<string> tasks)
        {
            foreach (var task in tasks)
            {
                if (!taskDictionary.ContainsKey(task))
                {
                    GrowlHelper.Error(string.Format(LocExtension.GetLocalizedValue<string>("TaskNotFoundError"),
                        name, task));
                }
            }
        }
    }
    public void ConnectToMAA()
    {
        ConfigureMaaProcessorForADB();
        ConfigureMaaProcessorForWin32();
    }

    private void ConfigureMaaProcessorForADB()
    {
        if (Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb)
        {
            var adbInputType = ConfigureAdbInputTypes();
            var adbScreenCapType = ConfigureAdbScreenCapTypes();

            MaaProcessor.MaaFwConfiguration.AdbDevice.Input = adbInputType;
            MaaProcessor.MaaFwConfiguration.AdbDevice.ScreenCap = adbScreenCapType;

            LoggerService.LogInfo(
                $"{LocExtension.GetLocalizedValue<string>("AdbInputMode")}{adbInputType},{LocExtension.GetLocalizedValue<string>("AdbCaptureMode")}{adbScreenCapType}");
        }
    }

    public string ScreenshotType()
    {
        if (Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb)
            return ConfigureAdbScreenCapTypes().ToString();
        return ConfigureWin32ScreenCapTypes().ToString();
    }


    private AdbInputMethods ConfigureAdbInputTypes()
    {
        return Instances.ConnectSettingsUserControlModel.AdbControlInputType;
    }

    private AdbScreencapMethods ConfigureAdbScreenCapTypes()
    {
        return Instances.ConnectSettingsUserControlModel.AdbControlScreenCapType;
    }

    private void ConfigureMaaProcessorForWin32()
    {
        if (Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Win32)
        {
            var win32InputType = ConfigureWin32InputTypes();
            var winScreenCapType = ConfigureWin32ScreenCapTypes();

            MaaProcessor.MaaFwConfiguration.DesktopWindow.Input = win32InputType;
            MaaProcessor.MaaFwConfiguration.DesktopWindow.ScreenCap = winScreenCapType;

            LoggerService.LogInfo(
                $"{"AdbInputMode".ToLocalization()}{win32InputType},{"AdbCaptureMode".ToLocalization()}{winScreenCapType}");
        }
    }

    private Win32ScreencapMethod ConfigureWin32ScreenCapTypes()
    {
        return Instances.ConnectSettingsUserControlModel.Win32ControlScreenCapType;
    }

    private Win32InputMethod ConfigureWin32InputTypes()
    {
        return Instances.ConnectSettingsUserControlModel.Win32ControlInputType;
    }

    private bool FirstTask = true;

    private void LoadTasks(List<TaskInterfaceItem> tasks, IList<DragItemViewModel>? oldDrags = null)
    {
        var items = ConfigurationHelper.GetValue(ConfigurationKeys.TaskItems, new List<TaskInterfaceItem>()) ?? [];
        var drags = (oldDrags?.ToList() ?? []).Union(items.Select(interfaceItem => new DragItemViewModel(interfaceItem))).ToList();

        if (FirstTask)
        {
            InitializeResources();
            FirstTask = false;
        }


        var (updateList, removeList) = SynchronizeTaskItems(drags, tasks);
        updateList.RemoveAll(d => removeList.Contains(d));

        UpdateViewModels(updateList, tasks);
    }

    private void InitializeResources()
    {
        Instances.GameSettingsUserControlModel.CurrentResources =
            MaaInterface.Instance?.Resources.Values.Count > 0
                ? new ObservableCollection<MaaInterface.MaaCustomResource>(MaaInterface.Instance.Resources.Values.ToList())
                :
                [
                    new MaaInterface.MaaCustomResource
                    {
                        Name = "Default",
                        Path = [MaaProcessor.ResourceBase]
                    }
                ];
        if (Instances.GameSettingsUserControlModel.CurrentResources.Count > 0 && Instances.GameSettingsUserControlModel.CurrentResources.All(r => r.Name != Instances.GameSettingsUserControlModel.CurrentResource))
            Instances.GameSettingsUserControlModel.CurrentResource = Instances.GameSettingsUserControlModel.CurrentResources[0].Name ?? "Default";
    }

    private (List<DragItemViewModel> updateList, List<DragItemViewModel> removeList) SynchronizeTaskItems(
        IList<DragItemViewModel> drags,
        List<TaskInterfaceItem> tasks)
    {
        var newDict = tasks
            .GroupBy(t => (t.Name, t.Entry)) // 按键分组
            .ToDictionary(
                group => group.Key, // 键为 (Name, Entry)
                group => group.Last() // 值取分组中的最后一个元素
            ); // 使用 (Name, Entry) 作为键
        var removeList = new List<DragItemViewModel>();
        var updateList = new List<DragItemViewModel>();
        
        if (drags.Count == 0)
            return (updateList, removeList);
     
        foreach (var oldItem in drags)
        {
            if (newDict.TryGetValue((oldItem.Name, oldItem.InterfaceItem?.Entry), out var newItem))
            {
                UpdateExistingItem(oldItem, newItem);
                updateList.Add(oldItem);
            }
            else
            {
                var sameNameTasks = tasks.Where(t => t.Entry == oldItem.InterfaceItem?.Entry).ToList();
                if (sameNameTasks.Any())
                {
                    var firstTask = sameNameTasks.First();
                    UpdateExistingItem(oldItem, firstTask, tasks.Any(t => t.Name == firstTask.Name));
                    updateList.Add(oldItem);
                }
                else
                {
                    removeList.Add(oldItem);
                }
            }
        }
        
        return (updateList, removeList);
    }

    private void UpdateExistingItem(DragItemViewModel oldItem, TaskInterfaceItem newItem, bool updateName = false)
    {
        if (oldItem.InterfaceItem == null) return;
        if (updateName)
            oldItem.InterfaceItem.Name = newItem.Name;
        else
        {
            if (oldItem.InterfaceItem.Name != newItem.Name)
                return;
        }
        oldItem.InterfaceItem.Entry = newItem.Entry;
        oldItem.InterfaceItem.PipelineOverride = newItem.PipelineOverride;
        oldItem.InterfaceItem.Document = newItem.Document;
        oldItem.InterfaceItem.Repeatable = newItem.Repeatable;

        if (newItem.Advanced != null)
        {
            var tempDict = oldItem.InterfaceItem.Advanced?.ToDictionary(t => t.Name) ?? new Dictionary<string, MaaInterface.MaaInterfaceSelectAdvanced>();
            var maaInterfaceSelectAdvanceds = JsonConvert.DeserializeObject<List<MaaInterface.MaaInterfaceSelectAdvanced>>(JsonConvert.SerializeObject(newItem.Advanced));
            oldItem.InterfaceItem.Advanced = maaInterfaceSelectAdvanceds.Select(opt =>
            {
                if (tempDict.TryGetValue(opt.Name ?? string.Empty, out var existing))
                {
                    opt.Data = existing.Data;
                }
                return opt;
            }).ToList();
        }
        else
        {
            oldItem.InterfaceItem.Advanced = null;
        }

        if (newItem.Option != null)
        {
            var tempDict = oldItem.InterfaceItem.Option?.ToDictionary(t => t.Name) ?? new Dictionary<string, MaaInterface.MaaInterfaceSelectOption>();
            var maaInterfaceSelectOptions = JsonConvert.DeserializeObject<List<MaaInterface.MaaInterfaceSelectOption>>(JsonConvert.SerializeObject(newItem.Option));
            oldItem.InterfaceItem.Option = maaInterfaceSelectOptions.Select(opt =>
            {
                if (tempDict.TryGetValue(opt.Name ?? string.Empty, out var existing))
                {
                    opt.Index = existing.Index;
                }
                else
                {
                    SetDefaultOptionValue(opt);
                }
                return opt;
            }).ToList();
        }
        else
        {
            oldItem.InterfaceItem.Option = null;
        }
    }

    private void SetDefaultOptionValue(MaaInterface.MaaInterfaceSelectOption option)
    {
        if (!(MaaInterface.Instance?.Option?.TryGetValue(option.Name ?? string.Empty, out var interfaceOption) ?? false)) return;

        var defaultIndex = interfaceOption.Cases?
                .FindIndex(c => c.Name == interfaceOption.DefaultCase)
            ?? -1;
        if (defaultIndex != -1) option.Index = defaultIndex;
    }

    private void UpdateViewModels(IList<DragItemViewModel> drags, List<TaskInterfaceItem> tasks)
    {
        var newItems = tasks.Select(t => new DragItemViewModel(t)).ToList();
        
        foreach (var item in newItems)
        {
            if (item.InterfaceItem?.Option != null && !drags.Any())
            {
                item.InterfaceItem.Option.ForEach(SetDefaultOptionValue);
            }
        }

        ViewModel.TasksSource.AddRange(newItems);


        if (!ViewModel.TaskItemViewModels.Any())
        {
            ViewModel.TaskItemViewModels = new ObservableCollection<DragItemViewModel>(drags.Any() ? drags : newItems);
        }
    }

    public void Toggle()
    {
        if (Instances.RootViewModel.IsRunning)
        {
            Stop();
        }
        else
        {
            Start();
        }
    }

    public void Start(bool onlyStart = false, bool checkUpdate = false)
    {
        if (InitializeData())
        {
            MaaProcessor.Money = 0;
            var tasks = ViewModel.TaskItemViewModels.ToList().FindAll(task => task.IsChecked || task.IsCheckedWithNull == null);
            ConnectToMAA();
            MaaProcessor.Instance.Start(tasks, onlyStart, checkUpdate);
        }
    }

    public void Stop()
    {
        MaaProcessor.Instance.Stop();
    }

    private void Start(object sender, RoutedEventArgs e) => Start();

    private void Stop(object sender, RoutedEventArgs e) => Stop();


    private void Edit(object sender, RoutedEventArgs e)
    {
        if (Instances.ConnectingViewModel.CurrentDevice == null)
        {
            GrowlHelper.Warning(
                "Warning_CannotConnect".ToLocalizationFormatted(true, Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb
                    ? "Emulator".ToLocalization()
                    : "Window".ToLocalization()));
            return;
        }

        TaskDialog?.Show();

        Instances.RootViewModel.Idle = false;
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        foreach (var task in ViewModel.TaskItemViewModels)
            task.IsChecked = true;
    }

    private void SelectNone(object sender, RoutedEventArgs e)
    {
        foreach (var task in ViewModel.TaskItemViewModels)
            task.IsChecked = false;
    }

    private void Add(object sender, RoutedEventArgs e)
    {
        Instances.RootViewModel.Idle = false;
        var addTaskDialog = new MFAWPF.Views.UI.Dialog.AddTaskDialog(ViewModel.TasksSource);
        addTaskDialog.ShowDialog();
        if (addTaskDialog.OutputContent != null)
        {
            ViewModel.TaskItemViewModels.Add(addTaskDialog.OutputContent.Clone());
            ConfigurationHelper.SetValue(ConfigurationKeys.TaskItems, ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
    }

    private void Delete(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;

        var contextMenu = menuItem?.Parent as ContextMenu;

        if (contextMenu?.PlacementTarget is TextBlock item)
        {
            if (item.DataContext is DragItemViewModel taskItemViewModel)
            {
                int index = ViewModel.TaskItemViewModels.IndexOf(taskItemViewModel);
                ViewModel.TaskItemViewModels.RemoveAt(index);
                Instances.TaskOptionSettingsUserControl.SetOption(taskItemViewModel, false);
                ConfigurationHelper.SetValue(ConfigurationKeys.TaskItems, ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
        }
    }
    private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;

        var parent = parentObject as T;
        return parent ?? FindVisualParent<T>(parentObject);
    }

    private void TaskList_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = FindVisualParent<ScrollViewer>((DependencyObject)sender);

        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
            e.Handled = true;
        }
    }

    public void ToggleCheckBoxNullOnRightClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Right)
        {
            return;
        }

        CheckBox checkBox = (CheckBox)sender;
        checkBox.IsChecked = checkBox.IsChecked == null ? (bool?)false : null;

    }

    private void UIElement_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Right)
        {
            return;
        }
        if (sender is CheckBox checkBox)
        {
            checkBox.IsChecked = checkBox.IsChecked == null ? (bool?)false : null;
        }
        e.Handled = true;
    }
}
