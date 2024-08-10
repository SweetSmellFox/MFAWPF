using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HandyControl.Controls;
using HandyControl.Data;
using MaaFramework.Binding;
using MFAWPF.Controls;
using MFAWPF.Data;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ComboBox = HandyControl.Controls.ComboBox;

namespace MFAWPF.Views
{
    public partial class MainWindow : CustomWindow
    {
        public static MainWindow Instance { get; private set; }
        private readonly MaaToolkit _maaToolkit;
        public bool IsADB { get; set; } = true;

        public static MainViewModel Data { get; private set; }
        public static readonly string Version = $"v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
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
            MaaProcessor.Instance.TaskStackChanged += OnTaskStackChanged;
        }

        private bool InitializeData()
        {
            DataSet.Data = JSONHelper.ReadFromConfigJsonFile("config", new Dictionary<string, object>());
            MaaInterface.Instance =
                JSONHelper.ReadFromJsonFilePath(MaaProcessor.Resource, "interface", new MaaInterface());
            if (MaaInterface.Instance != null)
            {
                Data.TaskItemViewModels.Clear();
                LoadTasks(MaaInterface.Instance.task);
            }

            ConnectToMAA();
            return LoadTask();
        }

        private bool firstTask = true;

        private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks)
        {
            foreach (var task in tasks)
            {
                var dragItem = new DragItemViewModel(task)
                {
                    IsCheckedWithNull = task.check ?? false
                };
                if (firstTask)
                {
                    // dragItem.EnableSetting = true;
                    ConnectSettingButton.IsChecked = true;
                    if (MaaInterface.Instance.Resources != null &&
                        MaaInterface.Instance.Resources.Count > DataSet.GetData("ResourceIndex", 0))
                        MaaProcessor.CurrentResources =
                            MaaInterface.Instance.Resources[
                                MaaInterface.Instance.Resources.Keys.ToList()[DataSet.GetData("ResourceIndex", 0)]];
                    else MaaProcessor.CurrentResources = new List<string> { MaaProcessor.ResourceBase };
                    firstTask = false;
                }

                Data.TaskItemViewModels.Add(dragItem);
            }
        }

        private void OnTaskStackChanged(object sender, EventArgs e)
        {
            if (MaaProcessor.Instance.TaskStack.Count > 0)
            {
                ToggleTaskButtonsVisibility(isRunning: true);
            }
            else
            {
                ToggleTaskButtonsVisibility(isRunning: false);
            }
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
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
                        Console.WriteLine($"{resourcePath}/pipeline/");
                        var jsonFiles = Directory.GetFiles($"{resourcePath}/pipeline/", "*.json");
                        var taskDictionaryA = new Dictionary<string, TaskModel>();
                        foreach (var file in jsonFiles)
                        {
                            var content = File.ReadAllText(file);
                            var taskData = JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(content);

                            foreach (var task in taskData)
                            {
                                if (!taskDictionaryA.TryAdd(task.Key, task.Value))
                                {
                                    Growls.ErrorGlobal($"Pipeline文件中存在重复的任务名称:\"{task.Key}\"！");
                                    return false;
                                }
                            }
                        }

                        taskDictionary = taskDictionary.MergeTaskModels(taskDictionaryA);
                    }
                }

                PopulateTasks(taskDictionary);

                return true;
            }
            catch (Exception ex)
            {
                Growls.ErrorGlobal($"载入Pipeline文件时出现错误: {ex.Message}");
                Console.WriteLine(ex);
                LoggerService.Logger.LogError(ex);
                return false;
            }
        }

        private void PopulateTasks(Dictionary<string, TaskModel> taskDictionary)
        {
            TaskDictionary = taskDictionary;
            foreach (var task in taskDictionary)
            {
                task.Value.name = task.Key;
                ValidateTaskLinks(taskDictionary, task);
                Data.SourceItems.Add(new TaskItemViewModel { Task = task.Value });
            }
        }

        private void ValidateTaskLinks(Dictionary<string, TaskModel> taskDictionary,
            KeyValuePair<string, TaskModel> task)
        {
            ValidateNextTasks(taskDictionary, task.Value.next);
            ValidateNextTasks(taskDictionary, task.Value.runout_next, "runout_next");
            ValidateNextTasks(taskDictionary, task.Value.timeout_next, "timeout_next");
        }

        private void ValidateNextTasks(Dictionary<string, TaskModel> taskDictionary, object nextTasks,
            string name = "next")
        {
            if (nextTasks is List<string> tasks)
            {
                foreach (var task in tasks)
                {
                    if (!taskDictionary.ContainsKey(task))
                    {
                        Growls.ErrorGlobal($"Pipeline文件中任务的{name}使用了不存在的任务:\"{task}\"！");
                    }
                }
            }
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            if (InitializeData())
            {
                MaaProcessor.Money = 0;
                var tasks = Data.TaskItemViewModels.ToList().FindAll(task => task.IsChecked == true);
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

            if ("adb".Equals(MaaProcessor.Config.Adb.Adb) && DataSet.TryGetData<JObject>("Adb", out var jObject))
            {
                deviceComboBox.ItemsSource = new List<DeviceInfo> { jObject.ToObject<DeviceInfo>() };
                deviceComboBox.SelectedIndex = 0;
                MaaProcessor.Config.IsConnected = true;
            }
            else AutoDetectDevice();

            MaaProcessor.Instance.CurrentInstance = null;
            if (ConnectSettingButton.IsChecked == true)
            {
                ConfigureSettingsPanel();
            }
        }

        private void DeviceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (deviceComboBox.SelectedItem is WindowInfo window)
            {
                Growl.Info($"已选择窗口:{window.Name}");
                MaaProcessor.Config.Win32.HWnd = window.Handle;
            }
            else if (deviceComboBox.SelectedItem is DeviceInfo device)
            {
                Growl.Info($"已选择模拟器:{device.Name}");
                MaaProcessor.Config.Adb.Adb = device.AdbPath;
                MaaProcessor.Config.Adb.AdbAddress = device.AdbSerial;
                MaaProcessor.Config.Adb.AdbConfig = device.AdbConfig;
                DataSet.SetData("Adb", device);
            }
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            AutoDetectDevice();
        }

        public async void AutoDetectDevice()
        {
            try
            {
                Growl.Info(IsADB ? "开始搜索模拟器!" : "开始搜索窗口!");
                MaaProcessor.Config.IsConnected = false;
                if (IsADB)
                {
                    var devices = await _maaToolkit.Device.FindAsync();
                    deviceComboBox.ItemsSource = devices;
                    MaaProcessor.Config.IsConnected = devices.Length > 0;
                    deviceComboBox.SelectedIndex = 0;
                }
                else
                {
                    var windows = _maaToolkit.Win32.Window.ListWindows().ToList();
                    deviceComboBox.ItemsSource = windows;
                    MaaProcessor.Config.IsConnected = windows.Count > 0;
                    deviceComboBox.SelectedIndex = windows.Count > 0
                        ? windows.FindIndex(win => !string.IsNullOrWhiteSpace(win.Name))
                        : 0;
                }

                if (!MaaProcessor.Config.IsConnected)
                {
                    Growl.Info(IsADB ? "未找到模拟器!" : "未找到窗口!");
                }
            }
            catch (Exception ex)
            {
                Growls.WarningGlobal($"{(IsADB ? "识别模拟器" : "识别窗口")}时发生错误: {ex.Message}");
                MaaProcessor.Config.IsConnected = false;
            }
        }

        private void ConfigureSettingsPanel(object sender = null, RoutedEventArgs e = null)
        {
            settingPanel.Children.Clear();
            if (IsADB)
            {
                AddResourcesOption();
                AddSettingOption("捕获方式",
                    new List<string>
                    {
                        "ScreencapFastestLosslessWay", "ScreencapRawWithGzip", "ScreencapFastestWayCompatible",
                        "ScreencapRawByNetcat", "ScreencapEncode", "ScreencapEncodeToFile", "ScreencapMinicapDirect",
                        "ScreencapMinicapStream", "ScreencapEmulatorExtras", "ScreencapFastestWay"
                    },
                    "AdbControlScreenCapType", 0);

                AddSettingOption("触控模式",
                    new List<string> { "MiniTouch (默认)", "MaaTouch (实验功能)", "AdbInput (不推荐)", "AutoDetect (自动检测)" },
                    "AdbControlTouchType", 0);
            }
            else
            {
                AddResourcesOption();
                AddSettingOption("捕获方式",
                    new List<string> { "ScreencapDXGIFramePool", "ScreencapDXGIDesktopDup", "ScreencapGDI" },
                    "Win32ControlScreenCapType", 0);

                AddSettingOption("触控类型",
                    new List<string> { "Seize", "SendMessage" },
                    "Win32ControlTouchType", 0);
            }
        }

        private void AddResourcesOption(int defaultValue = 0)
        {
            var comboBox = new ComboBox()
            {
                SelectedIndex = DataSet.GetData("ResourceIndex", defaultValue), DisplayMemberPath = "name",
                Style = FindResource("ComboBoxExtend") as Style,
                Margin = new Thickness(5)
            };
            if (MaaInterface.Instance.resource != null)
                comboBox.ItemsSource = MaaInterface.Instance.resource;
            var binding = new Binding("Idle")
            {
                Source = Data,
                Mode = BindingMode.OneWay
            };
            comboBox.SetBinding(ComboBox.IsEnabledProperty, binding);

            comboBox.SetValue(InfoElement.TitleProperty, "资源");
            comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

            comboBox.SelectionChanged += (sender, args) =>
            {
                var index = (sender as ComboBox)?.SelectedIndex ?? 0;

                if (MaaInterface.Instance.Resources != null && MaaInterface.Instance.Resources.Count > index)
                    MaaProcessor.CurrentResources =
                        MaaInterface.Instance.Resources[MaaInterface.Instance.Resources.Keys.ToList()[index]];

                DataSet.SetData("ResourceIndex", index);
            };

            settingPanel.Children.Add(comboBox);
        }

        private void AddSettingOption(string title, List<string> options, string datatype, int defaultValue = 0)
        {
            var comboBox = new ComboBox()
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
            comboBox.SetBinding(ComboBox.IsEnabledProperty, binding);

            comboBox.SetValue(InfoElement.TitleProperty, title);
            comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
            comboBox.SelectionChanged += (sender, args) =>
            {
                var index = (sender as ComboBox)?.SelectedIndex ?? 0;
                DataSet.SetData(datatype, index);
                MaaProcessor.Instance.CurrentInstance = null;
            };

            settingPanel.Children.Add(comboBox);
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
            if (MaaInterface.Instance != null && MaaInterface.Instance.option != null)
            {
                if (MaaInterface.Instance.option.TryGetValue(option.name, out var interfaceOption))
                {
                    ComboBox comboBox = new ComboBox
                    {
                        SelectedIndex = option.index ?? 0, Style = FindResource("ComboBoxExtend") as Style,
                        DisplayMemberPath = "name", Margin = new Thickness(5),
                    };

                    var multiBinding = new MultiBinding
                    {
                        Converter = FindResource("CustomIsEnabledConverter") as IMultiValueConverter,
                        Mode = BindingMode.OneWay
                    };

                    multiBinding.Bindings.Add(new Binding("IsCheckedWithNull") { Source = source });
                    multiBinding.Bindings.Add(new Binding("Idle") { Source = Data });

                    comboBox.SetBinding(ComboBox.IsEnabledProperty, multiBinding);

                    comboBox.ItemsSource = interfaceOption.cases;

                    comboBox.Tag = option.name;
                    comboBox.SelectionChanged += (sender, args) =>
                    {
                        option.index = comboBox.SelectedIndex;
                        JSONHelper.WriteToJsonFilePath(MaaProcessor.Resource, "interface", MaaInterface.Instance);
                    };

                    comboBox.SetValue(InfoElement.TitleProperty, option.name);
                    comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
                    settingPanel.Children.Add(comboBox);
                }
            }
        }

        private void AddRepeatOption(DragItemViewModel source)
        {
            if (source.InterfaceItem != null && source.InterfaceItem.repeatable == true)
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
                numericUpDown.ValueChanged += (sender, args) =>
                {
                    source.InterfaceItem.repeat_count = Convert.ToInt16(numericUpDown.Value);
                    JSONHelper.WriteToJsonFilePath(MaaProcessor.Resource, "interface", MaaInterface.Instance);
                };

                numericUpDown.SetValue(InfoElement.TitleProperty, "重复次数");
                numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
                settingPanel.Children.Add(numericUpDown);
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {
            var editDialog = new EditTaskDialog();
            editDialog.Show();
        }

        private void SelectAll(object sender, RoutedEventArgs e)
        {
            foreach (var task in Data.TaskItemViewModels)
                task.IsChecked = true;
        }

        private void SelectNone(object sender, RoutedEventArgs e)
        {
            foreach (var task in Data.TaskItemViewModels)
                task.IsChecked = false;
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
                var adbTouchType = ConfigureAdbControllerTypes();
                var adbScreenCapType = ConfigureAdbScreenCapTypes();

                MaaProcessor.Config.Adb.Touch = adbTouchType;
                MaaProcessor.Config.Adb.ScreenCap = adbScreenCapType;

                Console.WriteLine($"触控模式:{adbTouchType},捕获模式:{adbScreenCapType}");
            }
        }

        private AdbControllerTypes ConfigureAdbControllerTypes()
        {
            return DataSet.GetData("AdbControlTouchType", 0) switch
            {
                0 => AdbControllerTypes.InputPresetMiniTouch,
                1 => AdbControllerTypes.InputPresetMaaTouch,
                2 => AdbControllerTypes.InputPresetAdb,
                3 => AdbControllerTypes.InputPresetAutoDetect,
                _ => 0
            };
        }

        private AdbControllerTypes ConfigureAdbScreenCapTypes()
        {
            return DataSet.GetData("AdbControlScreenCapType", 0) switch
            {
                0 => AdbControllerTypes.ScreencapFastestLosslessWay,
                1 => AdbControllerTypes.ScreencapRawWithGzip,
                2 => AdbControllerTypes.ScreencapFastestWayCompatible,
                3 => AdbControllerTypes.ScreencapRawByNetcat,
                4 => AdbControllerTypes.ScreencapEncode,
                5 => AdbControllerTypes.ScreencapEncodeToFile,
                6 => AdbControllerTypes.ScreencapMinicapDirect,
                7 => AdbControllerTypes.ScreencapMinicapStream,
                8 => AdbControllerTypes.ScreencapEmulatorExtras,
                9 => AdbControllerTypes.ScreencapFastestWay,
                _ => 0
            };
        }

        private void ConfigureMaaProcessorForWin32()
        {
            if (!IsADB)
            {
                var winTouchType = ConfigureWin32ControllerTypes();
                var winScreenCapType = ConfigureWin32ScreenCapTypes();

                MaaProcessor.Config.Win32.Touch = winTouchType;
                MaaProcessor.Config.Win32.ScreenCap = winScreenCapType;

                Console.WriteLine($"触控模式:{winTouchType},捕获模式:{winScreenCapType}");
            }
        }

        private Win32ControllerTypes ConfigureWin32ControllerTypes()
        {
            return DataSet.GetData("Win32ControlTouchType", 0) switch
            {
                0 => Win32ControllerTypes.ScreencapDXGIFramePool,
                1 => Win32ControllerTypes.ScreencapDXGIDesktopDup,
                2 => Win32ControllerTypes.ScreencapGDI,
                _ => 0
            };
        }

        private Win32ControllerTypes ConfigureWin32ScreenCapTypes()
        {
            return DataSet.GetData("Win32ControlTouchType", 0) switch
            {
                0 => Win32ControllerTypes.TouchSeize | Win32ControllerTypes.KeySeize,
                1 => Win32ControllerTypes.TouchSendMessage | Win32ControllerTypes.KeySendMessage,
                _ => 0
            };
        }
    }
}