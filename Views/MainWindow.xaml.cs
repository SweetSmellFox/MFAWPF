using System.Diagnostics;
using System.Globalization;
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
using HandyControl.Interactivity;
using HandyControl.Themes;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Utils;
using MFAWPF.Utils.Converters;
using MFAWPF.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using WPFLocalizeExtension.Extensions;
using ComboBox = HandyControl.Controls.ComboBox;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TabControl = System.Windows.Controls.TabControl;
using TabItem = System.Windows.Controls.TabItem;
using TextBox = HandyControl.Controls.TextBox;

namespace MFAWPF.Views;

public partial class MainWindow
{
    public static MainWindow Instance { get; private set; }
    private readonly MaaToolkit _maaToolkit;

    public static MainViewModel? Data { get; private set; }

    public static readonly string Version =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

    public Dictionary<string, TaskModel> TaskDictionary = new();

    public MainWindow()
    {
        DataSet.Data = JsonHelper.ReadFromConfigJsonFile("config", new Dictionary<string, object>());
        DataSet.MaaConfig = JsonHelper.ReadFromConfigJsonFile("maa_option", new Dictionary<string, object>());
        LanguageManager.Initialize();
        InitializeComponent();
        Instance = this;
        version.Text = Version;
        Data = DataContext as MainViewModel;
        Loaded += (_, _) => { LoadUI(); };
        InitializeData();
        OCRHelper.Initialize();
        StateChanged += (_, _) =>
        {
            if (DataSet.GetData("ShouldMinimizeToTray", false))
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

    public bool InitializeData()
    {
        if (!File.Exists($"{AppContext.BaseDirectory}/interface.json"))
        {
            LoggerService.LogInfo("未找到interface文件，生成interface.json...");
            MaaInterface.Instance = new MaaInterface
                {
                    Version = "1.0",
                    Name = "Debug",
                    Task = [],
                    Resource =
                    [
                        new MaaInterface.MaaCustomResource
                        {
                            Name = "默认",
                            Path =
                            [
                                "{PROJECT_DIR}/resource/base",
                            ],
                        },
                    ],
                    Controller =
                    [
                        new MaaInterface.MaaResourceController()
                        {
                            Name = "adb 默认方式",
                            Type = "adb"
                        },
                    ],
                    Option = new Dictionary<string, MaaInterface.MaaInterfaceOption>
                    {
                        {
                            "测试", new MaaInterface.MaaInterfaceOption()
                            {
                                Cases =
                                [

                                    new MaaInterface.MaaInterfaceOptionCase
                                    {
                                        Name = "测试1",
                                        PipelineOverride = new Dictionary<string, TaskModel>()
                                    },
                                    new MaaInterface.MaaInterfaceOptionCase
                                    {
                                        Name = "测试2",
                                        PipelineOverride = new Dictionary<string, TaskModel>()
                                    }
                                ]
                            }
                        }
                    }
                }
                ;
            JsonHelper.WriteToJsonFilePath(AppContext.BaseDirectory, "interface",
                MaaInterface.Instance, new MaaInterfaceSelectOptionConverter(true));
        }
        else
        {
            MaaInterface.Instance =
                JsonHelper.ReadFromJsonFilePath(AppContext.BaseDirectory, "interface",
                    new MaaInterface(),
                    () => { }, new MaaInterfaceSelectOptionConverter(false));
        }

        if (MaaInterface.Instance != null)
        {
            AppendVersionLog(MaaInterface.Instance.Version);
            Data?.TasksSource.Clear();
            LoadTasks(MaaInterface.Instance.Task ?? new List<TaskInterfaceItem>());
        }

        ConnectToMAA();
        return LoadTask();

    }


    public bool FirstTask = true;

    private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks)
    {
        foreach (var task in tasks)
        {
            var dragItem = new DragItemViewModel(task);

            if (FirstTask)
            {
                if (MaaInterface.Instance?.Resources != null && MaaInterface.Instance.Resources.Count > DataSet.GetData("ResourceIndex", 0))
                    MaaProcessor.CurrentResources =
                        MaaInterface.Instance.Resources[
                            MaaInterface.Instance.Resources.Keys.ToList()[DataSet.GetData("ResourceIndex", 0)]];
                else
                    MaaProcessor.CurrentResources = new List<string>
                    {
                        MaaProcessor.ResourceBase
                    };
                FirstTask = false;
            }

            Data?.TasksSource.Add(dragItem);
        }

        if (Data?.TaskItemViewModels.Count == 0)
        {
            var items = DataSet.GetData("TaskItems",
                    new List<TaskInterfaceItem>())
                ?? new List<TaskInterfaceItem>();
            var dragItemViewModels = items.Select(interfaceItem => new DragItemViewModel(interfaceItem)).ToList();
            Data.TaskItemViewModels.AddRange(dragItemViewModels);
            if (Data.TaskItemViewModels.Count == 0 && Data.TasksSource.Count != 0)
            {
                foreach (var item in Data.TasksSource)
                    Data.TaskItemViewModels.Add(item);
            }
        }
    }

    private void OnTaskStackChanged(object? sender, EventArgs e)
    {
        ToggleTaskButtonsVisibility(isRunning: MaaProcessor.Instance.TaskQueue.Count > 0);
    }

    private void ChangeVisibility(bool visible)
    {
        Visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    public void ToggleTaskButtonsVisibility(bool isRunning)
    {
        Growls.Process(() =>
        {
            if (Data is not null)
                Data.IsRunning = isRunning;
            // startButton.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
            // startButton.IsEnabled = !isRunning;
            // stopButton.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            // stopButton.IsEnabled = isRunning;
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = !ConfirmExit();
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
                    var jsonFiles = Directory.GetFiles($"{resourcePath}/pipeline/", "*.json");
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
                                Growls.Error(string.Format(
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
            Growls.Error(string.Format(LocExtension.GetLocalizedValue<string>("PipelineLoadError"),
                ex.Message));
            Console.WriteLine(ex);
            LoggerService.LogError(ex);
            return false;
        }
    }

    private void PopulateTasks(Dictionary<string, TaskModel> taskDictionary)
    {
        TaskDictionary = taskDictionary;
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
                    Growls.Error(string.Format(LocExtension.GetLocalizedValue<string>("TaskNotFoundError"),
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

    public void Start(object? sender, RoutedEventArgs? e) => Start();

    public void Start(bool onlyStart = false)
    {
        if (Data?.Idle == false)
        {
            Growls.Warning("CannotStart".GetLocalizationString());
            return;
        }
        if (InitializeData())
        {
            MaaProcessor.Money = 0;
            var tasks = Data?.TaskItemViewModels.ToList().FindAll(task => task.IsChecked);
            ConnectToMAA();
            MaaProcessor.Instance.Start(tasks, onlyStart);
        }
    }

    public void Stop(object? sender, RoutedEventArgs? e) => Stop();

    public void Stop()
    {
        MaaProcessor.Instance.Stop();
    }

    private async void ConnectionTabControlOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Data is not null)
        {
            Data.IsAdb = adbTab.IsSelected;
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
            MaaProcessor.Config.DesktopWindow.Name = window.Name;
            MaaProcessor.Config.DesktopWindow.HWnd = window.Handle;
            MaaProcessor.Instance.SetCurrentTasker();
        }
        else if (deviceComboBox.SelectedItem is AdbDeviceInfo device)
        {
            Growl.Info(string.Format(LocExtension.GetLocalizedValue<string>("EmulatorSelectionMessage"),
                device.Name));
            MaaProcessor.Config.AdbDevice.Name = device.Name;
            MaaProcessor.Config.AdbDevice.AdbPath = device.AdbPath;
            MaaProcessor.Config.AdbDevice.AdbSerial = device.AdbSerial;
            MaaProcessor.Config.AdbDevice.Config = device.Config;
            MaaProcessor.Instance.SetCurrentTasker();
            DataSet.SetData("AdbDevice", device);
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
            Growl.Info((Data?.IsAdb).IsTrue()
                ? LocExtension.GetLocalizedValue<string>("EmulatorDetectionStarted")
                : LocExtension.GetLocalizedValue<string>("WindowDetectionStarted"));
            SetConnected(false);
            if ((Data?.IsAdb).IsTrue())
            {
                var devices = _maaToolkit.AdbDevice.Find();
                Growls.Process(() => deviceComboBox.ItemsSource = devices);
                SetConnected(devices.Count > 0);
                var emulatorConfig = DataSet.GetData("EmulatorConfig", string.Empty);
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
                Growls.Process(() => deviceComboBox.SelectedIndex = resultIndex);
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
                                DataSet.SetData("AdbControlInputType", result);
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
                                DataSet.SetData("AdbControlScreenCapType", result);
                            }
                        }
                    }
                }
            }
            else
            {
                var windows = _maaToolkit.Desktop.Window.Find().ToList();
                Growls.Process(() =>deviceComboBox.ItemsSource = windows);
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
                                DataSet.SetData("Win32ControlInputType", result);
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
                                DataSet.SetData("Win32ControlScreenCapType", result);
                            }
                        }
                    }
                }
                Growls.Process(() => deviceComboBox.SelectedIndex = resultIndex);
            }

