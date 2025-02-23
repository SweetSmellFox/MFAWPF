using HandyControl.Controls;
using HandyControl.Data;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFLocalizeExtension.Extensions;
using ComboBox = HandyControl.Controls.ComboBox;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TextBox = HandyControl.Controls.TextBox;
using MFAWPF.Views.UI.Dialog;

namespace MFAWPF.Views.UI;

public partial class RootView
{
    public static RootView Instance { get; private set; }
    private readonly MaaToolkit _maaToolkit;

    public static ViewModels.RootViewModel ViewModel { get; set; }

    public static readonly string Version =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

    public Dictionary<string, TaskModel> TaskDictionary = new();
    public Dictionary<string, TaskModel> BaseTasks = new();

    public RootView(ViewModels.RootViewModel viewModel)
    {
        MFAConfiguration.LoadConfig();
        MFAConfiguration.MaaConfig = JsonHelper.ReadFromConfigJsonFile("maa_option", new Dictionary<string, object>());
        LanguageHelper.Initialize();
        InitializeComponent();
        ViewModel = viewModel;
        Instance = this;
        DataContext = this;
        version.Text = Version;
        Loaded += (_, _) => { LoadUI(); };
        InitializeData();
        OCRHelper.Initialize();
        ThemeHelper.UpdateThemeIndexChanged(MFAConfiguration.GetConfiguration("ThemeIndex", 0));
        StateChanged += (_, _) =>
        {
            if (MFAConfiguration.GetConfiguration("ShouldMinimizeToTray", false))
            {
                ChangeVisibility(WindowState != WindowState.Minimized);
            }
        };
        _maaToolkit = new MaaToolkit(init: true);
        MaaProcessor.Instance.TaskStackChanged += OnTaskStackChanged;
        SetIconFromExeDirectory();

    }


    private void SetIconFromExeDirectory()
    {
        string exeDirectory = AppContext.BaseDirectory;
        string iconPath = Path.Combine(exeDirectory, "logo.ico");

        if (File.Exists(iconPath))
        {
            Icon = new BitmapImage(new Uri(iconPath));
        }
    }

    public bool InitializeData(Collection<DragItemViewModel>? dragItem = null)
    {
        MaaInterface.Check();

        if (MaaInterface.Instance != null)
        {
            AppendVersionLog(MaaInterface.Instance.Version);
            ViewModel?.TasksSource.Clear();
            LoadTasks(MaaInterface.Instance.Task ?? new List<TaskInterfaceItem>(), dragItem);
        }

        ConnectToMAA();

        return LoadTask();
    }


    public bool FirstTask = true;

