using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Interactivity;
using HandyControl.Themes;
using MaaFramework.Binding;
using MFAWPF.Controls;
using MFAWPF.Data;
using MFAWPF.Utils;
using MFAWPF.Utils.Converters;
using MFAWPF.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLocalizeExtension.Extensions;
using ComboBox = HandyControl.Controls.ComboBox;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TabControl = System.Windows.Controls.TabControl;
using TabItem = System.Windows.Controls.TabItem;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MFAWPF.Views
{
    public partial class MainWindow
    {
        public static MainWindow? Instance { get; private set; }
        private readonly MaaToolkit _maaToolkit;
        public bool IsADB { get; set; } = true;

        public static MainViewModel? Data { get; private set; }

        public static readonly string Version =
            $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

        public Dictionary<string, TaskModel> TaskDictionary = new();

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            version.Text = Version;
            _maaToolkit = new MaaToolkit(init: true);
            Data = DataContext as MainViewModel;

            InitializeData();
            OCRHelper.Initialize();
            VersionChecker.CheckVersion();
            LoadUI();
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
                logo.Source = Icon;
            }
        }

        private bool InitializeData()
        {
            DataSet.Data = JsonHelper.ReadFromConfigJsonFile("config", new Dictionary<string, object>());
            if (!File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}/interface.json"))
            {
                try
                {
                    File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}/interface.json",
                        JsonConvert.SerializeObject(new MaaInterface()
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
                                                Name = "测试1", Pipeline_Override = new Dictionary<string, TaskModel>()
                                            },
                                            new()
                                            {
                                                Name = "测试2", Pipeline_Override = new Dictionary<string, TaskModel>()
                                            }
                                        }
                                    }
                                }
                            }
                        }, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Include
                        }));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建文件时发生错误: {ex.Message}");
                    LoggerService.LogError(ex);
                }
            }

            MaaInterface.Instance =
                JsonHelper.ReadFromJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface", new MaaInterface(),
                    () => { });
            if (MaaInterface.Instance != null)
            {
                Data?.TasksSource.Clear();
                LoadTasks(MaaInterface.Instance.Task ?? new List<TaskInterfaceItem>());
            }

            ConnectToMAA();
            return LoadTask();
        }


        public bool firstTask = true;

        private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks)
        {
            foreach (var task in tasks)
            {
                var dragItem = new DragItemViewModel(task)
                {
                    IsCheckedWithNull = task.check ?? false,
                    SettingVisibility = task.repeatable == true || task.option?.Count > 0
                        ? Visibility.Visible
                        : Visibility.Hidden
                };

                if (firstTask)
                {
                    if (MaaInterface.Instance?.Resources != null &&
                        MaaInterface.Instance.Resources.Count > DataSet.GetData("ResourceIndex", 0))
                        MaaProcessor.CurrentResources =
                            MaaInterface.Instance.Resources[
                                MaaInterface.Instance.Resources.Keys.ToList()[DataSet.GetData("ResourceIndex", 0)]];
                    else MaaProcessor.CurrentResources = new List<string> { MaaProcessor.ResourceBase };
                    firstTask = false;
                }

                Data?.TasksSource.Add(dragItem);
            }

            if (Data?.TaskItemViewModels.Count == 0)
            {
                Data.TaskItemViewModels.AddRange(DataSet.GetData("Tasks",
                    new List<DragItemViewModel>()));
                if (Data.TaskItemViewModels.Count == 0 && Data.TasksSource != null && Data.TasksSource.Count != 0)
                {
                    foreach (var VARIABLE in Data.TasksSource)
                        Data.TaskItemViewModels.Add(VARIABLE);
                }
            }
        }

        private void OnTaskStackChanged(object? sender, EventArgs e)
        {
            ToggleTaskButtonsVisibility(isRunning: MaaProcessor.Instance.TaskQueue.Count > 0);
        }

        private void ToggleTaskButtonsVisibility(bool isRunning)
        {
            // 检查是否需要使用 Dispatcher
            if (Dispatcher.CheckAccess())
            {
                // 当前线程是 UI 线程，直接执行
                startButton.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
                startButton.IsEnabled = !isRunning;
                stopButton.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
                stopButton.IsEnabled = isRunning;
            }
            else
            {
                // 如果当前线程不是 UI 线程，通过 Dispatcher 调度到 UI 线程执行
                Dispatcher.Invoke(() => ToggleTaskButtonsVisibility(isRunning));
            }
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
                Data?.SourceItems.Add(new TaskItemViewModel { Task = task.Value });
            }
        }

        private void ValidateTaskLinks(Dictionary<string, TaskModel> taskDictionary,
            KeyValuePair<string, TaskModel> task)
        {
            ValidateNextTasks(taskDictionary, task.Value.Next);
            ValidateNextTasks(taskDictionary, task.Value.On_Error, "on_error");
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

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsADB = adbTab.IsSelected;

            if (IsFirstStart && "adb".Equals(MaaProcessor.Config.Adb.Adb) &&
                DataSet.TryGetData<JObject>("Adb", out var jObject))
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new AdbInputMethodsConverter());
                settings.Converters.Add(new AdbScreencapMethodsConverter());

                var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
                if (device != null)
                {
                    deviceComboBox.ItemsSource = new List<AdbDeviceInfo> { device };
                    deviceComboBox.SelectedIndex = 0;
                    MaaProcessor.Config.IsConnected = true;
                    if (DataSet.GetData("AutoStartIndex", 0) == 1)
                        Start(null, null);
                }

                IsFirstStart = false;
            }
            else AutoDetectDevice();

            MaaProcessor.Instance.SetCurrentTasker();
            if (ConnectSettingButton.IsChecked == true)
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
                MaaProcessor.Config.Win32.HWnd = window.Handle;
                MaaProcessor.Instance.SetCurrentTasker();
            }
            else if (deviceComboBox.SelectedItem is AdbDeviceInfo device)
            {
                Growl.Info(string.Format(LocExtension.GetLocalizedValue<string>("EmulatorSelectionMessage"),
                    device.Name));
                MaaProcessor.Config.Adb.Adb = device.AdbPath;
                MaaProcessor.Config.Adb.AdbAddress = device.AdbSerial;
                MaaProcessor.Config.Adb.AdbConfig = device.Config;
                MaaProcessor.Instance.SetCurrentTasker();
                DataSet.SetData("Adb", device);
            }
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            AutoDetectDevice();
        }

        public bool IsFirstStart = true;

        public async void AutoDetectDevice()
        {
            try
            {
                Growl.Info(IsADB
                    ? LocExtension.GetLocalizedValue<string>("EmulatorDetectionStarted")
                    : LocExtension.GetLocalizedValue<string>("WindowDetectionStarted"));
                MaaProcessor.Config.IsConnected = false;
                if (IsADB)
                {
                    var devices = await _maaToolkit.AdbDevice.FindAsync();
                    deviceComboBox.ItemsSource = devices;
                    MaaProcessor.Config.IsConnected = devices.Count > 0;
                    deviceComboBox.SelectedIndex = 0;
                    if (IsFirstStart && DataSet.GetData("AutoStartIndex", 0) == 1)
                    {
                        Start(null, null);
                        IsFirstStart = false;
                    }
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
                    Growl.Info(IsADB
                        ? LocExtension.GetLocalizedValue<string>("NoEmulatorFound")
                        : LocExtension.GetLocalizedValue<string>("NoWindowFound"));
                }
            }
            catch (Exception ex)
            {
                Growls.WarningGlobal(string.Format(LocExtension.GetLocalizedValue<string>("TaskStackError"),
                    IsADB ? "Simulator".GetLocalizationString() : "Window".GetLocalizationString(), ex.Message));
                MaaProcessor.Config.IsConnected = false;
                LoggerService.LogError(ex);
                Console.WriteLine(ex);
            }
        }

        private void ConfigureSettingsPanel(object? sender = null, RoutedEventArgs? e = null)
        {
            settingPanel.Children.Clear();
// Create a new TabControl instance
            TabControl tabControl = new TabControl
            {
                TabStripPlacement = Dock.Bottom,
                Background = Brushes.Transparent,
                Height = 350,
                BorderThickness = new Thickness(0),
                Style = (Style)FindResource("TabControlCapsule") // Assuming 'TabControlCapsule' is a style in resources
            };
            // var binding = new Binding("Idle")
            // {
            //     Source = Data,
            //     Mode = BindingMode.OneWay
            // };
            // tabControl.SetBinding(IsEnabledProperty, binding);
            StackPanel s1 = new(), s2 = new();
            AddResourcesOption(s1);
            if (IsADB)
            {
                AddSettingOption(s1, "CaptureModeOption",
                    [
                        "Default", "RawWithGzip", "RawByNetcat",
                        "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
                        "EmulatorExtras"
                    ],
                    "AdbControlScreenCapType", 0);
                AddBindSettingOption(s1, "InputModeOption",
                    ["MiniTouch", "MaaTouch", "AdbInput", "AutoDetect"],
                    "AdbControlInputType", 0);
            }
            else
            {
                AddSettingOption(s1, "CaptureModeOption",
                    ["FramePool", "DXGIDesktopDup", "GDI"],
                    "Win32ControlScreenCapType", 0);

                AddSettingOption(s1, "InputModeOption",
                    ["Seize", "SendMessage"],
                    "Win32ControlInputType", 0);
            }

            AddThemeOption(s1);
            AddLanguageOption(s1);
            AddAutoStartOption(s2);
            ScrollViewer sv1 = new()
                {
                    Content = s1, VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                sv2 = new()
                {
                    Content = s2, VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
            var commonSettingTabItem = new TabItem
            {
                Content = sv1
            };
            commonSettingTabItem.BindLocalization("CommonSetting", TabItem.HeaderProperty);
            var advancedSettingTabItem = new TabItem
            {
                Content = sv2
            };
            advancedSettingTabItem.BindLocalization("AdvancedSetting", TabItem.HeaderProperty);


            tabControl.Items.Add(commonSettingTabItem);
            tabControl.Items.Add(advancedSettingTabItem);

            settingPanel.Children.Add(tabControl);
        }

        private void About(object? sender = null, RoutedEventArgs? e = null)
        {
            settingPanel.Children.Clear();
            settingPanel.Children.Add(new Shield
            {
                Status = "MFAWPF", Subject = "Github", Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Command = ControlCommands.OpenLink,
                CommandParameter = "https://github.com/SweetSmellFox/MFAWPF"
            });
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
            comboBox.Items.Add(light);
            comboBox.Items.Add(dark);
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
                ThemeManager.Current.ApplicationTheme = index == 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
                DataSet.SetData("ThemeIndex", index);
            };
            comboBox.SelectedIndex = DataSet.GetData("ThemeIndex", defaultValue);
            panel.Children.Add(comboBox);
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
            var binding = new Binding("Idle")
            {
                Source = Data,
                Mode = BindingMode.OneWay
            };
            comboBox.SetBinding(IsEnabledProperty, binding);
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

        public void SetOption(DragItemViewModel dragItem, bool value)
        {
            if (dragItem.InterfaceItem != null && value)
            {
                settingPanel.Children.Clear();
                AddRepeatOption(dragItem);
                if (dragItem.InterfaceItem.option != null)
                {
                    foreach (var option in dragItem.InterfaceItem.option)
                        AddOption(option, dragItem);
                }
            }
        }

        private void AddOption(MaaInterface.MaaInterfaceSelectOption option, DragItemViewModel source)
        {
            if (MaaInterface.Instance != null && MaaInterface.Instance.Option != null && option.Name != null)
            {
                if (MaaInterface.Instance.Option.TryGetValue(option.Name, out var interfaceOption))
                {
                    ComboBox comboBox = new ComboBox
                    {
                        SelectedIndex = option.Index ?? 0, Style = FindResource("ComboBoxExtend") as Style,
                        DisplayMemberPath = "name", Margin = new Thickness(5),
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

                        DataSet.SetData("Tasks", Data?.TaskItemViewModels.ToList());
                    };
                    comboBox.SetValue(InfoElement.TitleProperty, option.Name);
                    comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
                    settingPanel.Children.Add(comboBox);
                }
            }
        }

        private void AddRepeatOption(DragItemViewModel source)
        {
            if (source.InterfaceItem is { repeatable: true })
            {
                NumericUpDown numericUpDown = new NumericUpDown
                {
                    Value = source.InterfaceItem.repeat_count ?? 1, Style = FindResource("NumericUpDownPlus") as Style,
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
                    source.InterfaceItem.repeat_count = Convert.ToInt16(numericUpDown.Value);
                    JsonHelper.WriteToJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface",
                        MaaInterface.Instance);
                };
                numericUpDown.BindLocalization("RepeatOption");
                numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
                settingPanel.Children.Add(numericUpDown);
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
                Growls.Warning("Warning_CannotConnect".GetLocalizedFormattedString((IsADB ? "Simulator" : "Window")));
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
                DataSet.SetData("Tasks", Data?.TaskItemViewModels.ToList());
            }
        }

        public void ConnectToMAA()
        {
            ConfigureMaaProcessorForADB();
            ConfigureMaaProcessorForWin32();
        }

        private void ConfigureMaaProcessorForADB()
        {
            if (IsADB)
            {
                var adbInputType = ConfigureAdbInputTypes();
                var adbScreenCapType = ConfigureAdbScreenCapTypes();

                MaaProcessor.Config.Adb.Input = adbInputType;
                MaaProcessor.Config.Adb.ScreenCap = adbScreenCapType;

                Console.WriteLine(
                    $"{LocExtension.GetLocalizedValue<string>("AdbInputMode")}{adbInputType},{LocExtension.GetLocalizedValue<string>("AdbCaptureMode")}{adbScreenCapType}");
            }
        }

        public string ScreenshotType()
        {
            if (IsADB)
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
            if (!IsADB)
            {
                var win32InputType = ConfigureWin32InputTypes();
                var winScreenCapType = ConfigureWin32ScreenCapTypes();

                MaaProcessor.Config.Win32.Input = win32InputType;
                MaaProcessor.Config.Win32.ScreenCap = winScreenCapType;

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
                    Data.TaskItemViewModels?.RemoveAt(index);
                    DataSet.SetData("Tasks", Data.TaskItemViewModels?.ToList());
                }
            }
        }

        public void ShowResourceName(string name)
        {
            if (Dispatcher.CheckAccess())
            {
                resourceName.Visibility = Visibility.Visible;
                resourceNameText.Visibility = Visibility.Visible;
                resourceName.Text = name;
            }
            else
                Dispatcher.Invoke(() => ShowResourceName(name));
        }

        public void ShowResourceVersion(string version)
        {
            if (Dispatcher.CheckAccess())
            {
                resourceVersion.Visibility = Visibility.Visible;
                resourceVersionText.Visibility = Visibility.Visible;
                resourceVersion.Text = version;
            }
            else
                Dispatcher.Invoke(() => ShowResourceVersion(version));
        }

        public void LoadUI()
        {
            if (Dispatcher.CheckAccess())
            {
                ConnectSettingButton.IsChecked = true;
                var value = DataSet.GetData("EnableEdit", true);
                if (!value)
                    EditButton.Visibility = Visibility.Collapsed;
                DataSet.SetData("EnableEdit", value);
            }
            else
                Dispatcher.Invoke(LoadUI);
        }

        private void ToggleWindowTopMost(object sender, RoutedEventArgs e)
        {
            if (Data == null) return;
            Topmost = !Topmost;
            if (Topmost)
                Data.WindowTopMostButtonForeground = FindResource("PrimaryBrush") as Brush ?? Brushes.DarkGray;
            else
                Data.WindowTopMostButtonForeground = FindResource("ActionIconColor") as Brush ?? Brushes.DarkGray;
        }
    }
}