            if (!IsConnected())
            {
                Growl.Info((Data?.IsAdb).IsTrue()
                    ? LocExtension.GetLocalizedValue<string>("NoEmulatorFound")
                    : LocExtension.GetLocalizedValue<string>("NoWindowFound"));
            }
        }
        catch (Exception ex)
        {
            Growls.Warning(string.Format(LocExtension.GetLocalizedValue<string>("TaskStackError"),
                (Data?.IsAdb).IsTrue() ? "Emulator".GetLocalizationString() : "Window".GetLocalizationString(),
                ex.Message));
            SetConnected(false);
            LoggerService.LogError(ex);
            Console.WriteLine(ex);
        }
    }

    private void ConfigureSettingsPanel(object? sender = null, RoutedEventArgs? e = null)
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

    private void AddSwitchConfiguration(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };
        string configPath = Path.Combine(Environment.CurrentDirectory, "config");
        foreach (string file in Directory.GetFiles(configPath))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".json") && fileName != "maa_option.json")
            {
                comboBox.Items.Add(fileName);
            }
        }


        comboBox.BindLocalization("SwitchConfiguration");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        comboBox.SelectionChanged += (sender, _) =>
        {
            string selectedItem = (string)comboBox.SelectedItem;
            if (selectedItem == "config.json")
            {
                //
            }
            // else if (selectedItem == "maa_option.json")
            // {
            //     // 什么都不做，等待后续添加逻辑
            // }
            else if (selectedItem == "config.json.bak")
            {
                string _currentFile = Path.Combine(configPath, "config.json");
                string _selectedItem = Path.Combine(configPath, "config.json.bak");
                string _bakContent = File.ReadAllText(_selectedItem);
                File.WriteAllText(_currentFile, _bakContent);
                RestartMFA();
            }
            else
            {
                // 恢复成绝对路径
                string _currentFile = Path.Combine(configPath, "config.json");
                string _selectedItem = Path.Combine(configPath, selectedItem);
                SwapFiles(_currentFile, _selectedItem);
                RestartMFA();
            }
        };
        // comboBox.SelectedIndex = DataSet.GetData("SwitchConfigurationIndex", defaultValue);
        panel.Children.Add(comboBox);
    }

    private void SwapFiles(string file1Path, string file2Path)
    {
        // 备份文件
        string backupFilePath = $"{file1Path}.bak";
        File.Copy(file1Path, backupFilePath, true);

        // 读取文件内容
        string file1Content = File.ReadAllText(file1Path);
        string file2Content = File.ReadAllText(file2Path);

        // 只更换 config.json 的内容
        File.WriteAllText(file1Path, file2Content);
    }

    private void RestartMFA()
    {
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        Growls.Process(Application.Current.Shutdown);
    }


    private void AddAbout()
    {
        StackPanel s1 = new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            },
            s2 = new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        var t1 = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        t1.BindLocalization("ProjectLink", TextBlock.TextProperty);
        s1.Children.Add(t1);
        s1.Children.Add(new Shield
        {
            Status = "MFAWPF",
            Subject = "Github",
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Command = ControlCommands.OpenLink,
            CommandParameter = "https://github.com/SweetSmellFox/MFAWPF"
        });
        var resourceLink = MaaInterface.Instance?.Url;
        if (!string.IsNullOrWhiteSpace(resourceLink))
        {
            var t2 = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };
            t2.BindLocalization("ResourceLink", TextBlock.TextProperty);
            s2.Children.Add(t2);
            s2.Children.Add(new Shield
            {
                Status = MaaInterface.Instance?.Name ?? "Resource",
                Subject = "Github",
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Command = ControlCommands.OpenLink,
                CommandParameter = resourceLink
            });
        }
        settingsView.MFAShieldTextBlock.Text = Version;
        var resourceVersion = MaaInterface.Instance?.Version;
        if (!string.IsNullOrWhiteSpace(resourceVersion))
        {
            settingsView.ResourceShieldTextBlock.Text = resourceVersion;
        }
        else
        {
            settingsView.ResourceShield.Visibility = Visibility.Collapsed;
        }
        settingsView.settingStackPanel.Children.Add(s1);
        settingsView.settingStackPanel.Children.Add(s2);
    }

    private void AddResourcesOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            SelectedIndex = DataSet.GetData("ResourceIndex", defaultValue),
            Style = FindResource("ComboBoxExtend") as Style,
            DisplayMemberPath = "Name",
            Margin = new Thickness(5)
        };

        var binding = new Binding("Idle")
        {
            Source = Data,
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
                    Name = LanguageManager.GetLocalizedString(VARIABLE.Name),
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
            DataSet.SetData("ResourceIndex", index);
        };

        panel.Children.Add(comboBox);
    }

    private void AddThemeOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };
        var light = new ComboBoxItem();
        light.BindLocalization("LightColor", ContentProperty);
        var dark = new ComboBoxItem();
        dark.BindLocalization("DarkColor", ContentProperty);
        var followSystem = new ComboBoxItem();
        followSystem.BindLocalization("FollowingSystem", ContentProperty);
        comboBox.Items.Add(light);
        comboBox.Items.Add(dark);
        comboBox.Items.Add(followSystem);
        var binding = new Binding("Idle")
        {
            Source = Data,
            Mode = BindingMode.OneWay
        };
        comboBox.SetBinding(IsEnabledProperty, binding);
        comboBox.BindLocalization("ThemeOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;

            switch (index)
            {
                case 0:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    break;
                case 1:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    break;
                default:
                    FollowSystemTheme();
                    break;
            }

            ThemeManager.Current.ApplicationTheme = index == 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            DataSet.SetData("ThemeIndex", index);
        };
        comboBox.SelectedIndex = DataSet.GetData("ThemeIndex", defaultValue);
        panel.Children.Add(comboBox);
    }

    public static void FollowSystemTheme()
    {
        ThemeManager.Current.ApplicationTheme =
            ThemeHelper.IsLightTheme() ? ApplicationTheme.Light : ApplicationTheme.Dark;
    }

    private void AddLanguageOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            DisplayMemberPath = "Name",
        };

        comboBox.ItemsSource = LanguageManager.SupportedLanguages;

        var binding = new Binding("Idle")
        {
            Source = Data,
            Mode = BindingMode.OneWay
        };

        comboBox.SetBinding(IsEnabledProperty, binding);
        comboBox.BindLocalization("LanguageOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        comboBox.SelectionChanged += (sender, _) =>
        {
            if ((sender as ComboBox)?.SelectedItem is LanguageManager.SupportedLanguage language)
                LanguageManager.ChangeLanguage(language);
            DataSet.SetData("LangIndex", (sender as ComboBox)?.SelectedIndex ?? 0);
        };

        comboBox.SelectedIndex = DataSet.GetData("LangIndex", defaultValue);
        panel.Children.Add(comboBox);
    }

    private void SetSettingOption(ComboBox? comboBox,
        string titleKey,
        IEnumerable<string> options,
        string datatype,
        int defaultValue = 0)
    {
        comboBox.SelectedIndex = DataSet.GetData(datatype, defaultValue);
        comboBox.ItemsSource = options;
        comboBox.BindLocalization(titleKey);
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData(datatype, index);
            MaaProcessor.Instance.SetCurrentTasker();
        };
    }

    private void SetBindSettingOption(ComboBox? comboBox,
        string titleKey,
        IEnumerable<string> options,
        string datatype,
        int defaultValue = 0)

    {
        comboBox.SelectedIndex = DataSet.GetData(datatype, defaultValue);

        foreach (var s in options)
        {
            var comboBoxItem = new ComboBoxItem();
            comboBoxItem.BindLocalization(s, ContentProperty);
            comboBox.Items.Add(comboBoxItem);
        }

        comboBox.BindLocalization(titleKey);
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData(datatype, index);
            MaaProcessor.Instance.SetCurrentTasker();
        };
    }

    private void AddStartEmulatorOption(Panel? panel = null)
    {
        panel ??= settingPanel;
        var textBox = new TextBox
        {
            //Text = DataSet.GetData("AdbConfig", "{\"extras\":{}}"), HorizontalAlignment = HorizontalAlignment.Stretch,
            Text = DataSet.GetData("EmulatorConfig", ""),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ToolTip = "mumu是-v 多开号(从0开始),雷电是index=多开号(也是0)",
            Margin = new Thickness(5)
        };


        textBox.BindLocalization("StartupParameter");
        textBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        textBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("EmulatorConfig", text);
        };


        // var comboBox = new ComboBox
        // {
        //     Style = FindResource("ComboBoxExtend") as Style,
        //     Margin = new Thickness(5),
        //     DisplayMemberPath = "Name",
        //     ItemsSource = new List<SettingViewModel>
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
        //     Source = Data,
        // };
        // comboBox.ItemsSource = LanguageManager.SupportedLanguages;
        //
        // var binding = new Binding("Idle")
        // {
        //     Source = Data,
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
        //     DataSet.SetData("ConnectEmulatorMode", (sender as ComboBox)?.SelectedIndex ?? 0);
        // };
        //
        // comboBox.SelectedIndex = DataSet.GetData("ConnectEmulatorMode", "General");
        // panel.Children.Add(comboBox);
        panel.Children.Add(textBox);
    }


    private void AddIntroduction(Panel? panel = null, string input = "")
    {
        panel ??= settingPanel;
        input = LanguageManager.GetLocalizedString(input);
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

                Span span = new Span();
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


    private void AddAutoStartOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            DisplayMemberPath = "Name"
        };

        comboBox.ItemsSource = Data.BeforeTaskList;
        comboBox.BindLocalization("AutoStartOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectedIndex = DataSet.GetData("AutoStartIndex", defaultValue);

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData("AutoStartIndex", index);
            Data.BeforeTask = Data.BeforeTaskList[index].Name;
        };


        panel.Children.Add(comboBox);
    }

    private void AddAfterTaskOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            DisplayMemberPath = "Name"
        };
        comboBox.ItemsSource = Data.AfterTaskList;

        comboBox.BindLocalization("AfterTaskOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectedIndex = DataSet.GetData("AfterTaskIndex", defaultValue);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData("AfterTaskIndex", index);
            Data.AfterTask = Data.AfterTaskList[index].Name;
        };

        panel.Children.Add(comboBox);
    }

    private void SetRememberAdbOption(CheckBox? checkBox)
    {
        checkBox.IsChecked = DataSet.GetData("RememberAdb", true);
        checkBox.BindLocalization("RememberAdb", ContentProperty);
        checkBox.Click += (_, _) => { DataSet.SetData("RememberAdb", checkBox.IsChecked); };
    }

    private void AddStartSettingOption(Panel? panel = null)
    {
        panel ??= settingPanel;
        // var binding = new Binding("Idle")
        // {
        //     Source = Data,
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
            Text = DataSet.GetData("SoftwarePath", string.Empty),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        t1.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("SoftwarePath", text);
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
                Title = "SelectExecutableFile".GetLocalizationString(),
                Filter = "ExeFilter".GetLocalizationString()
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
            Value = DataSet.GetData("WaitSoftwareTime", 60.0),
            Style = FindResource("NumericUpDownExtend") as Style
        };

        numericUpDown.BindLocalization("WaitSoftware");
        numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        // numericUpDown.SetBinding(IsEnabledProperty, binding);
        numericUpDown.ValueChanged += (sender, _) =>
        {
            var value = (sender as NumericUpDown)?.Value ?? 60;
            DataSet.SetData("WaitSoftwareTime", value);
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
                        VARIABLE.Name = LanguageManager.GetLocalizedString(VARIABLE.Name);
                    }
                }

                ComboBox comboBox = new ComboBox
                {
                    SelectedIndex = option.Index ?? 0,
                    Style = FindResource("ComboBoxExtend") as Style,
                    DisplayMemberPath = "Name",
                    Margin = new Thickness(5),
                };

                var multiBinding = new MultiBinding()
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
                    Source = Data
                });

                comboBox.SetBinding(IsEnabledProperty, multiBinding);

                comboBox.ItemsSource = interfaceOption.Cases;
                if (!string.IsNullOrWhiteSpace(interfaceOption.DefaultCase) && interfaceOption.Cases != null)
                {
                    int index = interfaceOption.Cases.FindIndex(@case => @case.Name == interfaceOption.DefaultCase);
                    if (index != -1)
                    {
                        comboBox.SelectedIndex = index;
                    }
                }
                comboBox.Tag = option.Name;

                comboBox.SelectionChanged += (_, _) =>
                {
                    option.Index = comboBox.SelectedIndex;

                    DataSet.SetData("TaskItems",
                        Data?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
                };

                comboBox.SetValue(ToolTipProperty, option.Name);
                comboBox.SetValue(TitleElement.TitleProperty, option.Name);
                comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

                panel.Children.Add(comboBox);
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
                Source = Data
            });

            numericUpDown.SetBinding(ComboBox.IsEnabledProperty, multiBinding);

            numericUpDown.Tag = source.Name;
            numericUpDown.ValueChanged += (_, _) =>
            {
                source.InterfaceItem.RepeatCount = Convert.ToInt16(numericUpDown.Value);
                DataSet.SetData("TaskItems",
                    Data?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            };
            numericUpDown.BindLocalization("RepeatOption");
            numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
            panel.Children.Add(numericUpDown);
        }
    }

    private static EditTaskDialog? _taskDialog;

    public static EditTaskDialog? TaskDialog
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
            Growls.Warning(
                "Warning_CannotConnect".GetLocalizedFormattedString((Data?.IsAdb).IsTrue()
                    ? "Emulator".GetLocalizationString()
                    : "Window".GetLocalizationString()));
            return;
        }

        TaskDialog?.Show();
        if (Data != null)
            Data.Idle = false;
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        if (Data == null) return;
        foreach (var task in Data.TaskItemViewModels)
            task.IsChecked = true;
    }

    private void SelectNone(object sender, RoutedEventArgs e)
    {
        if (Data == null) return;
        foreach (var task in Data.TaskItemViewModels)
            task.IsChecked = false;
    }

    private void Add(object sender, RoutedEventArgs e)
    {
        if (Data != null)
            Data.Idle = false;
        var addTaskDialog = new AddTaskDialog(Data?.TasksSource);
        addTaskDialog.ShowDialog();
        if (addTaskDialog.OutputContent != null)
        {
            Data?.TaskItemViewModels.Add(addTaskDialog.OutputContent.Clone());
            DataSet.SetData("TaskItems", Data?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
    }

    public void ConnectToMAA()
    {
        ConfigureMaaProcessorForADB();
        ConfigureMaaProcessorForWin32();
    }

    private void ConfigureMaaProcessorForADB()
    {
        if ((Data?.IsAdb).IsTrue())
        {
            var adbInputType = ConfigureAdbInputTypes();
            var adbScreenCapType = ConfigureAdbScreenCapTypes();

            MaaProcessor.Config.AdbDevice.Input = adbInputType;
            MaaProcessor.Config.AdbDevice.ScreenCap = adbScreenCapType;

            Console.WriteLine(
                $"{LocExtension.GetLocalizedValue<string>("AdbInputMode")}{adbInputType},{LocExtension.GetLocalizedValue<string>("AdbCaptureMode")}{adbScreenCapType}");
        }
    }

    public string ScreenshotType()
    {
        if ((Data?.IsAdb).IsTrue())
            return ConfigureAdbScreenCapTypes().ToString();
        return ConfigureWin32ScreenCapTypes().ToString();
    }

    private AdbInputMethods ConfigureAdbInputTypes()
    {
        return DataSet.GetData("AdbControlInputType", 0) switch
        {
            0 => AdbInputMethods.MinitouchAndAdbKey,
            1 => AdbInputMethods.Maatouch,
            2 => AdbInputMethods.AdbShell,
            3 => AdbInputMethods.All,
            _ => 0
        };
    }

    private AdbScreencapMethods ConfigureAdbScreenCapTypes()
    {
        return DataSet.GetData("AdbControlScreenCapType", 0) switch
        {
            0 => AdbScreencapMethods.Default,
            1 => AdbScreencapMethods.RawWithGzip,
            2 => AdbScreencapMethods.RawByNetcat,
            3 => AdbScreencapMethods.Encode,
            4 => AdbScreencapMethods.EncodeToFileAndPull,
            5 => AdbScreencapMethods.MinicapDirect,
            6 => AdbScreencapMethods.MinicapStream,
            7 => AdbScreencapMethods.EmulatorExtras,
            _ => 0
        };
    }

    private void ConfigureMaaProcessorForWin32()
    {
        if (!(Data?.IsAdb).IsTrue())
        {
            var win32InputType = ConfigureWin32InputTypes();
            var winScreenCapType = ConfigureWin32ScreenCapTypes();

            MaaProcessor.Config.DesktopWindow.Input = win32InputType;
            MaaProcessor.Config.DesktopWindow.ScreenCap = winScreenCapType;

            Console.WriteLine(
                $"{"AdbInputMode".GetLocalizationString()}{win32InputType},{"AdbCaptureMode".GetLocalizationString()}{winScreenCapType}");
            LoggerService.LogInfo(
                $"{"AdbInputMode".GetLocalizationString()}{win32InputType},{"AdbCaptureMode".GetLocalizationString()}{winScreenCapType}");
        }
    }

    private Win32ScreencapMethod ConfigureWin32ScreenCapTypes()
    {
        return DataSet.GetData("Win32ControlScreenCapType", 0) switch
        {
            0 => Win32ScreencapMethod.FramePool,
            1 => Win32ScreencapMethod.DXGIDesktopDup,
            2 => Win32ScreencapMethod.GDI,
            _ => 0
        };
    }

    private Win32InputMethod ConfigureWin32InputTypes()
    {
        return DataSet.GetData("Win32ControlInputType", 0) switch
        {
            0 => Win32InputMethod.Seize,
            1 => Win32InputMethod.SendMessage,
            _ => 0
        };
    }

    private void Delete(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is Grid item)
        {
            if (item.DataContext is DragItemViewModel taskItemViewModel && Data != null)
            {
                // 获取选中项的索引
                int index = Data.TaskItemViewModels.IndexOf(taskItemViewModel);
                Data.TaskItemViewModels.RemoveAt(index);
                DataSet.SetData("TaskItems", Data.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
        }
    }

    public void ShowResourceName(string name)
    {
        Growls.Process(() =>
        {
            resourceName.Visibility = Visibility.Visible;
            resourceNameText.Visibility = Visibility.Visible;
            resourceName.Text = name;
        });
    }

    public void ShowResourceVersion(string v)
    {
        Growls.Process(() =>
        {
            resourceVersion.Visibility = Visibility.Visible;
            resourceVersionText.Visibility = Visibility.Visible;
            resourceVersion.Text = v;
        });
    }

    public void ShowCustomTitle(string v)
    {
        Growls.Process(() =>
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
        Growls.Process(() =>
        {
            InitializationSettings();
            ConnectionTabControl.SelectedIndex = MaaInterface.Instance?.DefaultController == "win32" ? 1 : 0;
            ConfigureTaskSettingsPanel();
            if (DataSet.GetData("AutoStartIndex", 0) >= 1)
            {
                MaaProcessor.Instance.TaskQueue.Push(new MFATask
                {
                    Name = "启动前",
                    Type = MFATask.MFATaskType.MFA,
                    Action = WaitSoftware,
                });
                Start(DataSet.GetData("AutoStartIndex", 0) == 1);
            }
            else
            {
                if (Data?.IsAdb == true && DataSet.GetData("RememberAdb", true) && "adb".Equals(MaaProcessor.Config.AdbDevice.AdbPath) && DataSet.TryGetData<JObject>("AdbDevice", out var jObject))
                {
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new AdbInputMethodsConverter());
                    settings.Converters.Add(new AdbScreencapMethodsConverter());

                    var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
                    if (device != null)
                    {
                        Growls.Process(() =>
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
            }
            ConnectionTabControl.SelectionChanged += ConnectionTabControlOnSelectionChanged;
            if (Data != null)
                Data.NotLock = MaaInterface.Instance?.LockController != true;
            ConnectSettingButton.IsChecked = true;
            var value = DataSet.GetData("EnableEdit", false);
            if (!value)
                EditButton.Visibility = Visibility.Collapsed;
            DataSet.SetData("EnableEdit", value);
            Data.IsDebugMode = MFAExtensions.IsDebugMode();
            if (!string.IsNullOrWhiteSpace(MaaInterface.Instance?.Message))
            {
                Growl.Info(MaaInterface.Instance.Message);
            }
            VersionChecker.Check();


        });
        TaskManager.RunTaskAsync(async () =>
        {
            await Task.Delay(1000);
            Growls.Process(() =>
            {
                if (DataSet.GetData("AutoMinimize", false))
                {
                    Collapse();
                }

                if (DataSet.GetData("AutoHide", false))
                {
                    Hide();
                }
            });
        });
    }

    public static void AppendVersionLog(string? resourceVersion)
    {
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
        string? color = "Gray",
        string weight = "Regular",
        bool showTime = true)
    {
        Data?.AddLog(content, color, weight, showTime);
    }

    public static void AddLog(string content,
        Brush? color = null,
        string weight = "Regular",
        bool showTime = true)
    {
        Data?.AddLog(content, color, weight, showTime);
    }

    public static void AddLogByKey(string key, Brush? color = null, params string[]? formatArgsKeys)
    {
        Data?.AddLogByKey(key, color, formatArgsKeys);
    }

    public void RunScript(string str = "Prescript")
    {
        bool enable = str switch
        {
            "Prescript" => !string.IsNullOrWhiteSpace(DataSet.GetData("Prescript", string.Empty)),
            "Post-script" => !string.IsNullOrWhiteSpace(DataSet.GetData("Post-script", string.Empty)),
            _ => false,
        };
        if (!enable)
        {
            return;
        }

        Func<bool> func = str switch
        {
            "Prescript" => () => ExecuteScript(DataSet.GetData("Prescript", string.Empty)),
            "Post-script" => () => ExecuteScript(DataSet.GetData("Post-script", string.Empty)),
            _ => () => false,
        };

        if (!func())
        {
            LoggerService.LogError($"Failed to execute the {str}.");
        }
    }

    private static bool ExecuteScript(string? scriptPath)
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
        settingsView.MinimizeToTrayCheckBox.IsChecked = DataSet.GetData("ShouldMinimizeToTray", true);
        settingsView.MinimizeToTrayCheckBox.Checked += (_, _) => { DataSet.SetData("ShouldMinimizeToTray", true); };
        settingsView.MinimizeToTrayCheckBox.Unchecked += (_, _) => { DataSet.SetData("ShouldMinimizeToTray", false); };

        //语言设置

        settingsView.languageSettings.ItemsSource = LanguageManager.SupportedLanguages;
        settingsView.languageSettings.DisplayMemberPath = "Name";
        settingsView.languageSettings.BindLocalization("LanguageOption");
        settingsView.languageSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        settingsView.languageSettings.SelectionChanged += (sender, _) =>
        {
            if ((sender as ComboBox)?.SelectedItem is LanguageManager.SupportedLanguage language)
                LanguageManager.ChangeLanguage(language);
            DataSet.SetData("LangIndex", (sender as ComboBox)?.SelectedIndex ?? 0);
        };
        var binding2 = new Binding("LanguageIndex")
        {
            Source = Data,
            Mode = BindingMode.OneWay
        };
        settingsView.languageSettings.SetBinding(ComboBox.SelectedIndexProperty, binding2);

        //主题设置
        settingsView.themeSettings.ItemsSource = new ObservableCollection<SettingViewModel>()
        {
            new("LightColor"),
            new("DarkColor"),
            new("LightColor"),
        };
        settingsView.themeSettings.DisplayMemberPath = "Name";
        settingsView.themeSettings.BindLocalization("ThemeOption");
        settingsView.themeSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        settingsView.themeSettings.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;

            switch (index)
            {
                case 0:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    break;
                case 1:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    break;
                default:
                    FollowSystemTheme();
                    break;
            }

            ThemeManager.Current.ApplicationTheme = index == 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            DataSet.SetData("ThemeIndex", index);
        };
        settingsView.themeSettings.SelectedIndex = DataSet.GetData("ThemeIndex", 0);

        //性能设置
        settingsView.performanceSettings.IsChecked = DataSet.GetData("EnableGPU", true);
        settingsView.performanceSettings.Checked += (_, _) => { DataSet.SetData("EnableGPU", true); };
        settingsView.performanceSettings.Unchecked += (_, _) => { DataSet.SetData("EnableGPU", false); };

        //运行设置
        settingsView.enableRecordingSettings.IsChecked = DataSet.MaaConfig.GetConfig("recording", false);
        settingsView.enableRecordingSettings.Checked += (_, _) => { DataSet.MaaConfig.SetConfig("recording", true); };
        settingsView.enableRecordingSettings.Unchecked += (_, _) => { DataSet.MaaConfig.SetConfig("recording", false); };

        settingsView.enableSaveDrawSettings.IsChecked = DataSet.MaaConfig.GetConfig("save_draw", false);
        settingsView.enableSaveDrawSettings.Checked += (_, _) => { DataSet.MaaConfig.SetConfig("save_draw", true); };
        settingsView.enableSaveDrawSettings.Unchecked += (_, _) => { DataSet.MaaConfig.SetConfig("save_draw", false); };

        settingsView.showHitDrawSettings.IsChecked = DataSet.MaaConfig.GetConfig("show_hit_draw", false);
        settingsView.showHitDrawSettings.Checked += (_, _) => { DataSet.MaaConfig.SetConfig("show_hit_draw", true); };
        settingsView.showHitDrawSettings.Unchecked += (_, _) => { DataSet.MaaConfig.SetConfig("show_hit_draw", false); };

        settingsView.beforeTaskSettings.Text = DataSet.GetData("Prescript", string.Empty);
        settingsView.beforeTaskSettings.BindLocalization("Prescript");
        settingsView.beforeTaskSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        settingsView.beforeTaskSettings.TextChanged += (_, _) => { DataSet.SetData("Prescript", settingsView.beforeTaskSettings.Text); };

        settingsView.afterTaskSettings.Text = DataSet.GetData("Post-script", string.Empty);
        settingsView.afterTaskSettings.BindLocalization("Post-script");
        settingsView.afterTaskSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        settingsView.afterTaskSettings.TextChanged += (_, _) => { DataSet.SetData("Post-script", settingsView.afterTaskSettings.Text); };

        //启动设置
        settingsView.AutoMinimizeCheckBox.IsChecked = DataSet.GetData("AutoMinimize", false);
        settingsView.AutoMinimizeCheckBox.Checked += (_, _) => { DataSet.SetData("AutoMinimize", true); };
        settingsView.AutoMinimizeCheckBox.Unchecked += (_, _) => { DataSet.SetData("AutoMinimize", false); };

        settingsView.AutoHideCheckBox.IsChecked = DataSet.GetData("AutoHide", false);
        settingsView.AutoHideCheckBox.Checked += (_, _) => { DataSet.SetData("AutoHide", true); };
        settingsView.AutoHideCheckBox.Unchecked += (_, _) => { DataSet.SetData("AutoHide", false); };

        settingsView.SoftwarePathTextBox.Text = DataSet.GetData("SoftwarePath", string.Empty);
        settingsView.SoftwarePathTextBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("SoftwarePath", text);
        };
        settingsView.SoftwarePathTextBox.PreviewDrop += File_Drop;

        settingsView.SoftwarePathSelectButton.Click += (_, _) =>
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "SelectExecutableFile".GetLocalizationString(),
                Filter = "ExeFilter".GetLocalizationString()
            };

            if (openFileDialog.ShowDialog().IsTrue())
            {
                settingsView.SoftwarePathTextBox.Text = openFileDialog.FileName;
            }
        };

        settingsView.ExtrasTextBox.Text = DataSet.GetData("EmulatorConfig", string.Empty);
        settingsView.ExtrasTextBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("EmulatorConfig", text);
        };

        settingsView.WaitSoftwareTimeTextBox.Value = DataSet.GetData("WaitSoftwareTime", 60.0);
        settingsView.ExtrasTextBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("EmulatorConfig", text);
        };
        //切换配置
        string configPath = Path.Combine(Environment.CurrentDirectory, "config");
        foreach (string file in Directory.GetFiles(configPath))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".json") && fileName != "maa_option.json")
            {
                settingsView.swtichConfigs.Items.Add(fileName);
            }
        }

        //连接设置
        SetSettingOption(settingsView.adbCaptureComboBox, "CaptureModeOption",
            [
                "Default", "RawWithGzip", "RawByNetcat",
                "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
                "EmulatorExtras"
            ],
            "AdbControlScreenCapType");
        SetBindSettingOption(settingsView.adbInputComboBox, "InputModeOption",
            ["MiniTouch", "MaaTouch", "AdbInput", "AutoDetect"],
            "AdbControlInputType");

        SetRememberAdbOption(settingsView.rememberAdbButton);

        SetSettingOption(settingsView.win32CaptureComboBox, "CaptureModeOption",
            ["FramePool", "DXGIDesktopDup", "GDI"],
            "Win32ControlScreenCapType");

        SetSettingOption(settingsView.win32InputComboBox, "InputModeOption",
            ["Seize", "SendMessage"],
            "Win32ControlInputType");


        settingsView.swtichConfigs.SelectionChanged += (sender, _) =>
        {
            string selectedItem = (string)settingsView.swtichConfigs.SelectedItem;
            if (selectedItem == "config.json")
            {
                //
            }
            // else if (selectedItem == "maa_option.json")
            // {
            //     // 什么都不做，等待后续添加逻辑
            // }
            else if (selectedItem == "config.json.bak")
            {
                string _currentFile = Path.Combine(configPath, "config.json");
                string _selectedItem = Path.Combine(configPath, "config.json.bak");
                string _bakContent = File.ReadAllText(_selectedItem);
                File.WriteAllText(_currentFile, _bakContent);
                RestartMFA();
            }
            else
            {
                // 恢复成绝对路径
                string _currentFile = Path.Combine(configPath, "config.json");
                string _selectedItem = Path.Combine(configPath, selectedItem);
                SwapFiles(_currentFile, _selectedItem);
                RestartMFA();
            }
        };
        //软件更新
        // settingsView.CdkPassword.UnsafePassword = SimpleEncryptionHelper.Decrypt(DataSet.GetData("DownloadCDK", string.Empty));
        // settingsView.CdkPassword.PasswordChanged += (_, _) => { DataSet.SetData("DownloadCDK", SimpleEncryptionHelper.Encrypt(settingsView.CdkPassword.Password)); };

        settingsView.enableCheckVersionSettings.IsChecked = DataSet.GetData("EnableCheckVersion", true);
        settingsView.enableCheckVersionSettings.Checked += (_, _) => { DataSet.SetData("EnableCheckVersion", true); };
        settingsView.enableCheckVersionSettings.Unchecked += (_, _) => { DataSet.SetData("EnableCheckVersion", false); };

        settingsView.enableAutoUpdateResourceSettings.IsChecked = DataSet.GetData("EnableAutoUpdateResource", false);
        settingsView.enableAutoUpdateResourceSettings.Checked += (_, _) => { DataSet.SetData("EnableAutoUpdateResource", true); };
        settingsView.enableAutoUpdateResourceSettings.Unchecked += (_, _) => { DataSet.SetData("EnableAutoUpdateResource", false); };

        settingsView.enableAutoUpdateMFASettings.IsChecked = DataSet.GetData("EnableAutoUpdateMFA", false);
        settingsView.enableAutoUpdateMFASettings.Checked += (_, _) => { DataSet.SetData("EnableAutoUpdateMFA", true); };
        settingsView.enableAutoUpdateMFASettings.Unchecked += (_, _) => { DataSet.SetData("EnableAutoUpdateMFA", false); };

        if (!string.IsNullOrWhiteSpace(MaaInterface.Instance.RID))
        {
            Data.DownloadSourceList.Add(new("MirrorChyan"));
            Data.DownloadSourceIndex = Data.DownloadSourceIndex;
        }
        //关于我们
        AddAbout();
    }

    private void ConfigureTaskSettingsPanel(object? sender = null, RoutedEventArgs? e = null)
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
        if (DataSet.GetData("AutoStartIndex", 0) >= 1)
        {
            MaaProcessor.Instance.StartSoftware();
        }

        if ((Data?.IsAdb).IsTrue() && DataSet.GetData("RememberAdb", true) && "adb".Equals(MaaProcessor.Config.AdbDevice.AdbPath) && DataSet.TryGetData<JObject>("AdbDevice", out var jObject))
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new AdbInputMethodsConverter());
            settings.Converters.Add(new AdbScreencapMethodsConverter());

            var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
            if (device != null)
            {
                Growls.Process(() =>
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
            Growls.Process(AutoDetectDevice);
    }
    public bool IsConnected()
    {
        return (Data?.IsConnected).IsTrue();
    }

    public void SetConnected(bool isConnected)
    {
        if (Data == null) return;
        Data.IsConnected = isConnected;
    }

    public void SetUpdating(bool isUpdating)
    {
        if (Data == null) return;
        Data.IsUpdating = isUpdating;
    }

    public bool ConfirmExit()
    {
        if (!Data.IsRunning)
            return true;
        var result = MessageBoxHelper.Show("ConfirmExitText".GetLocalizationString(),
            "ConfirmExitTitle".GetLocalizationString(), buttons: MessageBoxButton.YesNo, icon: MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    private void File_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (sender is TextBox textBox)
            textBox.Text = files?[0] ?? string.Empty;
    }
}
