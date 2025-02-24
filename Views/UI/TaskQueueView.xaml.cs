using HandyControl.Controls;
using HandyControl.Data;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels;
using MFAWPF.ViewModels.UI;
using MFAWPF.Views.UserControl.Settings;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WPFLocalizeExtension.Extensions;
using static MFAWPF.Views.UI.RootView;
using ComboBox = System.Windows.Controls.ComboBox;
using DragItemViewModel = MFAWPF.ViewModels.Tool.DragItemViewModel;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using ViewModel = MFAWPF.ViewModels.ViewModel;

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
            Instances.RootViewModel.ShowResourceVersion(version);
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
            if (Instances.TaskQueueViewModel.CurrentResources.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(Instances.TaskQueueViewModel.CurrentResource) && !string.IsNullOrWhiteSpace(Instances.TaskQueueViewModel.CurrentResources[0].Name))
                    Instances.TaskQueueViewModel.CurrentResource = Instances.TaskQueueViewModel.CurrentResources[0].Name;
            }
            if (Instances.TaskQueueViewModel.CurrentResources.Any(r => r.Name == Instances.TaskQueueViewModel.CurrentResource))
            {
                var resources = Instances.TaskQueueViewModel.CurrentResources.Where(r => r.Name == Instances.TaskQueueViewModel.CurrentResource);
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
        if (Instances.RootViewModel.IsAdb)
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
        if (Instances.RootViewModel.IsAdb)
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
        if (!Instances.RootViewModel.IsAdb)
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

    public bool FirstTask = true;

    private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks, Collection<DragItemViewModel>? drag = null)
    {
        foreach (var task in tasks)
        {
            var dragItem = new DragItemViewModel(task);

            if (FirstTask)
            {
                if (MaaInterface.Instance?.Resources != null && MaaInterface.Instance.Resources.Count > 0)
                    Instances.TaskQueueViewModel.CurrentResources = new ObservableCollection<MaaInterface.MaaCustomResource>(MaaInterface.Instance.Resources.Values.ToList());
                else
                    Instances.TaskQueueViewModel.CurrentResources =
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
        if (!Instances.RootView.IsConnected())
        {
            GrowlHelper.Warning(
                "Warning_CannotConnect".ToLocalizationFormatted(Instances.RootViewModel.IsAdb
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
                // 获取选中项的索引
                int index = ViewModel.TaskItemViewModels.IndexOf(taskItemViewModel);
                ViewModel.TaskItemViewModels.RemoveAt(index);
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

    private void AddIntroduction(Panel panel = null, string input = "")
    {
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

    private void SetResourcesOption(HandyControl.Controls.ComboBox comboBox)
    {
        // if (MaaInterface.Instance?.Resource != null)
        // {
        //     var a = new List<MaaInterface.MaaCustomResource>();
        //     foreach (var resource in MaaInterface.Instance.Resource)
        //     {
        //         var o = new MaaInterface.MaaCustomResource
        //         {
        //             Name = LanguageHelper.GetLocalizedString(resource?.Name),
        //             Path = resource.Path
        //         };
        //         a.Add(o);
        //
        //     }
        //     comboBox.ItemsSource = a;
        // }
        // comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("ResourceIndex", 0);
        // comboBox.SelectionChanged += (sender, _) =>
        // {
        //     var index = (sender as ComboBox)?.SelectedIndex ?? 0;
        //
        //     if (MaaInterface.Instance?.Resources != null && MaaInterface.Instance.Resources.Count > index)
        //         Instances.RootInstances.TaskQueueViewModel.CurrentResources  =
        //             MaaInterface.Instance.Resources[MaaInterface.Instance.Resources.Keys.ToList()[index]];
        //     MFAConfiguration.SetConfiguration("ResourceIndex", index);
        // };

    }


    private void AddAutoStartOption(StackPanel stackPanel)
    {
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            SelectedValuePath = "ResourceKey",
            DisplayMemberPath = "Name"
        };

        var source = new Binding("BeforeTaskList")
        {
            Source = Instances.RootViewModel
        };
        comboBox.SetBinding(ItemsControl.ItemsSourceProperty, source);
        comboBox.BindLocalization("AutoStartOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        var value = new Binding("BeforeTask")
        {
            Source = Instances.RootViewModel,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay
        };
        comboBox.SetBinding(Selector.SelectedValueProperty, value);
        stackPanel.Children.Add(comboBox);
    }

    private void AddAfterTaskOption(StackPanel stackPanel, int defaultValue = 0)
    {
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
            SelectedValuePath = "ResourceKey",
            DisplayMemberPath = "Name"
        };
        var source = new Binding("AfterTaskList")
        {
            Source = Instances.RootViewModel
        };
        comboBox.SetBinding(ItemsControl.ItemsSourceProperty, source);
        comboBox.BindLocalization("AfterTaskOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        var value = new Binding("AfterTask")
        {
            Source = Instances.RootViewModel,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay
        };
        comboBox.SetBinding(Selector.SelectedValueProperty, value);
        stackPanel.Children.Add(comboBox);
    }

    public void SetOption(DragItemViewModel dragItem, bool value)
    {
        if (dragItem.InterfaceItem != null && value)
        {
            SettingPanel.Children.Clear();
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

            var sc1 = new ScrollViewer()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = s1
            };
            var heightBinding = new Binding("ActualHeight")
            {
                Source = SettingPanel,
                Converter = new SubtractConverter(),
                ConverterParameter = "20"
            };
            sc1.SetBinding(HeightProperty, heightBinding);
            SettingPanel.Children.Add(sc1);
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
                    Source = Instances.RootViewModel
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
                Source = Instances.RootViewModel
            });

            numericUpDown.SetBinding(ComboBox.IsEnabledProperty, multiBinding);

            numericUpDown.Tag = source.Name;
            numericUpDown.ValueChanged += (_, _) =>
            {
                source.InterfaceItem.RepeatCount = Convert.ToInt16(numericUpDown.Value);
                MFAConfiguration.SetConfiguration("TaskItems",
                    ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            };
            numericUpDown.BindLocalization("RepeatOption");
            numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
            panel.Children.Add(numericUpDown);
        }
    }
}
