using HandyControl.Controls;
using HandyControl.Data;
using MFAWPF.Data;
using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static MFAWPF.Views.UI.RootView;
using ComboBox = System.Windows.Controls.ComboBox;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using ViewModel = MFAWPF.ViewModels.ViewModel;

namespace MFAWPF.Views.UI;

public partial class TaskQueueView
{
    public TaskQueueView()
    {
        InitializeComponent();
    }

    public void Start(object sender, RoutedEventArgs e) => Instance.Start();

    public void Stop(object sender, RoutedEventArgs e) => Instance.Stop();


    private void AddResourcesOption(int defaultValue = 0)
    {
        var comboBox = new ComboBox
        {
            SelectedIndex = MFAConfiguration.GetConfiguration("ResourceIndex", defaultValue),
            Style = FindResource("ComboBoxExtend") as Style,
            DisplayMemberPath = "Name",
            Margin = new Thickness(5)
        };

        var binding = new Binding("Idle")
        {
            Source = RootView.ViewModel,
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

        SettingPanel.Children.Add(comboBox);
    }
    private void Edit(object sender, RoutedEventArgs e)
    {
        if (!Instance.IsConnected())
        {
            GrowlHelper.Warning(
                "Warning_CannotConnect".ToLocalizationFormatted(RootView.ViewModel.IsAdb
                    ? "Emulator".ToLocalization()
                    : "Window".ToLocalization()));
            return;
        }

        TaskDialog?.Show();

        RootView.ViewModel.Idle = false;
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        foreach (var task in RootView.ViewModel.TaskItemViewModels)
            task.IsChecked = true;
    }

    private void SelectNone(object sender, RoutedEventArgs e)
    {
        foreach (var task in RootView.ViewModel.TaskItemViewModels)
            task.IsChecked = false;
    }

    private void Add(object sender, RoutedEventArgs e)
    {
        RootView.ViewModel.Idle = false;
        var addTaskDialog = new MFAWPF.Views.UI.Dialog.AddTaskDialog(RootView.ViewModel.TasksSource);
        addTaskDialog.ShowDialog();
        if (addTaskDialog.OutputContent != null)
        {
            RootView.ViewModel.TaskItemViewModels.Add(addTaskDialog.OutputContent.Clone());
            MFAConfiguration.SetConfiguration("TaskItems", RootView.ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
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
                int index = RootView.ViewModel.TaskItemViewModels.IndexOf(taskItemViewModel);
                RootView.ViewModel.TaskItemViewModels.RemoveAt(index);
                MFAConfiguration.SetConfiguration("TaskItems", RootView.ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
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

    public void ConfigureTaskSettingsPanel()
    {
        SettingPanel.Children.Clear();
        StackPanel s2 = new()
        {
            Margin = new Thickness(2)
        };
        AddResourcesOption();
        AddAutoStartOption();
        AddAfterTaskOption();

        ScrollViewer sv2 = new()
        {
            Content = s2,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        SettingPanel.Children.Add(sv2);
    }

    private void ConfigureTaskSettingsPanel(object sender, RoutedEventArgs e) => ConfigureTaskSettingsPanel();

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


    private void AddAutoStartOption(int defaultValue = 0)
    {
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5)
        };

        comboBox.ItemsSource = RootView.ViewModel.BeforeTaskList;
        comboBox.BindLocalization("AutoStartOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("AutoStartIndex", defaultValue);

        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            MFAConfiguration.SetConfiguration("AutoStartIndex", index);
            RootView.ViewModel.BeforeTask = RootView.ViewModel.BeforeTaskList[index].Name;
        };


        SettingPanel.Children.Add(comboBox);
    }

    private void AddAfterTaskOption(int defaultValue = 0)
    {
        var comboBox = new ComboBox
        {
            Style = FindResource("ComboBoxExtend") as Style,
            Margin = new Thickness(5),
        };
        comboBox.ItemsSource = RootView.ViewModel.AfterTaskList;

        comboBox.BindLocalization("AfterTaskOption");
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
        comboBox.SelectedIndex = MFAConfiguration.GetConfiguration("AfterTaskIndex", defaultValue);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            MFAConfiguration.SetConfiguration("AfterTaskIndex", index);
            RootView.ViewModel.AfterTask = RootView.ViewModel.AfterTaskList[index].Name;
        };

        SettingPanel.Children.Add(comboBox);
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
                    Source = RootView.ViewModel
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
                            RootView.ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
                    };
                    toggleButton.Unchecked += (_, _) =>
                    {
                        option.Index = no;
                        MFAConfiguration.SetConfiguration("TaskItems",
                            RootView.ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
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
                            RootView.ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
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
                Source = RootView.ViewModel
            });

            numericUpDown.SetBinding(ComboBox.IsEnabledProperty, multiBinding);

            numericUpDown.Tag = source.Name;
            numericUpDown.ValueChanged += (_, _) =>
            {
                source.InterfaceItem.RepeatCount = Convert.ToInt16(numericUpDown.Value);
                MFAConfiguration.SetConfiguration("TaskItems",
                    RootView.ViewModel.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            };
            numericUpDown.BindLocalization("RepeatOption");
            numericUpDown.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);
            panel.Children.Add(numericUpDown);
        }
    }
}