    private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks, Collection<DragItemViewModel>? drag = null)
    {
        foreach (var task in tasks)
        {
            var dragItem = new DragItemViewModel(task);

            if (FirstTask)
            {
                if (MaaInterface.Instance?.Resources != null && MaaInterface.Instance.Resources.Count > MFAConfiguration.GetConfiguration("ResourceIndex", 0))
                    MaaProcessor.CurrentResources =
                        MaaInterface.Instance.Resources[
                            MaaInterface.Instance.Resources.Keys.ToList()[MFAConfiguration.GetConfiguration("ResourceIndex", 0)]];
                else
                    MaaProcessor.CurrentResources =
                    [
                        MaaProcessor.ResourceBase
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

            ViewModel?.TasksSource.Add(dragItem);
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

    private void OnTaskStackChanged(object sender, EventArgs e)
    {
        ToggleTaskButtonsVisibility(isRunning: MaaProcessor.Instance.TaskQueue.Count > 0);
    }

    private void ChangeVisibility(bool visible)
    {
        Visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    public void ToggleTaskButtonsVisibility(bool isRunning)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            if (ViewModel is not null)
                ViewModel.IsRunning = isRunning;
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = !ConfirmExit();
        MFAConfiguration.SetConfiguration("TaskItems", ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        base.OnClosed(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Application.Current.Shutdown();
    }


    private bool LoadTask()
    {
        try
        {
            var taskDictionary = new Dictionary<string, TaskModel>();
            if (MaaProcessor.CurrentResources != null)
            {
                foreach (var resourcePath in MaaProcessor.CurrentResources)
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

    public void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void Collapse()
    {
        WindowState = WindowState.Minimized;
    }

    public void SwitchWindowState()
    {
        Console.WriteLine(WindowState);
        if (WindowState == WindowState.Minimized)
        {
            ShowWindow();
        }
        else
        {
            Collapse();
        }
    }


    public void Start(bool onlyStart = false, bool checkUpdate = false)
    {
        if (ViewModel.Idle == false)
        {
            GrowlHelper.Warning("CannotStart".ToLocalization());
            return;
        }
        if (InitializeData())
        {
            MaaProcessor.Money = 0;
            var tasks = ViewModel?.TaskItemViewModels.ToList().FindAll(task => task.IsChecked);
            ConnectToMAA();
            MaaProcessor.Instance.Start(tasks, onlyStart, checkUpdate);
        }
    }

    public void Stop()
    {
        MaaProcessor.Instance.Stop();
    }

    private async void ConnectionTabControlOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.IsAdb = ConnectingView.AdbTab.IsSelected;
            ConnectingView.ButtonCustom.Visibility = ConnectingView.AdbTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        AutoDetectDevice();

        MaaProcessor.Instance.SetCurrentTasker();
        if (TaskQueueView.ConnectSettingButton.IsChecked.IsTrue())
        {
            TaskQueueView.ConfigureSettingsPanel();
        }
    }

    public void DeviceComboBox_OnSelectionChanged()
    {
        if (ConnectingView.DeviceComboBox.SelectedItem is DesktopWindowInfo window)
        {
            Growl.Info(string.Format(LocExtension.GetLocalizedValue<string>("WindowSelectionMessage"),
                window.Name));
            MaaProcessor.MaaFwConfig.DesktopWindow.Name = window.Name;
            MaaProcessor.MaaFwConfig.DesktopWindow.HWnd = window.Handle;
            MaaProcessor.Instance.SetCurrentTasker();
        }
        else if (ConnectingView.DeviceComboBox.SelectedItem is AdbDeviceInfo device)
        {
            Growl.Info(string.Format(LocExtension.GetLocalizedValue<string>("EmulatorSelectionMessage"),
                device.Name));
            MaaProcessor.MaaFwConfig.AdbDevice.Name = device.Name;
            MaaProcessor.MaaFwConfig.AdbDevice.AdbPath = device.AdbPath;
            MaaProcessor.MaaFwConfig.AdbDevice.AdbSerial = device.AdbSerial;
            MaaProcessor.MaaFwConfig.AdbDevice.Config = device.Config;
            MaaProcessor.Instance.SetCurrentTasker();
            MFAConfiguration.SetConfiguration("AdbDevice", device);
        }
    }


    public void CustomAdb()
    {
        var deviceInfo =
            ConnectingView.DeviceComboBox.Items.Count > 0 && ConnectingView.DeviceComboBox.SelectedItem is AdbDeviceInfo device
                ? device
                : null;
        var dialog = new AdbEditorDialog(deviceInfo);
        if (dialog.ShowDialog().IsTrue())
        {
            ConnectingView.DeviceComboBox.ItemsSource = new List<AdbDeviceInfo>
            {
                dialog.Output
            };
            ConnectingView.DeviceComboBox.SelectedIndex = 0;
            SetConnected(true);
        }
    }

    public bool IsFirstStart = true;

    public bool TryGetIndexFromConfig(string config, out int index)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("extras", out JsonElement extras) && extras.TryGetProperty("mumu", out JsonElement mumu) && mumu.TryGetProperty("index", out JsonElement indexElement))
            {
                index = indexElement.GetInt32();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析 Config 时出错: {ex.Message}");
            LoggerService.LogError(ex);
        }

        index = 0;
        return false;
    }

    public static int ExtractNumberFromEmulatorConfig(string emulatorConfig)
    {
        var match = Regex.Match(emulatorConfig, @"\d+");

        if (match.Success)
        {
            return int.Parse(match.Value);
        }

        return 0;
    }

    public void AutoDetectDevice()
    {
        try
        {
            Growl.Info((ViewModel?.IsAdb).IsTrue()
                ? LocExtension.GetLocalizedValue<string>("EmulatorDetectionStarted")
                : LocExtension.GetLocalizedValue<string>("WindowDetectionStarted"));
            SetConnected(false);
            if ((ViewModel?.IsAdb).IsTrue())
            {
                var devices = _maaToolkit.AdbDevice.Find();
                DispatcherHelper.RunOnMainThread(() => ConnectingView.DeviceComboBox.ItemsSource = devices);
                SetConnected(devices.Count > 0);
                var emulatorConfig = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);
                var resultIndex = 0;
                if (!string.IsNullOrWhiteSpace(emulatorConfig))
                {
                    var extractedNumber = ExtractNumberFromEmulatorConfig(emulatorConfig);

                    foreach (var device in devices)
                    {
                        if (TryGetIndexFromConfig(device.Config, out int index))
                        {
                            if (index == extractedNumber)
                            {
                                resultIndex = devices.IndexOf(device);
                                break;
                            }
                        }
                        else resultIndex = 0;
                    }
                }
                else
                    resultIndex = 0;
                DispatcherHelper.RunOnMainThread(() => ConnectingView.DeviceComboBox.SelectedIndex = resultIndex);
                if (MaaInterface.Instance?.Controller != null)
                {
                    if (MaaInterface.Instance.Controller.Any(controller => controller.Type != null && controller.Type.ToLower().Equals("adb")))
                    {
                        var adbController = MaaInterface.Instance.Controller.FirstOrDefault(controller => controller.Type != null && controller.Type.ToLower().Equals("adb"));
                        if (adbController?.Adb != null)
                        {
                            if (adbController.Adb.Input != null)
                            {
                                int result = 3;
                                switch (adbController.Adb.Input)
                                {
                                    case 1:
                                        result = 2;
                                        break;
                                    case 2:
                                        result = 0;
                                        break;
                                    case 4:
                                        result = 1;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("AdbControlInputType", result);
                            }
                            if (adbController.Adb.ScreenCap != null)
                            {
                                int result = 0;
                                switch (adbController.Adb.ScreenCap)
                                {
                                    case 1:
                                        result = 4;
                                        break;
                                    case 2:
                                        result = 3;
                                        break;
                                    case 4:
                                        result = 1;
                                        break;
                                    case 8:
                                        result = 2;
                                        break;
                                    case 16:
                                        result = 5;
                                        break;
                                    case 32:
                                        result = 6;
                                        break;
                                    case 64:
                                        result = 7;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("AdbControlScreenCapType", result);
                            }
                        }
                    }
                }
            }
            else
            {
                var windows = _maaToolkit.Desktop.Window.Find().ToList();
                DispatcherHelper.RunOnMainThread(() => ConnectingView.DeviceComboBox.ItemsSource = windows);
                SetConnected(windows.Count > 0);
                var resultIndex = windows.Count > 0
                    ? windows.ToList().FindIndex(win => !string.IsNullOrWhiteSpace(win.Name))
                    : 0;
                if (MaaInterface.Instance?.Controller != null)
                {
                    if (MaaInterface.Instance.Controller.Any(controller => controller.Type != null && controller.Type.ToLower().Equals("win32")))
                    {
                        var win32Controller = MaaInterface.Instance.Controller.FirstOrDefault(controller => controller.Type != null && controller.Type.ToLower().Equals("win32"));
                        if (win32Controller?.Win32 != null)
                        {
                            var filteredWindows = windows.Where(win => !string.IsNullOrWhiteSpace(win.Name) || !string.IsNullOrWhiteSpace(win.ClassName)).ToList();

                            if (!string.IsNullOrWhiteSpace(win32Controller.Win32.WindowRegex))
                            {
                                var windowRegex = new Regex(win32Controller.Win32.WindowRegex);
                                filteredWindows = filteredWindows.Where(win => windowRegex.IsMatch(win.Name)).ToList();
                                resultIndex = filteredWindows.Count > 0
                                    ? windows.FindIndex(win => win.Name.Equals(filteredWindows.First().Name))
                                    : 0;
                            }

                            if (!string.IsNullOrWhiteSpace(win32Controller.Win32.ClassRegex))
                            {
                                var classRegex = new Regex(win32Controller.Win32.ClassRegex);
                                var filteredWindowsByClass = filteredWindows.Where(win => classRegex.IsMatch(win.ClassName)).ToList();
                                resultIndex = filteredWindowsByClass.Count > 0
                                    ? windows.FindIndex(win => win.ClassName.Equals(filteredWindowsByClass.First().ClassName))
                                    : 0;
                            }

                            if (win32Controller.Win32.Input != null)
                            {
                                int result = 0;
                                switch (win32Controller.Win32.Input)
                                {
                                    case 1:
                                        result = 0;
                                        break;
                                    case 2:
                                        result = 1;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("Win32ControlInputType", result);
                            }
                            if (win32Controller.Win32.ScreenCap != null)
                            {
                                int result = 0;
                                switch (win32Controller.Win32.ScreenCap)
                                {
                                    case 1:
                                        result = 2;
                                        break;
                                    case 2:
                                        result = 0;
                                        break;
                                    case 4:
                                        result = 1;
                                        break;
                                }
                                MFAConfiguration.SetConfiguration("Win32ControlScreenCapType", result);
                            }
                        }
                    }
                }
                DispatcherHelper.RunOnMainThread(() => ConnectingView.DeviceComboBox.SelectedIndex = resultIndex);
            }

            if (!IsConnected())
            {
                Growl.Info((ViewModel?.IsAdb).IsTrue()
                    ? LocExtension.GetLocalizedValue<string>("NoEmulatorFound")
                    : LocExtension.GetLocalizedValue<string>("NoWindowFound"));
            }
        }
        catch (Exception ex)
        {
            GrowlHelper.Warning(string.Format(LocExtension.GetLocalizedValue<string>("TaskStackError"),
                (ViewModel?.IsAdb).IsTrue() ? "Emulator".ToLocalization() : "Window".ToLocalization(),
                ex.Message));
            SetConnected(false);
            LoggerService.LogError(ex);
            Console.WriteLine(ex);
        }
    }

    public void RestartMFA()
    {
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        DispatcherHelper.RunOnMainThread(Application.Current.Shutdown);
    }


    private static EditTaskDialog? _taskDialog;

    public static EditTaskDialog TaskDialog
    {
        get
        {

            _taskDialog ??= new EditTaskDialog();
            return _taskDialog;
        }
        set => _taskDialog = value;
    }


    public void ConnectToMAA()
    {
        ConfigureMaaProcessorForADB();
        ConfigureMaaProcessorForWin32();
    }

    private void ConfigureMaaProcessorForADB()
    {
        if ((ViewModel?.IsAdb).IsTrue())
        {
            var adbInputType = ConfigureAdbInputTypes();
            var adbScreenCapType = ConfigureAdbScreenCapTypes();

            MaaProcessor.MaaFwConfig.AdbDevice.Input = adbInputType;
            MaaProcessor.MaaFwConfig.AdbDevice.ScreenCap = adbScreenCapType;

            Console.WriteLine(
                $"{LocExtension.GetLocalizedValue<string>("AdbInputMode")}{adbInputType},{LocExtension.GetLocalizedValue<string>("AdbCaptureMode")}{adbScreenCapType}");
        }
    }

    public string ScreenshotType()
    {
        if ((ViewModel?.IsAdb).IsTrue())
            return ConfigureAdbScreenCapTypes().ToString();
        return ConfigureWin32ScreenCapTypes().ToString();
    }


    private AdbInputMethods ConfigureAdbInputTypes()
    {
        return MFAConfiguration.GetConfiguration("AdbControlInputType", "MinitouchAndAdbKey") switch
        {
            "MiniTouch" => AdbInputMethods.MinitouchAndAdbKey,
            "MaaTouch" => AdbInputMethods.Maatouch,
            "AdbInput" => AdbInputMethods.AdbShell,
            "AutoDetect" => AdbInputMethods.All,
            _ => AdbInputMethods.MinitouchAndAdbKey
        };
    }

    private AdbScreencapMethods ConfigureAdbScreenCapTypes()
    {
        return MFAConfiguration.GetConfiguration("AdbControlScreenCapType", "Default") switch
        {
            "Default" => AdbScreencapMethods.Default,
            "RawWithGzip" => AdbScreencapMethods.RawWithGzip,
            "RawByNetcat" => AdbScreencapMethods.RawByNetcat,
            "Encode" => AdbScreencapMethods.Encode,
            "EncodeToFileAndPull" => AdbScreencapMethods.EncodeToFileAndPull,
            "MinicapDirect" => AdbScreencapMethods.MinicapDirect,
            "MinicapStream" => AdbScreencapMethods.MinicapStream,
            "EmulatorExtras" => AdbScreencapMethods.EmulatorExtras,
            _ => AdbScreencapMethods.Default
        };
    }

    private void ConfigureMaaProcessorForWin32()
    {
        if (!(ViewModel?.IsAdb).IsTrue())
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
        return MFAConfiguration.GetConfiguration("Win32ControlScreenCapType", "FramePool") switch
        {
            "FramePool" => Win32ScreencapMethod.FramePool,
            "DXGIDesktopDup" => Win32ScreencapMethod.DXGIDesktopDup,
            "GDI" => Win32ScreencapMethod.GDI,
            _ => Win32ScreencapMethod.FramePool
        };
    }

    private Win32InputMethod ConfigureWin32InputTypes()
    {
        return MFAConfiguration.GetConfiguration("Win32ControlInputType", "Seize") switch
        {
            "Seize" => Win32InputMethod.Seize,
            "SendMessage" => Win32InputMethod.SendMessage,
            _ => Win32InputMethod.Seize
        };
    }


    public void ShowResourceName(string name)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            resourceName.Visibility = Visibility.Visible;
            resourceNameText.Visibility = Visibility.Visible;
            resourceName.Text = name;
        });
    }

    public void ShowResourceVersion(string v)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            resourceVersion.Visibility = Visibility.Visible;
            resourceVersionText.Visibility = Visibility.Visible;
            resourceVersion.Text = v;
        });
    }

    public void ShowCustomTitle(string v)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            title.Visibility = Visibility.Collapsed;
            version.Visibility = Visibility.Collapsed;
            resourceName.Visibility = Visibility.Collapsed;
            resourceNameText.Visibility = Visibility.Collapsed;
            resourceVersionText.Visibility = Visibility.Collapsed;
            customTitle.Visibility = Visibility.Visible;
            customTitle.Text = v;
        });
    }

    public void LoadUI()
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            InitializationSettings();
            ConnectingView.ConnectionTabControl.SelectedIndex = MaaInterface.Instance?.DefaultController == "win32" ? 1 : 0;
            if (MFAConfiguration.GetConfiguration("AutoStartIndex", 0) >= 1)
            {
                MaaProcessor.Instance.TaskQueue.Push(new MFATask
                {
                    Name = "启动前",
                    Type = MFATask.MFATaskType.MFA,
                    Action = WaitSoftware,
                });
                Start(MFAConfiguration.GetConfiguration("AutoStartIndex", 0) == 1, checkUpdate: true);
            }
            else
            {
                if (ViewModel?.IsAdb == true && MFAConfiguration.GetConfiguration("RememberAdb", true) && "adb".Equals(MaaProcessor.MaaFwConfig.AdbDevice.AdbPath) && MFAConfiguration.TryGetData<JObject>("AdbDevice", out var jObject))
                {
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new AdbInputMethodsConverter());
                    settings.Converters.Add(new AdbScreencapMethodsConverter());

                    var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
                    if (device != null)
                    {
                        DispatcherHelper.RunOnMainThread(() =>
                        {
                            ConnectingView.DeviceComboBox.ItemsSource = new List<AdbDeviceInfo>
                            {
                                device
                            };
                            ConnectingView.DeviceComboBox.SelectedIndex = 0;
                            SetConnected(true);
                        });
                    }
                }
                VersionChecker.Check();
            }
            ConnectingView.ConnectionTabControl.SelectionChanged += ConnectionTabControlOnSelectionChanged;
            if (ViewModel != null)
                ViewModel.NotLock = MaaInterface.Instance?.LockController != true;
            // TaskQueueView.ConnectSettingButton.IsChecked = true;
            MFAConfiguration.SetConfiguration("EnableEdit", MFAConfiguration.GetConfiguration("EnableEdit", false));
            ViewModel.IsDebugMode = MFAExtensions.IsDebugMode();
            if (!string.IsNullOrWhiteSpace(MaaInterface.Instance?.Message))
            {
                Growl.Info(MaaInterface.Instance.Message);
            }

        });
        TaskManager.RunTaskAsync(async () =>
        {
            await Task.Delay(1000);
            DispatcherHelper.RunOnMainThread(() =>
            {
                if (MFAConfiguration.GetConfiguration("AutoMinimize", false))
                {
                    Collapse();
                }

                if (MFAConfiguration.GetConfiguration("AutoHide", false))
                {
                    Hide();
                }
            });
        });
    }

    public static void AppendVersionLog(string? resourceVersion)
    {
        if (resourceVersion is null)
        {
            return;
        }
        string debugFolderPath = Path.Combine(AppContext.BaseDirectory, "debug");
        if (!Directory.Exists(debugFolderPath))
        {
            Directory.CreateDirectory(debugFolderPath);
        }

        string logFilePath = Path.Combine(debugFolderPath, "maa.log");
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formattedLogMessage =
            $"[{timeStamp}][INF][Px14600][Tx16498][Parser.cpp][L56][MaaNS::ProjectInterfaceNS::Parser::parse_interface] ";
        var logMessage = $"MFAWPF Version: [mfa.version={Version}] "
            + (resourceVersion is null
                ? ""
                : $"Interface Version: [data.version=v{resourceVersion.Replace("v", "")}] ");
        LoggerService.LogInfo(logMessage);

        try
        {
            File.AppendAllText(logFilePath, formattedLogMessage + logMessage);
        }
        catch (Exception)
        {
            Console.WriteLine("尝试写入失败！");
        }
    }

    private void ToggleWindowTopMost(object sender, RoutedPropertyChangedEventArgs<bool> e)
    {
        Topmost = e.NewValue;
    }

    public static void AddLogByColor(string content,
        string color = "Gray",
        string weight = "Regular",
        bool showTime = true)
    {
        ViewModel?.AddLog(content, color, weight, showTime);
    }

    public static void AddLog(string content,
        Brush? color = null,
        string weight = "Regular",
        bool showTime = true)
    {
        ViewModel?.AddLog(content, color, weight, showTime);
    }

    public static void AddLogByKey(string key, Brush? color = null, params string[] formatArgsKeys)
    {
        ViewModel?.AddLogByKey(key, color, formatArgsKeys);
    }

    public void RunScript(string str = "Prescript")
    {
        bool enable = str switch
        {
            "Prescript" => !string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("Prescript", string.Empty)),
            "Post-script" => !string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("Post-script", string.Empty)),
            _ => false,
        };
        if (!enable)
        {
            return;
        }

        Func<bool> func = str switch
        {
            "Prescript" => () => ExecuteScript(MFAConfiguration.GetConfiguration("Prescript", string.Empty)),
            "Post-script" => () => ExecuteScript(MFAConfiguration.GetConfiguration("Post-script", string.Empty)),
            _ => () => false,
        };

        if (!func())
        {
            LoggerService.LogError($"Failed to execute the {str}.");
        }
    }

    private static bool ExecuteScript(string scriptPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return false;
            }

            string fileName;
            string arguments;

            if (scriptPath.StartsWith('\"'))
            {
                var parts = scriptPath.Split("\"", 3);
                fileName = parts[1];
                arguments = parts.Length > 2 ? parts[2] : string.Empty;
            }
            else
            {
                fileName = scriptPath;
                arguments = string.Empty;
            }

            bool createNoWindow = arguments.Contains("-noWindow");
            bool minimized = arguments.Contains("-minimized");

            if (createNoWindow)
            {
                arguments = arguments.Replace("-noWindow", string.Empty).Trim();
            }

            if (minimized)
            {
                arguments = arguments.Replace("-minimized", string.Empty).Trim();
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WindowStyle = minimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal,
                    CreateNoWindow = createNoWindow,
                    UseShellExecute = !createNoWindow,
                },
            };
            process.Start();
            process.WaitForExit();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void InitializationSettings()
    {
        var settingsView = App.Services.GetRequiredService<SettingsView>();

        SettingViewBorder.Child = settingsView;
    }

    public async void WaitSoftware()
    {
        if (MFAConfiguration.GetConfiguration("AutoStartIndex", 0) >= 1)
        {
            MaaProcessor.Instance.StartSoftware();
        }

        if ((ViewModel?.IsAdb).IsTrue() && MFAConfiguration.GetConfiguration("RememberAdb", true) && "adb".Equals(MaaProcessor.MaaFwConfig.AdbDevice.AdbPath) && MFAConfiguration.TryGetData<JObject>("AdbDevice", out var jObject))
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new AdbInputMethodsConverter());
            settings.Converters.Add(new AdbScreencapMethodsConverter());

            var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
            if (device != null)
            {
                DispatcherHelper.RunOnMainThread(() =>
                {
                    ConnectingView.DeviceComboBox.ItemsSource = new List<AdbDeviceInfo>
                    {
                        device
                    };
                    ConnectingView.DeviceComboBox.SelectedIndex = 0;
                    SetConnected(true);
                });
            }
        }
        else
            DispatcherHelper.RunOnMainThread(AutoDetectDevice);
    }
    public bool IsConnected()
    {
        return (ViewModel?.IsConnected).IsTrue();
    }

    public void SetConnected(bool isConnected)
    {
        if (ViewModel == null) return;
        ViewModel.IsConnected = isConnected;
    }

    public void SetUpdating(bool isUpdating)
    {
        if (ViewModel == null) return;
        ViewModel.IsUpdating = isUpdating;
    }

    public bool ConfirmExit()
    {
        if (!ViewModel.IsRunning)
            return true;
        var result = MessageBoxHelper.Show("ConfirmExitText".ToLocalization(),
            "ConfirmExitTitle".ToLocalization(), buttons: MessageBoxButton.YesNo, icon: MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public void ClearTasks(Action? action = null)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            ViewModel.TaskItemViewModels = new();
            action?.Invoke();
        });
    }
}
