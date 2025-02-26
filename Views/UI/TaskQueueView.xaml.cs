using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Helper.ValueType;
using MFAWPF.ViewModels.UI;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
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
    public Dictionary<string, TaskModel> BaseTasks = new();
    public Dictionary<string, TaskModel> TaskDictionary = new();
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
            var taskDictionary = new Dictionary<string, TaskModel>();
            if (Instances.GameSettingsUserControlModel.CurrentResources.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(Instances.GameSettingsUserControlModel.CurrentResource) && !string.IsNullOrWhiteSpace(Instances.GameSettingsUserControlModel.CurrentResources[0].Name))
                    Instances.GameSettingsUserControlModel.CurrentResource = Instances.GameSettingsUserControlModel.CurrentResources[0].Name;
            }
            if (Instances.GameSettingsUserControlModel.CurrentResources.Any(r => r.Name == Instances.GameSettingsUserControlModel.CurrentResource))
            {
                var resources = Instances.GameSettingsUserControlModel.CurrentResources.Where(r => r.Name == Instances.GameSettingsUserControlModel.CurrentResource);
                foreach (var resourcePath in resources)
                {
                    if (!Path.Exists($"{resourcePath}/pipeline/"))
                        break;
                    var jsonFiles = Directory.GetFiles($"{resourcePath}/pipeline/", "*.json", SearchOption.AllDirectories);
                    var taskDictionaryA = new Dictionary<string, TaskModel>();
                    foreach (var file in jsonFiles)
                    {
                        var content = File.ReadAllText(file);
                        var taskData = JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(content);
                        if (taskData == null || taskData.Count == 0)
                            break;
                        foreach (var task in taskData)
                        {
                            if (!taskDictionaryA.TryAdd(task.Key, task.Value))
                            {
                                GrowlHelper.Error(string.Format(
                                    LocExtension.GetLocalizedValue<string>("DuplicateTaskError"), task.Key));
                                return false;
                            }
                        }
                    }

                    taskDictionary = taskDictionary.MergeTaskModels(taskDictionaryA);
                }
            }

            if (taskDictionary.Count == 0)
            {

                if (!string.IsNullOrWhiteSpace($"{MaaProcessor.ResourceBase}/pipeline") && !Directory.Exists($"{MaaProcessor.ResourceBase}/pipeline"))
                {
                    try
                    {
                        Directory.CreateDirectory($"{MaaProcessor.ResourceBase}/pipeline");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"创建目录时发生错误: {ex.Message}");
                        LoggerService.LogError(ex);
                    }
                }

                if (!File.Exists($"{MaaProcessor.ResourceBase}/pipeline/sample.json"))
                {
                    try
                    {
                        File.WriteAllText($"{MaaProcessor.ResourceBase}/pipeline/sample.json",
                            JsonConvert.SerializeObject(new Dictionary<string, TaskModel>
                            {
                                {
                                    "MFAWPF", new TaskModel()
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
                        Console.WriteLine($"创建文件时发生错误: {ex.Message}");
                        LoggerService.LogError(ex);
                    }
                }
            }

            PopulateTasks(taskDictionary);

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
        BaseTasks = taskDictionary;
        foreach (var task in taskDictionary)
        {
            task.Value.Name = task.Key;
            ValidateTaskLinks(taskDictionary, task);
        }
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

            MaaProcessor.MaaFwConfig.AdbDevice.Input = adbInputType;
            MaaProcessor.MaaFwConfig.AdbDevice.ScreenCap = adbScreenCapType;

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

            MaaProcessor.MaaFwConfig.DesktopWindow.Input = win32InputType;
            MaaProcessor.MaaFwConfig.DesktopWindow.ScreenCap = winScreenCapType;

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

    public bool FirstTask = true;

    private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks, Collection<DragItemViewModel>? drag = null)
    {
        foreach (var task in tasks)
        {
            var dragItem = new DragItemViewModel(task);

            if (FirstTask)
            {
                if (MaaInterface.Instance?.Resources != null && MaaInterface.Instance.Resources.Count > 0)
                    Instances.GameSettingsUserControlModel.CurrentResources = new ObservableCollection<MaaInterface.MaaCustomResource>(MaaInterface.Instance.Resources.Values.ToList());
                else
                    Instances.GameSettingsUserControlModel.CurrentResources =
                    [
                        new MaaInterface.MaaCustomResource
                        {
                            Name = "Default",
                            Path = [MaaProcessor.ResourceBase]
                        }
                    ];
                FirstTask = false;
            }
            if (drag != null)
            {
                var oldDict = drag
                    .Where(vm => vm.InterfaceItem?.Name != null)
                    .ToDictionary(vm => vm.InterfaceItem.Name);

                foreach (var newItem in tasks)
                {
                    if (newItem?.Name == null) continue;

                    if (oldDict.TryGetValue(newItem.Name, out var oldVm))
                    {
                        var oldItem = oldVm.InterfaceItem;
                        if (oldItem == null) continue;

                        if (oldItem.Check.HasValue)
                        {
                            newItem.Check = oldItem.Check;
                        }

                        if (oldItem.Option != null && newItem.Option != null)
                        {
                            foreach (var newOption in newItem.Option)
                            {
                                var oldOption = oldItem.Option.FirstOrDefault(o =>
                                    o.Name == newOption.Name && o.Index.HasValue);

                                if (oldOption != null)
                                {

                                    int maxValidIndex = newItem.Option.Count - 1;
                                    int desiredIndex = oldOption.Index ?? 0;

                                    newOption.Index = (desiredIndex >= 0 && desiredIndex <= maxValidIndex)
                                        ? desiredIndex
                                        : 0;
                                }
                            }
                        }
                    }
                }
            }
            else if (dragItem.InterfaceItem?.Option != null)
            {
                foreach (var option in dragItem.InterfaceItem.Option)
                {
                    if (MaaInterface.Instance?.Option != null && MaaInterface.Instance.Option.TryGetValue(option.Name, out var interfaceOption))
                    {
                        if (interfaceOption.Cases != null)
                        {
                            if (!string.IsNullOrWhiteSpace(interfaceOption.DefaultCase) && interfaceOption.Cases != null)
                            {

                                var index = interfaceOption.Cases.FindIndex(@case => @case.Name == interfaceOption.DefaultCase);
                                if (index != -1)
                                {
                                    option.Index = index;
                                }

                            }
                        }
                    }

                }
            }

            ViewModel.TasksSource.Add(dragItem);
        }

        if (ViewModel.TaskItemViewModels.Count == 0)
        {
            var items = MFAConfiguration.GetConfiguration("TaskItems",
                    new List<TaskInterfaceItem>())
                ?? new List<TaskInterfaceItem>();
            var dragItemViewModels = items.Select(interfaceItem => new DragItemViewModel(interfaceItem)).ToList();
            var tempViewModel = new ObservableCollection<DragItemViewModel>();
            tempViewModel.AddRange(dragItemViewModels);
            if (tempViewModel.Count == 0 && ViewModel.TasksSource.Count != 0)
            {
                foreach (var item in ViewModel.TasksSource)
                    tempViewModel.Add(item);
            }
            ViewModel.TaskItemViewModels = tempViewModel;
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
        if (!Instances.RootViewModel.Idle)
        {
            GrowlHelper.Warning("CannotStart".ToLocalization());
            return;
        }
        if (InitializeData())
        {
            MaaProcessor.Money = 0;
            var tasks = ViewModel.TaskItemViewModels.ToList().FindAll(task => task.IsChecked);
            ConnectToMAA();
            MaaProcessor.Instance.Start(tasks, onlyStart, checkUpdate);
        }
    }

    public void Stop()
    {
        MaaProcessor.Instance.Stop();
    }

    public void Start(object sender, RoutedEventArgs e) => Start();

    public void Stop(object sender, RoutedEventArgs e) => Stop();


    private void Edit(object sender, RoutedEventArgs e)
    {
        if (Instances.ConnectingViewModel.CurrentDevice == null)
        {
            GrowlHelper.Warning(
                "Warning_CannotConnect".ToLocalizationFormatted(Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb
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
            MFAConfiguration.SetConfiguration("TaskItems", ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
    }
    private void Delete(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        var contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is Grid item)
        {
            if (item.DataContext is DragItemViewModel taskItemViewModel)
            {
                int index = ViewModel.TaskItemViewModels.IndexOf(taskItemViewModel);
                ViewModel.TaskItemViewModels.RemoveAt(index);
                Instances.TaskOptionSettingsUserControl.SetOption(taskItemViewModel, false);
                MFAConfiguration.SetConfiguration("TaskItems", ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
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
}
