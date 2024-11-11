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
using WPFLocalizeExtension.Extensions;
using ComboBox = HandyControl.Controls.ComboBox;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TabControl = System.Windows.Controls.TabControl;
using TabItem = System.Windows.Controls.TabItem;
using TextBox = HandyControl.Controls.TextBox;
using System.Collections.ObjectModel;
using MFAWPF.Controls;

namespace MFAWPF.Views;

public partial class MainWindow
{
    public static MainWindow? Instance { get; private set; }
    private readonly MaaToolkit _maaToolkit;

    public static MainViewModel? Data { get; private set; }

    public static readonly string Version =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

    public Dictionary<string, TaskModel> TaskDictionary = new();

    private readonly MFAWPF.Utils.PresetManager _presetManager = new();

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        version.Text = Version;
        _maaToolkit = new MaaToolkit(init: true);
        Data = DataContext as MainViewModel;
        Loaded += (_, _) => { LoadUI(); };
        InitializeData();
        OCRHelper.Initialize();
        VersionChecker.CheckVersion();

        MaaProcessor.Instance.TaskStackChanged += OnTaskStackChanged;

        SetIconFromExeDirectory();
    }

    private void SetIconFromExeDirectory()
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string iconPath = Path.Combine(exeDirectory, "logo.ico");

        if (File.Exists(iconPath))
        {
            Icon = new BitmapImage(new Uri(iconPath));
        }
    }

    private bool InitializeData()
    {
        DataSet.Data = JsonHelper.ReadFromConfigJsonFile("config", new Dictionary<string, object>());
        if (!File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}/interface.json"))
        {
            Console.WriteLine("未找到interface文件，生成interface.json...");
            LoggerService.LogInfo("未找到interface文件，生成interface.json...");
            MaaInterface.Instance = new MaaInterface
            {
                Version = "1.0",
                Name = "Debug",
                Task = new List<TaskInterfaceItem>(),
                Resource = new List<MaaInterface.MaaCustomResource>
                {
                    new()
                    {
                        Name = "默认", Path = new List<string> { "{PROJECT_DIR}/resource/base" }
                    }
                },
                Recognition = new Dictionary<string, MaaInterface.CustomExecutor>(),
                Action = new Dictionary<string, MaaInterface.CustomExecutor>(),
                Option = new Dictionary<string, MaaInterface.MaaInterfaceOption>
                {
                    {
                        "测试", new MaaInterface.MaaInterfaceOption()
                        {
                            Cases = new List<MaaInterface.MaaInterfaceOptionCase>
                            {
                                new()
                                {
                                    Name = "测试1", PipelineOverride = new Dictionary<string, TaskModel>()
                                },
                                new()
                                {
                                    Name = "测试2", PipelineOverride = new Dictionary<string, TaskModel>()
                                }
                            }
                        }
                    }
                }
            };
            JsonHelper.WriteToJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface",
                MaaInterface.Instance, new MaaInterfaceSelectOptionConverter(true));
        }
        else
        {
            MaaInterface.Instance =
                JsonHelper.ReadFromJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface",
                    new MaaInterface(),
                    () => { }, new MaaInterfaceSelectOptionConverter(false));
        }


        if (MaaInterface.Instance != null)
        {
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
                if (MaaInterface.Instance?.Resources != null &&
                    MaaInterface.Instance.Resources.Count > DataSet.GetData("ResourceIndex", 0))
                    MaaProcessor.CurrentResources =
                        MaaInterface.Instance.Resources[
                            MaaInterface.Instance.Resources.Keys.ToList()[DataSet.GetData("ResourceIndex", 0)]];
                else MaaProcessor.CurrentResources = new List<string> { MaaProcessor.ResourceBase };
                FirstTask = false;
            }

            Data?.TasksSource.Add(dragItem);
        }

        if (Data?.TaskItemViewModels.Count == 0)
        {
            var items = DataSet.GetData("TaskItems",
                new List<TaskInterfaceItem>()) ?? new List<TaskInterfaceItem>();
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

    public void ToggleTaskButtonsVisibility(bool isRunning)
    {
        Growls.Process(() =>
        {
            startButton.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
            startButton.IsEnabled = !isRunning;
            stopButton.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            stopButton.IsEnabled = isRunning;
        });
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
                                Growls.ErrorGlobal(string.Format(
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
                string? directoryPath = Path.GetDirectoryName($"{MaaProcessor.ResourceBase}/pipeline");
                if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(directoryPath);
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
            Growls.ErrorGlobal(string.Format(LocExtension.GetLocalizedValue<string>("PipelineLoadError"),
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

    private void ValidateNextTasks(Dictionary<string, TaskModel> taskDictionary, object? nextTasks,
        string name = "next")
    {
        if (nextTasks is List<string> tasks)
        {
            foreach (var task in tasks)
            {
                if (!taskDictionary.ContainsKey(task))
                {
                    Growls.ErrorGlobal(string.Format(LocExtension.GetLocalizedValue<string>("TaskNotFoundError"),
                        name, task));
                }
            }
        }
    }

    private void Start(object? sender, RoutedEventArgs? e)
    {
        if (InitializeData())
        {
            MaaProcessor.Money = 0;
            var tasks = Data?.TaskItemViewModels.ToList().FindAll(task => task.IsChecked);
            ConnectToMAA();
            MaaProcessor.Instance.Start(tasks);
        }
    }

    private void Stop(object sender, RoutedEventArgs e)
    {
        MaaProcessor.Instance.Stop();
    }

    private async void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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
            deviceComboBox.ItemsSource = new List<AdbDeviceInfo> { dialog.Output };
            deviceComboBox.SelectedIndex = 0;
            MaaProcessor.Config.IsConnected = true;
        }
    }

    public bool IsFirstStart = true;

    public bool TryGetIndexFromConfig(string config, out int index)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("extras", out JsonElement extras) &&
                extras.TryGetProperty("mumu", out JsonElement mumu) &&
                mumu.TryGetProperty("index", out JsonElement indexElement))
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

    public async void AutoDetectDevice()
    {
        try
        {
            Growl.Info((Data?.IsAdb).IsTrue()
                ? LocExtension.GetLocalizedValue<string>("EmulatorDetectionStarted")
                : LocExtension.GetLocalizedValue<string>("WindowDetectionStarted"));
            MaaProcessor.Config.IsConnected = false;
            if ((Data?.IsAdb).IsTrue())
            {
                var devices = await _maaToolkit.AdbDevice.FindAsync();
                deviceComboBox.ItemsSource = devices;
                MaaProcessor.Config.IsConnected = devices.Count > 0;
                var emulatorConfig = DataSet.GetData("EmulatorConfig", string.Empty);
                if (!string.IsNullOrWhiteSpace(emulatorConfig))
                {
                    var extractedNumber = ExtractNumberFromEmulatorConfig(emulatorConfig);

                    foreach (var device in devices)
                    {
                        if (TryGetIndexFromConfig(device.Config, out int index))
                        {
                            if (index == extractedNumber)
                            {
                                deviceComboBox.SelectedIndex = devices.IndexOf(device);
                            }
                        }
                        else deviceComboBox.SelectedIndex = 0;
                    }
                }
                else
                    deviceComboBox.SelectedIndex = 0;
            }
            else
            {
                var windows = _maaToolkit.Desktop.Window.Find();
                deviceComboBox.ItemsSource = windows;
                MaaProcessor.Config.IsConnected = windows.Count > 0;
                deviceComboBox.SelectedIndex = windows.Count > 0
                    ? windows.ToList().FindIndex(win => !string.IsNullOrWhiteSpace(win.Name))
                    : 0;
            }

            if (!MaaProcessor.Config.IsConnected)
            {
                Growl.Info((Data?.IsAdb).IsTrue()
                    ? LocExtension.GetLocalizedValue<string>("NoEmulatorFound")
                    : LocExtension.GetLocalizedValue<string>("NoWindowFound"));
            }
        }
        catch (Exception ex)
        {
            Growls.WarningGlobal(string.Format(LocExtension.GetLocalizedValue<string>("TaskStackError"),
                (Data?.IsAdb).IsTrue() ? "Emulator".GetLocalizationString() : "Window".GetLocalizationString(),
                ex.Message));
            MaaProcessor.Config.IsConnected = false;
            LoggerService.LogError(ex);
            Console.WriteLine(ex);
        }
    }

    private void ConfigureSettingsPanel(object? sender = null, RoutedEventArgs? e = null)
    {
        settingPanel.Children.Clear();


        var tabControl = new TabControl
        {
            TabStripPlacement = Dock.Bottom, Height = 400,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Stretch,
            Style = (Style)FindResource("TabControlCapsule")
        };
        Binding heightBinding = new Binding("ActualHeight")
        {
            Source = settingPanel,
            Converter = new SubtractConverter(),
            ConverterParameter = "20"
        };
        tabControl.SetBinding(HeightProperty, heightBinding);

        StackPanel s1 = new() { Margin = new Thickness(2) }, s2 = new() { Margin = new Thickness(2) };
        AddResourcesOption(s1);

        AddAutoStartOption(s2);
        if ((Data?.IsAdb).IsTrue())
        {
            AddSettingOption(s1, "CaptureModeOption",
                [
                    "Default", "RawWithGzip", "RawByNetcat",
                    "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
                    "EmulatorExtras"
                ],
                "AdbControlScreenCapType");
            AddBindSettingOption(s1, "InputModeOption",
                ["MiniTouch", "MaaTouch", "AdbInput", "AutoDetect"],
                "AdbControlInputType");
            AddAfterTaskOption(s2);

            AddStartSettingOption(s2);
            //AddSwitchConfiguration(s2);
            //AddStartExtrasOption(s2);
            AddStartEmulatorOption(s2);
            AddRememberAdbOption(s2);

            //AddIntroduction(s2,
            //"[size:24][b][color:blue]这是一个蓝色的大标题[/color][/b][/size]\n[color:green][i]这是绿色的斜体文本。[/i][/color]\n[u]这是带有下划线的文本。[/u]\n[s]这是带有删除线的文本。[/s]\n[b][color:red]这是红色的粗体文本。[/color][/b]\n[size:18]这是一个较小的字号文本，字号为18。[/size]\n");
        }
        else
        {
            AddSettingOption(s1, "CaptureModeOption",
                ["FramePool", "DXGIDesktopDup", "GDI"],
                "Win32ControlScreenCapType");

            AddSettingOption(s1, "InputModeOption",
                ["Seize", "SendMessage"],
                "Win32ControlInputType");
        }

        AddThemeOption(s1);
        AddLanguageOption(s1);
        AddGpuOption(s2);
        ScrollViewer sv1 = new()
            {
                Content = s1, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            },
            sv2 = new()
            {
                Content = s2, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        var commonSettingTabItem = new TabItem
        {
            Content = sv1
        };
        commonSettingTabItem.BindLocalization("CommonSetting", HeaderedContentControl.HeaderProperty);
        var advancedSettingTabItem = new TabItem
        {
            Content = sv2
        };
        advancedSettingTabItem.BindLocalization("AdvancedSetting", HeaderedContentControl.HeaderProperty);


        tabControl.Items.Add(commonSettingTabItem);
        tabControl.Items.Add(advancedSettingTabItem);

        settingPanel.Children.Add(tabControl);
    }

    private void AddSwitchConfiguration(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };
        string configPath = Path.Combine(Environment.CurrentDirectory, "presets");
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

    private void About(object? sender = null, RoutedEventArgs? e = null)
    {
        settingPanel.Children.Clear();
        StackPanel s1 = new()
            {
                Orientation = Orientation.Horizontal, Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            },
            s2 = new()
            {
                Orientation = Orientation.Horizontal, Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        var t1 = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        t1.BindLocalization("ProjectLink", TextBlock.TextProperty);
        s1.Children.Add(t1);
        s1.Children.Add(new Shield
        {
            Status = "MFAWPF", Subject = "Github", Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Command = ControlCommands.OpenLink,
            CommandParameter = "https://github.com/SweetSmellFox/MFAWPF"
        });
        var resourceLink = MaaInterface.Instance?.Url;
        if (!string.IsNullOrWhiteSpace(resourceLink))
        {
            var t2 = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
            t2.BindLocalization("ResourceLink", TextBlock.TextProperty);
            s2.Children.Add(t2);
            s2.Children.Add(new Shield
            {
                Status = MaaInterface.Instance?.Name ?? "Resource", Subject = "Github",
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Command = ControlCommands.OpenLink,
                CommandParameter = resourceLink
            });
        }

        settingPanel.Children.Add(s1);
        settingPanel.Children.Add(s2);
    }

    private void AddResourcesOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            SelectedIndex = DataSet.GetData("ResourceIndex", defaultValue), DisplayMemberPath = "Name",
            Style = FindResource("ComboBoxExtend") as Style,
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
            comboBox.ItemsSource = MaaInterface.Instance.Resource;
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
            Margin = new Thickness(5)
        };

        comboBox.ItemsSource = new List<string> { "简体中文", "English" };
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
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            LanguageManager.ChangeLanguage(
                CultureInfo.CreateSpecificCulture(index == 0 ? "zh-cn" : "en-us"));
            DataSet.SetData("LangIndex", index);
        };

        comboBox.SelectedIndex = DataSet.GetData("LangIndex", defaultValue);
        panel.Children.Add(comboBox);
    }

    private void AddSettingOption(Panel? panel, string titleKey, IEnumerable<string> options, string datatype,
        int defaultValue = 0)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = options,
            SelectedIndex = DataSet.GetData(datatype, defaultValue),
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };
        var binding = new Binding("Idle")
        {
            Source = Data,
            Mode = BindingMode.OneWay
        };
        comboBox.SetBinding(IsEnabledProperty, binding);
        comboBox.BindLocalization(titleKey);
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData(datatype, index);
            MaaProcessor.Instance.SetCurrentTasker();
        };

        panel?.Children.Add(comboBox);
    }

    private void AddBindSettingOption(Panel? panel, string titleKey, IEnumerable<string> options, string datatype,
        int defaultValue = 0)

    {
        var comboBox = new ComboBox
        {
            SelectedIndex = DataSet.GetData(datatype, defaultValue),
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };
        var binding = new Binding("Idle")
        {
            Source = Data,
            Mode = BindingMode.OneWay
        };
        comboBox.SetBinding(IsEnabledProperty, binding);
        foreach (var s in options)
        {
            var comboBoxItem = new ComboBoxItem();
            comboBoxItem.BindLocalization(s, ContentProperty);
            comboBox.Items.Add(comboBoxItem);
        }

        comboBox.BindLocalization(titleKey);
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData(datatype, index);
            MaaProcessor.Instance.SetCurrentTasker();
        };

        panel?.Children.Add(comboBox);
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
        panel.Children.Add(textBox);
    }


    private void AddIntroduction(Panel? panel = null, string input = "")
    {
        panel ??= settingPanel;

        RichTextBox richTextBox = new RichTextBox
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsReadOnly = true
        };

        FlowDocument flowDocument = new FlowDocument();
        Paragraph paragraph = new Paragraph();

        string pattern = @"\[(?<tag>[^\]]+):(?<value>[^\]]*)\](?<content>.*?)\[/\k<tag>\]";
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
            Margin = new Thickness(5)
        };
        var c1 = new ComboBoxItem();
        c1.BindLocalization("None", ContentProperty);
        var c2 = new ComboBoxItem();
        c2.BindLocalization("StartupScript", ContentProperty);
        comboBox.Items.Add(c1);
        comboBox.Items.Add(c2);

        comboBox.BindLocalization("AutoStartOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData("AutoStartIndex", index);
        };
        comboBox.SelectedIndex = DataSet.GetData("AutoStartIndex", defaultValue);
        panel.Children.Add(comboBox);
    }

    private void AddAfterTaskOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= settingPanel;
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };
        var c1 = new ComboBoxItem();
        c1.BindLocalization("None", ContentProperty);
        var c2 = new ComboBoxItem();
        c2.BindLocalization("CloseMFA", ContentProperty);
        var c3 = new ComboBoxItem();
        c3.BindLocalization("CloseEmulator", ContentProperty);
        var c4 = new ComboBoxItem();
        c4.BindLocalization("CloseEmulatorAndMFA", ContentProperty);
        var c5 = new ComboBoxItem();
        c5.BindLocalization("ShutDown", ContentProperty);
        var c6 = new ComboBoxItem();
        c6.BindLocalization("CloseEmulatorAndRestartMFA", ContentProperty);
        var c7 = new ComboBoxItem();
        c7.BindLocalization("Restart", ContentProperty);
        var c8 = new ComboBoxItem();
        c8.BindLocalization("DingTalkMessageAsync", ContentProperty);
        comboBox.Items.Add(c1);
        comboBox.Items.Add(c2);
        comboBox.Items.Add(c3);
        comboBox.Items.Add(c4);
        comboBox.Items.Add(c5);
        comboBox.Items.Add(c6);
        comboBox.Items.Add(c7);
        comboBox.Items.Add(c8);
        comboBox.BindLocalization("AfterTaskOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData("AfterTaskIndex", index);
        };
        comboBox.SelectedIndex = DataSet.GetData("AfterTaskIndex", defaultValue);
        panel.Children.Add(comboBox);
    }

    private void AddGpuOption(Panel? panel = null)
    {
        panel ??= settingPanel;
        var checkBox = new CheckBox
        {
            IsChecked = DataSet.GetData("EnableGPU", true), Margin = new Thickness(5)
        };
        checkBox.BindLocalization("EnableGPU", ContentProperty);
        checkBox.Click += (_, _) => { DataSet.SetData("EnableGPU", checkBox.IsChecked); };
        panel.Children.Add(checkBox);
    }

    private void AddRememberAdbOption(Panel? panel = null)
    {
        panel ??= settingPanel;
        var checkBox = new CheckBox
        {
            IsChecked = DataSet.GetData("RememberAdb", true), Margin = new Thickness(5)
        };
        checkBox.BindLocalization("RememberAdb", ContentProperty);
        checkBox.Click += (_, _) => { DataSet.SetData("RememberAdb", checkBox.IsChecked); };
        panel.Children.Add(checkBox);
    }

    private void AddStartSettingOption(Panel? panel = null)
    {
        panel ??= settingPanel;
        // var binding = new Binding("Idle")
        // {
        //     Source = Data,
        //     Mode = BindingMode.OneWay
        // };

        var checkBox = new CheckBox
        {
            IsChecked = DataSet.GetData("StartEmulator", false), Margin = new Thickness(5)
        };
        checkBox.BindLocalization("StartEmulator", ContentProperty);
        checkBox.Click += (_, _) => { DataSet.SetData("StartEmulator", checkBox.IsChecked); };
        // checkBox.SetBinding(IsEnabledProperty, binding);


        var grid = new Grid { Margin = new Thickness(5) };

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
            Text = DataSet.GetData("EmulatorPath", string.Empty), HorizontalAlignment = HorizontalAlignment.Stretch
        };
        t1.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("EmulatorPath", text);
        };
        t1.SetValue(InfoElement.ShowClearButtonProperty, true);
        t1.BindLocalization("EmulatorPath");
        Grid.SetColumn(t1, 0);


        var path = new System.Windows.Shapes.Path
        {
            Width = 15, MaxWidth = 15, Stretch = Stretch.Uniform, Data = FindResource("LoadGeometry") as Geometry,
            Fill = FindResource("GrayColor4") as Brush
        };

        // var b1 = new Binding("GrayColor4")
        // {
        //     Source = this
        // };
        // button.SetBinding(System.Windows.Shapes.Path.FillProperty, b1);
        var button = new Button { Content = path, VerticalAlignment = VerticalAlignment.Bottom };
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
            Margin = new Thickness(5), Value = DataSet.GetData("WaitEmulatorTime", 60.0),
            Style = FindResource("NumericUpDownExtend") as Style
        };

        numericUpDown.BindLocalization("WaitEmulator");
        numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        // numericUpDown.SetBinding(IsEnabledProperty, binding);
        numericUpDown.ValueChanged += (sender, _) =>
        {
            var value = (sender as NumericUpDown)?.Value ?? 60;
            DataSet.SetData("WaitEmulatorTime", value);
        };
        panel.Children.Add(checkBox);
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

            if (!string.IsNullOrWhiteSpace(dragItem.InterfaceItem.Document))
            {
                AddIntroduction(s1, Regex.Unescape(dragItem.InterfaceItem.Document));
            }

            var sc1 = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = s1
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
                ComboBox comboBox = new ComboBox
                {
                    SelectedIndex = option.Index ?? 0, Style = FindResource("ComboBoxExtend") as Style,
                    DisplayMemberPath = "Name", Margin = new Thickness(5),
                };

                var multiBinding = new MultiBinding
                {
                    Converter = FindResource("CustomIsEnabledConverter") as IMultiValueConverter,
                    Mode = BindingMode.OneWay
                };

                multiBinding.Bindings.Add(new Binding("IsCheckedWithNull") { Source = source });
                multiBinding.Bindings.Add(new Binding("Idle") { Source = Data });

                comboBox.SetBinding(IsEnabledProperty, multiBinding);

                comboBox.ItemsSource = interfaceOption.Cases;

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
                Value = source.InterfaceItem.RepeatCount ?? 1, Style = FindResource("NumericUpDownPlus") as Style,
                Margin = new Thickness(5), Increment = 1, Minimum = -1, DecimalPlaces = 0
            };

            var multiBinding = new MultiBinding
            {
                Converter = FindResource("CustomIsEnabledConverter") as IMultiValueConverter,
                Mode = BindingMode.OneWay
            };

            multiBinding.Bindings.Add(new Binding("IsCheckedWithNull") { Source = source });
            multiBinding.Bindings.Add(new Binding("Idle") { Source = Data });

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
        if (!MaaProcessor.Config.IsConnected)
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
            TabControl.SelectedIndex = MaaInterface.Instance?.DefaultController == "win32" ? 1 : 0;
            WaitEmulator();

            TabControl.SelectionChanged += TabControl_OnSelectionChanged;
            if (Data != null)
                Data.NotLock = MaaInterface.Instance?.LockController != true;
            ConnectSettingButton.IsChecked = true;
            var value = DataSet.GetData("EnableEdit", true);
            if (!value)
                EditButton.Visibility = Visibility.Collapsed;
            DataSet.SetData("EnableEdit", value);
        });
    }

    public void WaitEmulator()
    {
        Task.Run(
            async () =>
            {
                if (DataSet.GetData("StartEmulator", false))
                {
                    await MaaProcessor.Instance.StartEmulator();
                }

                if ((Data?.IsAdb).IsTrue() && DataSet.GetData("RememberAdb", true) &&
                    "adb".Equals(MaaProcessor.Config.AdbDevice.AdbPath) &&
                    DataSet.TryGetData<JObject>("AdbDevice", out var jObject))
                {
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new AdbInputMethodsConverter());
                    settings.Converters.Add(new AdbScreencapMethodsConverter());

                    var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
                    if (device != null)
                    {
                        Growls.Process(() =>
                        {
                            deviceComboBox.ItemsSource = new List<AdbDeviceInfo> { device };
                            deviceComboBox.SelectedIndex = 0;
                            MaaProcessor.Config.IsConnected = true;
                            if (DataSet.GetData("AutoStartIndex", 0) == 1)
                            {
                                if (MaaProcessor.Instance.ShouldEndStart)
                                {
                                    MaaProcessor.Instance.EndAutoStart();
                                }
                                else
                                {
                                    Start(null, null);
                                }
                            }
                        });
                    }
                }
                else
                    Growls.Process(() =>
                    {
                        AutoDetectDevice();
                        if ((Data?.IsAdb).IsTrue() && DataSet.GetData("AutoStartIndex", 0) == 1)
                        {
                            if (MaaProcessor.Instance.ShouldEndStart)
                            {
                                MaaProcessor.Instance.EndAutoStart();
                            }
                            else
                            {
                                Start(null, null);
                            }
                        }
                    });
            });
    }

    private void ToggleWindowTopMost(object sender, RoutedPropertyChangedEventArgs<bool> e)
    {
        Topmost = e.NewValue;
    }

    private async void SavePreset(object sender, RoutedEventArgs e)
    {
        try 
        {
            LoggerService.LogInfo("开始保存预设流程");
            
            var inputDialog = new InputDialog
            {
                Title = "保存预设",
                Message = "请输入预设名称:"
            };

            LoggerService.LogInfo("显示输入对话框");
            if (inputDialog.ShowDialog() == true)
            {
                string presetName = inputDialog.InputText;
                LoggerService.LogInfo($"用户输入的预设名称: {presetName}");
                
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    LoggerService.LogInfo("预设名称为空，终止保存");
                    Growl.Warning("预设名称不能为空");
                    return;
                }

                LoggerService.LogInfo($"开始调用 PresetManager.SavePreset: {presetName}");
                await _presetManager.SavePreset(presetName);
                LoggerService.LogInfo("预设保存完成");
            }
            else
            {
                LoggerService.LogInfo("用户取消了保存预设操作");
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"保存预设时发生异常: {ex}");
            Growl.Error($"保存预设失败: {ex.Message}");
        }
    }

    private async void LoadPreset(object sender, RoutedEventArgs e)
    {
        var presets = _presetManager.GetPresetNames();
        if (!presets.Any())
        {
            Growl.Warning("没有可用的预设");
            return;
        }

        var dialog = new PresetSelectDialog(presets);
        if (dialog.ShowDialog() == true && dialog.SelectedPreset != null)
        {
            string selectedPreset = dialog.SelectedPreset;
            var maaInterface = await _presetManager.LoadPreset(selectedPreset);
            if (maaInterface != null)
            {
                MaaInterface.Instance = maaInterface;
                
                // 重新加载配置
                DataSet.Data = JsonHelper.ReadFromConfigJsonFile("config", new Dictionary<string, object>());

                // 重新初始化界面
                RestartMFA();
 
            }
        }
    }
}