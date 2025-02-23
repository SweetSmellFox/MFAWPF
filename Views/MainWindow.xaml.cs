using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Themes;
using HandyControl.Tools.Extension;
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
using System.Configuration;
using System.Windows.Controls.Primitives;
using WPFLocalizeExtension.Extensions;
using ComboBox = HandyControl.Controls.ComboBox;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TextBox = HandyControl.Controls.TextBox;

namespace MFAWPF.Views;

public partial class MainWindow
{
    public static MainWindow Instance { get; private set; }
    private readonly MaaToolkit _maaToolkit;

    public static ViewModels.MainViewModel ViewModel { get; set; }

    public static readonly string Version =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

    public Dictionary<string, TaskModel> TaskDictionary = new();
    public Dictionary<string, TaskModel> BaseTasks = new();

    public MainWindow(ViewModels.MainViewModel viewModel)
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

    private void TaskList_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = FindVisualParent<ScrollViewer>((DependencyObject)sender);

        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
            e.Handled = true;
        }
    }

    private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;

        var parent = parentObject as T;
        return parent ?? FindVisualParent<T>(parentObject);
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

    public void Start(object sender, RoutedEventArgs e) => Start();

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

    public void Stop(object sender, RoutedEventArgs e) => Stop();

    public void Stop()
    {
        MaaProcessor.Instance.Stop();
    }

    private async void ConnectionTabControlOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.IsAdb = adbTab.IsSelected;
            btnCustom.Visibility = adbTab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        AutoDetectDevice();

        MaaProcessor.Instance.SetCurrentTasker();
        if (ConnectSettingButton.IsChecked.IsTrue())
        {
            ConfigureSettingsPanel();
        }
    }

    private void DeviceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (deviceComboBox.SelectedItem is DesktopWindowInfo window)
        {
            Growl.Info(string.Format(LocExtension.GetLocalizedValue<string>("WindowSelectionMessage"),
                window.Name));
            MaaProcessor.MaaFwConfig.DesktopWindow.Name = window.Name;
            MaaProcessor.MaaFwConfig.DesktopWindow.HWnd = window.Handle;
            MaaProcessor.Instance.SetCurrentTasker();
        }
        else if (deviceComboBox.SelectedItem is AdbDeviceInfo device)
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

    private void Refresh(object sender, RoutedEventArgs e)
    {
        AutoDetectDevice();
    }

    private void CustomAdb(object sender, RoutedEventArgs e)
    {
        var deviceInfo =
            deviceComboBox.Items.Count > 0 && deviceComboBox.SelectedItem is AdbDeviceInfo device
                ? device
                : null;
        AdbEditorDialog dialog = new AdbEditorDialog(deviceInfo);
        if (dialog.ShowDialog().IsTrue())
        {
            deviceComboBox.ItemsSource = new List<AdbDeviceInfo>
            {
                dialog.Output
            };
            deviceComboBox.SelectedIndex = 0;
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
                DispatcherHelper.RunOnMainThread(() => deviceComboBox.ItemsSource = devices);
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
                DispatcherHelper.RunOnMainThread(() => deviceComboBox.SelectedIndex = resultIndex);
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
                DispatcherHelper.RunOnMainThread(() => deviceComboBox.ItemsSource = windows);
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
                DispatcherHelper.RunOnMainThread(() => deviceComboBox.SelectedIndex = resultIndex);
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

    private void ConfigureSettingsPanel(object sender = null, RoutedEventArgs e = null)
    {
        settingPanel.Children.Clear();


        // var tabControl = new TabControl
        // {
        //     TabStripPlacement = Dock.Bottom,
        //     Height = 400,
        //     Background = Brushes.Transparent,
        //     BorderThickness = new Thickness(0),
        //     VerticalAlignment = VerticalAlignment.Stretch,
        //     Style = (Style)FindResource("TabControlCapsule")
        // };
        // Binding heightBinding = new Binding("ActualHeight")
        // {
        //     Source = settingPanel,
        //     Converter = new SubtractConverter(),
        //     ConverterParameter = "20"
        // };
        // tabControl.SetBinding(HeightProperty, heightBinding);

        StackPanel s1 = new()
            {
                Margin = new Thickness(2)
            },
            s2 = new()
            {
                Margin = new Thickness(2)
            };
        AddResourcesOption(s1);

        ScrollViewer sv1 = new()
            {
                Content = s1,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            },
            sv2 = new()
            {
                Content = s2,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        // var commonSettingTabItem = new TabItem
        // {
        //     Content = sv1
        // };
        // commonSettingTabItem.BindLocalization("CommonSetting", HeaderedContentControl.HeaderProperty);
        // var advancedSettingTabItem = new TabItem
        // {
        //     Content = sv2
        // };
        // advancedSettingTabItem.BindLocalization("AdvancedSetting", HeaderedContentControl.HeaderProperty);

        //
        // tabControl.Items.Add(commonSettingTabItem);
        // tabControl.Items.Add(advancedSettingTabItem);

        settingPanel.Children.Add(sv1);
    }

    // private void AddSwitchConfiguration(Panel panel = null, int defaultValue = 0)
    // {
    //     panel ??= settingPanel;
    //     var comboBox = new ComboBox
    //     {
    //         Style = FindResource("ComboBoxExtend") as Style,
    //         Margin = new Thickness(5)
    //     };
    //     string configPath = Path.Combine(Environment.CurrentDirectory, "config");
    //     foreach (string file in Directory.GetFiles(configPath))
    //     {
    //         string fileName = Path.GetFileName(file);
    //         if (fileName.EndsWith(".json") && fileName != "maa_option.json")
    //         {
    //             comboBox.Items.Add(fileName);
    //         }
    //     }
    //
    //
    //     comboBox.BindLocalization("SwitchConfiguration");
    //     comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
    //
    //     comboBox.SelectionChanged += (sender, _) =>
    //     {
    //         string selectedItem = (string)comboBox.SelectedItem;
    //         if (selectedItem == "config.json")
    //         {
    //             //
    //         }
    //         // else if (selectedItem == "maa_option.json")
    //         // {
    //         //     // 什么都不做，等待后续添加逻辑
    //         // }
    //         else if (selectedItem == "config.json.bak")
    //         {
    //             string _currentFile = Path.Combine(configPath, "config.json");
    //             string _selectedItem = Path.Combine(configPath, "config.json.bak");
    //             string _bakContent = File.ReadAllText(_selectedItem);
    //             File.WriteAllText(_currentFile, _bakContent);
    //             RestartMFA();
    //         }
    //         else
    //         {
    //             // 恢复成绝对路径
    //             string _currentFile = Path.Combine(configPath, "config.json");
    //             string _selectedItem = Path.Combine(configPath, selectedItem);
    //             SwapFiles(_currentFile, _selectedItem);
    //             RestartMFA();
    //         }
    //     };
    //     // comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("SwitchConfigurationIndex", defaultValue);
    //     panel.Children.Add(comboBox);
    // }


    public void RestartMFA()
    {
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        DispatcherHelper.RunOnMainThread(Application.Current.Shutdown);
    }

    private void AddResourcesOption(Panel panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            SelectedIndex = MFAConfiguration.GetConfiguration("ResourceIndex", defaultValue),
            Style = FindResource("ComboBoxExtend") as Style,
            DisplayMemberPath = "Name",
            Margin = new Thickness(5)
        };

        var binding = new Binding("Idle")
        {
            Source = ViewModel,
            Mode = BindingMode.OneWay
        };
        comboBox.SetBinding(IsEnabledProperty, binding);

        comboBox.BindLocalization("ResourceOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        if (MaaInterface.Instance?.Resource != null)
        {
            var a = new List<MaaInterface.MaaCustomResource>();
            foreach (var VARIABLE in MaaInterface.Instance.Resource)
            {
                var o = new MaaInterface.MaaCustomResource
                {
                    Name = LanguageHelper.GetLocalizedString(VARIABLE.Name),
                    Path = VARIABLE.Path
                };
                a.Add(o);

            }
            comboBox.ItemsSource = a;
        }

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;

            if (MaaInterface.Instance?.Resources != null && MaaInterface.Instance.Resources.Count > index)
                MaaProcessor.CurrentResources =
                    MaaInterface.Instance.Resources[MaaInterface.Instance.Resources.Keys.ToList()[index]];
            MFAConfiguration.SetConfiguration("ResourceIndex", index);
        };

        panel.Children.Add(comboBox);
    }

    private void AddStartEmulatorOption(Panel panel = null)
    {
        panel ??= settingPanel;
        var textBox = new TextBox
        {
            //Text = MFAConfiguration.GetConfiguration("AdbConfig", "{\"extras\":{}}"), HorizontalAlignment = HorizontalAlignment.Stretch,
            Text = MFAConfiguration.GetConfiguration("EmulatorConfig", ""),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ToolTip = "mumu是-v 多开号(从0开始),雷电是index=多开号(也是0)",
            Margin = new Thickness(5)
        };


        textBox.BindLocalization("StartupParameter");
        textBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        textBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            MFAConfiguration.SetConfiguration("EmulatorConfig", text);
        };


        // var comboBox = new ComboBox
        // {
        //     Style = FindResource("ComboBoxExtend") as Style,
        //     Margin = new Thickness(5),
        //     DisplayMemberPath = "Name",
        //     ItemsSource = new List<LocalizationViewModel>
        //     {
        //         new("General"),
        //         new("BlueStacks"),
        //         new("MuMuEmulator12"),
        //         new("LDPlayer"),
        //         new("Nox"),
        //         new("XYAZ"),
        //     }
        // };
        // var cBinding = new CalcBinding.Binding
        // {
        //     Path = "IsAdb",
        //     Source = ViewModel,
        // };
        // comboBox.ItemsSource = LanguageManager.SupportedLanguages;
        //
        // var binding = new Binding("Idle")
        // {
        //     Source = ViewModel,
        //     Mode = BindingMode.OneWay
        // };
        //
        // comboBox.SetBinding(IsEnabledProperty, binding);
        // comboBox.BindLocalization("LanguageOption");
        // comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        //
        // comboBox.SelectionChanged += (sender, _) =>
        // {
        //     if ((sender as ComboBox)?.SelectedItem is LanguageManager.SupportedLanguage language)
        //         LanguageManager.ChangeLanguage(language);
        //     MFAConfiguration.SetConfiguration("ConnectEmulatorMode", (sender as ComboBox)?.SelectedIndex ?? 0);
        // };
        //
        // comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("ConnectEmulatorMode", "General");
        // panel.Children.Add(comboBox);
        panel.Children.Add(textBox);
    }


    private void AddIntroduction(Panel panel = null, string input = "")
    {
        panel ??= settingPanel;
        input = LanguageHelper.GetLocalizedString(input);
        RichTextBox richTextBox = new RichTextBox
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsReadOnly = true
        };

        FlowDocument flowDocument = new FlowDocument();
        Paragraph paragraph = new Paragraph();

        string pattern = @"\[(?<tag>[^\]]+):?(?<value>[^\]]*)\](?<content>.*?)\[/\k<tag>\]";
        Regex regex = new Regex(pattern);
        int lastIndex = 0;

        void ParseAndApplyTags(string text, Span currentSpan)
        {
            List<Inline> inlinesToAdd = new List<Inline>();
            lastIndex = 0;

            foreach (Match match in regex.Matches(text))
            {
                if (match.Index > lastIndex)
                {
                    string textBeforeMatch = text.Substring(lastIndex, match.Index - lastIndex);

                    textBeforeMatch = textBeforeMatch.Replace("\n", Environment.NewLine);
                    inlinesToAdd.Add(new Run(textBeforeMatch));
                }


                string tag = match.Groups["tag"].Value.ToLower();
                string value = match.Groups["value"].Value.ToLower();
                string content = match.Groups["content"].Value;

                var span = new Span();
                ParseAndApplyTags(content, span);


                switch (tag)
                {
                    case "color":
                        span.Foreground = new BrushConverter().ConvertFromString(value) as Brush ?? span.Foreground;
                        break;
                    case "size":
                        if (double.TryParse(value, out double fontSize))
                        {
                            span.FontSize = fontSize;
                        }

                        break;
                    case "b":
                        span.FontWeight = FontWeights.Bold;
                        break;
                    case "i":
                        span.FontStyle = FontStyles.Italic;
                        break;
                    case "u":
                        span.TextDecorations = TextDecorations.Underline;
                        break;
                    case "s":
                        span.TextDecorations = TextDecorations.Strikethrough;
                        break;
                }

                inlinesToAdd.Add(span);

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                string textAfterMatch = text.Substring(lastIndex);

                textAfterMatch = textAfterMatch.Replace("\n", Environment.NewLine);
                inlinesToAdd.Add(new Run(textAfterMatch));
            }

            foreach (var inline in inlinesToAdd)
            {
                currentSpan.Inlines.Add(inline);
            }
        }


        Span rootSpan = new Span();

        ParseAndApplyTags(input, rootSpan);

        paragraph.Inlines.Add(rootSpan);

        flowDocument.Blocks.Add(paragraph);
        richTextBox.Document = flowDocument;
        panel.Children.Add(richTextBox);
    }


    private void AddAutoStartOption(Panel panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            DisplayMemberPath = "Name"
        };

        comboBox.ItemsSource = ViewModel.BeforeTaskList;
        comboBox.BindLocalization("AutoStartOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("AutoStartIndex", defaultValue);

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            MFAConfiguration.SetConfiguration("AutoStartIndex", index);
            ViewModel.BeforeTask = ViewModel.BeforeTaskList[index].Name;
        };


        panel.Children.Add(comboBox);
    }

    private void AddAfterTaskOption(Panel panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            DisplayMemberPath = "Name"
        };
        comboBox.ItemsSource = ViewModel.AfterTaskList;

        comboBox.BindLocalization("AfterTaskOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("AfterTaskIndex", defaultValue);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            MFAConfiguration.SetConfiguration("AfterTaskIndex", index);
            ViewModel.AfterTask = ViewModel.AfterTaskList[index].Name;
        };

        panel.Children.Add(comboBox);
    }


    private void AddStartSettingOption(Panel panel = null)
    {
        panel ??= settingPanel;
        // var binding = new Binding("Idle")
        // {
        //     Source = ViewModel,
        //     Mode = BindingMode.OneWay
        // };
        // checkBox.SetBinding(IsEnabledProperty, binding);


        var grid = new Grid
        {
            Margin = new Thickness(5)
        };

        var col1 = new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        };
        var col2 = new ColumnDefinition
        {
            Width = new GridLength(40)
        };

        grid.ColumnDefinitions.Add(col1);
        grid.ColumnDefinitions.Add(col2);

        var t1 = new TextBox
        {
            Text = MFAConfiguration.GetConfiguration("SoftwarePath", string.Empty),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        t1.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            MFAConfiguration.SetConfiguration("SoftwarePath", text);
        };
        t1.SetValue(InfoElement.ShowClearButtonProperty, true);
        t1.BindLocalization("SoftwarePath");
        Grid.SetColumn(t1, 0);

        var path = new System.Windows.Shapes.Path
        {
            Width = 15,
            MaxWidth = 15,
            Stretch = Stretch.Uniform,
            Data = FindResource("LoadGeometry") as Geometry,
            Fill = FindResource("GrayColor4") as Brush
        };

        // var b1 = new Binding("GrayColor4")
        // {
        //     Source = this
        // };
        // button.SetBinding(System.Windows.Shapes.Path.FillProperty, b1);
        var button = new Button
        {
            Content = path,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        button.Click += (_, _) =>
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "SelectExecutableFile".ToLocalization(),
                Filter = "ExeFilter".ToLocalization()
            };

            if (openFileDialog.ShowDialog().IsTrue())
            {
                t1.Text = openFileDialog.FileName;
            }
        };
        button.BindLocalization("Select", ToolTipProperty);
        Grid.SetColumn(button, 1);

        grid.Children.Add(t1);
        grid.Children.Add(button);

        var numericUpDown = new NumericUpDown
        {
            Margin = new Thickness(5),
            Value = MFAConfiguration.GetConfiguration("WaitSoftwareTime", 60.0),
            Style = FindResource("NumericUpDownExtend") as Style
        };

        numericUpDown.BindLocalization("WaitSoftware");
        numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        // numericUpDown.SetBinding(IsEnabledProperty, binding);
        numericUpDown.ValueChanged += (sender, _) =>
        {
            var value = (sender as NumericUpDown)?.Value ?? 60;
            MFAConfiguration.SetConfiguration("WaitSoftwareTime", value);
        };
        panel.Children.Add(grid);
        panel.Children.Add(numericUpDown);
    }

    public void SetOption(DragItemViewModel dragItem, bool value)
    {
        if (dragItem.InterfaceItem != null && value)
        {
            settingPanel.Children.Clear();
            var s1 = new StackPanel();

            AddRepeatOption(s1, dragItem);
            if (dragItem.InterfaceItem.Option != null)
            {
                foreach (var option in dragItem.InterfaceItem.Option)
                    AddOption(s1, option, dragItem);
            }

            if (dragItem.InterfaceItem.Document != null && dragItem.InterfaceItem.Document.Count > 0)
            {
                string combinedString = string.Join("\\n", dragItem.InterfaceItem.Document);
                AddIntroduction(s1, Regex.Unescape(combinedString));
            }

            var sc1 = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = s1
            };
            Binding heightBinding = new Binding("ActualHeight")
            {
                Source = settingPanel,
                Converter = new SubtractConverter(),
                ConverterParameter = "20"
            };
            sc1.SetBinding(HeightProperty, heightBinding);
            settingPanel.Children.Add(sc1);
        }
    }

    private void AddOption(Panel panel, MaaInterface.MaaInterfaceSelectOption option, DragItemViewModel source)
    {
        if (MaaInterface.Instance != null && MaaInterface.Instance.Option != null && option.Name != null)
        {
            if (MaaInterface.Instance.Option.TryGetValue(option.Name, out var interfaceOption))
            {
                if (interfaceOption.Cases != null)
                {
                    foreach (var VARIABLE in interfaceOption.Cases)
                    {
                        VARIABLE.Name = LanguageHelper.GetLocalizedString(VARIABLE.Name);
                    }
                }


                var multiBinding = new MultiBinding
                {
                    Converter = FindResource("CustomIsEnabledConverter") as IMultiValueConverter,
                    Mode = BindingMode.OneWay
                };

                multiBinding.Bindings.Add(new Binding("IsCheckedWithNull")
                {
                    Source = source
                });
                multiBinding.Bindings.Add(new Binding("Idle")
                {
                    Source = ViewModel
                });
                Console.WriteLine(interfaceOption.Cases.ShouldSwitchButton(out _, out _));
                if (interfaceOption.Cases.ShouldSwitchButton(out var yes, out var no))
                {
                    var toggleButton = new ToggleButton
                    {
                        IsChecked = option.Index == yes,
                        Style = FindResource("ToggleButtonSwitch") as Style,
                        Height = 20,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Tag = option.Name,
                        MinWidth = 60,
                        Margin = new Thickness(0, 0, -12, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Grid.SetColumn(toggleButton, 2);
                    toggleButton.SetBinding(IsEnabledProperty, multiBinding);
                    toggleButton.Checked += (_, _) =>
                    {
                        option.Index = yes;
                        MFAConfiguration.SetConfiguration("TaskItems",
                            ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
                    };
                    toggleButton.Unchecked += (_, _) =>
                    {
                        option.Index = no;
                        MFAConfiguration.SetConfiguration("TaskItems",
                            ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
                    };
                    var textBlock = new TextBlock
                    {
                        Text = option.Name,
                        Margin = new Thickness(0, 0, 5, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap
                    };
                    Grid.SetColumn(textBlock, 0);
                    toggleButton.SetValue(ToolTipProperty, option.Name);
                    var grid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition
                            {
                                Width = GridLength.Auto
                            },
                            new ColumnDefinition
                            {
                                Width = new GridLength(1, GridUnitType.Star)
                            },
                            new ColumnDefinition
                            {
                                Width = GridLength.Auto
                            }
                        },
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(12, 5, 0, 5),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var spacer = new FrameworkElement();
                    Grid.SetColumn(spacer, 1);

                    grid.Children.Add(textBlock);
                    grid.Children.Add(spacer);
                    grid.Children.Add(toggleButton);


                    panel.Children.Add(grid);
                }
                else
                {
                    var comboBox = new ComboBox
                    {
                        SelectedIndex = option.Index ?? 0,
                        Style = FindResource("ComboBoxExtend") as Style,
                        DisplayMemberPath = "Name",
                        Margin = new Thickness(5),
                    };
                    comboBox.SetBinding(IsEnabledProperty, multiBinding);

                    comboBox.ItemsSource = interfaceOption.Cases;

                    comboBox.Tag = option.Name;

                    comboBox.SelectionChanged += (_, _) =>
                    {
                        option.Index = comboBox.SelectedIndex;

                        MFAConfiguration.SetConfiguration("TaskItems",
                            ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
                    };

                    comboBox.SetValue(ToolTipProperty, option.Name);
                    comboBox.SetValue(TitleElement.TitleProperty, option.Name);
                    comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

                    panel.Children.Add(comboBox);
                }
            }
        }
    }


    private void AddRepeatOption(Panel panel, DragItemViewModel source)
    {
        if (source.InterfaceItem is { Repeatable: true })
        {
            NumericUpDown numericUpDown = new NumericUpDown
            {
                Value = source.InterfaceItem.RepeatCount ?? 1,
                Style = FindResource("NumericUpDownPlus") as Style,
                Margin = new Thickness(5),
                Increment = 1,
                Minimum = -1,
                DecimalPlaces = 0
            };

            var multiBinding = new MultiBinding
            {
                Converter = FindResource("CustomIsEnabledConverter") as IMultiValueConverter,
                Mode = BindingMode.OneWay
            };

            multiBinding.Bindings.Add(new Binding("IsCheckedWithNull")
            {
                Source = source
            });
            multiBinding.Bindings.Add(new Binding("Idle")
            {
                Source = ViewModel
            });

            numericUpDown.SetBinding(ComboBox.IsEnabledProperty, multiBinding);

            numericUpDown.Tag = source.Name;
            numericUpDown.ValueChanged += (_, _) =>
            {
                source.InterfaceItem.RepeatCount = Convert.ToInt16(numericUpDown.Value);
                MFAConfiguration.SetConfiguration("TaskItems",
                    ViewModel?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            };
            numericUpDown.BindLocalization("RepeatOption");
            numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
            panel.Children.Add(numericUpDown);
        }
    }

    private static EditTaskDialog _taskDialog;

    public static EditTaskDialog TaskDialog
    {
        get
        {
            if (_taskDialog == null)
                _taskDialog = new EditTaskDialog();
            return _taskDialog;
        }
        set => _taskDialog = value;
    }

    private void Edit(object sender, RoutedEventArgs e)
    {
        if (!IsConnected())
        {
            GrowlHelper.Warning(
                "Warning_CannotConnect".ToLocalizationFormatted((ViewModel?.IsAdb).IsTrue()
                    ? "Emulator".ToLocalization()
                    : "Window".ToLocalization()));
            return;
        }

        TaskDialog?.Show();
        if (ViewModel != null)
            ViewModel.Idle = false;
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        foreach (var task in ViewModel.TaskItemViewModels)
            task.IsChecked = true;
    }

    private void SelectNone(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        foreach (var task in ViewModel.TaskItemViewModels)
            task.IsChecked = false;
    }

    private void Add(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.Idle = false;
        var addTaskDialog = new AddTaskDialog(ViewModel?.TasksSource);
        addTaskDialog.ShowDialog();
        if (addTaskDialog.OutputContent != null)
        {
            ViewModel?.TaskItemViewModels.Add(addTaskDialog.OutputContent.Clone());
            MFAConfiguration.SetConfiguration("TaskItems", ViewModel?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
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

    private void Delete(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        var contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is Grid item)
        {
            if (item.DataContext is DragItemViewModel taskItemViewModel && ViewModel != null)
            {
                // 获取选中项的索引
                int index = ViewModel.TaskItemViewModels.IndexOf(taskItemViewModel);
                ViewModel.TaskItemViewModels.RemoveAt(index);
                MFAConfiguration.SetConfiguration("TaskItems", ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
        }
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
            ConnectionTabControl.SelectedIndex = MaaInterface.Instance?.DefaultController == "win32" ? 1 : 0;
            ConfigureTaskSettingsPanel();
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
                            deviceComboBox.ItemsSource = new List<AdbDeviceInfo>
                            {
                                device
                            };
                            deviceComboBox.SelectedIndex = 0;
                            SetConnected(true);
                        });
                    }
                }
                VersionChecker.Check();
            }
            ConnectionTabControl.SelectionChanged += ConnectionTabControlOnSelectionChanged;
            if (ViewModel != null)
                ViewModel.NotLock = MaaInterface.Instance?.LockController != true;
            ConnectSettingButton.IsChecked = true;
            var value = MFAConfiguration.GetConfiguration("EnableEdit", false);
            if (!value)
                EditButton.Visibility = Visibility.Collapsed;
            MFAConfiguration.SetConfiguration("EnableEdit", value);
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

    private void ConfigureTaskSettingsPanel(object sender = null, RoutedEventArgs e = null)
    {
        settingPanel.Children.Clear();
        StackPanel s2 = new()
        {
            Margin = new Thickness(2)
        };

        AddAutoStartOption(s2);
        AddAfterTaskOption(s2);

        // AddStartSettingOption(s2);
        // AddStartEmulatorOption(s2);


        ScrollViewer sv2 = new()
        {
            Content = s2,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        settingPanel.Children.Add(sv2);
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
                    deviceComboBox.ItemsSource = new List<AdbDeviceInfo>
                    {
                        device
                    };
                    deviceComboBox.SelectedIndex = 0;
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
