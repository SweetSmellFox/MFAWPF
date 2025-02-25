using HandyControl.Controls;
using HandyControl.Data;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.ViewModels.Tool;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using ComboBox = System.Windows.Controls.ComboBox;

namespace MFAWPF.Views.UserControl.Settings;

public partial class TaskOptionSettingsUserControl
{

    public TaskOptionSettingsUserControl()
    {
        InitializeComponent();
    }
    
    private static readonly ConcurrentDictionary<string, StackPanel> PanelCache = new();
    
     public void SetOption(DragItemViewModel dragItem, bool value)
    {
        if (dragItem.InterfaceItem == null)
        {
            HideAllPanels();
            return;
        }

        var cacheKey = $"{dragItem.Name}_{dragItem.InterfaceItem.GetHashCode()}";
        
        if (!value)
        {
            HideCurrentPanel(cacheKey);
            return;
        }

        var newPanel = PanelCache.GetOrAdd(cacheKey, key => 
        {
            var p = new StackPanel();
            GeneratePanelContent(p, dragItem);
            OptionSettings.Children.Add(p); 
            return p;
        });
        
        newPanel.Visibility = Visibility.Visible;
    }

    private void GeneratePanelContent(StackPanel panel, DragItemViewModel dragItem)
    {
        AddRepeatOption(panel, dragItem);
        
        if (dragItem.InterfaceItem?.Option != null)
        {
            foreach (var option in dragItem.InterfaceItem.Option)
            {
                AddOption(panel, option, dragItem);
            }
        }

        if (dragItem.InterfaceItem?.Document?.Count > 0)
        {
            var docText = Regex.Unescape(string.Join("\\n", dragItem.InterfaceItem.Document));
            AddIntroduction(panel, docText);
        }
    }

    private void HideCurrentPanel(string key)
    {
        if (PanelCache.TryGetValue(key, out var oldPanel))
        {
            oldPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void HideAllPanels()
    {
        foreach (var panel in PanelCache.Values)
        {
            panel.Visibility = Visibility.Collapsed;
        }
    }

    private void AddIntroduction(Panel panel, string input = "")
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

    private void AddRepeatOption(Panel panel, DragItemViewModel source)
    {
        if (source.InterfaceItem is not { Repeatable: true }) return;

        var numericUpDown = new NumericUpDown
        {
            Value = source.InterfaceItem.RepeatCount ?? 1,
            Style = FindResource("NumericUpDownPlus") as Style,
            Margin = new Thickness(5),
            Increment = 1,
            Minimum = -1,
            DecimalPlaces = 0
        };

        numericUpDown.ValueChanged += (_, _) =>
        {
            source.InterfaceItem.RepeatCount = Convert.ToInt32(numericUpDown.Value);
            SaveConfiguration();
        };

        panel.Children.Add(numericUpDown);
    }

    private void AddOption(Panel panel, MaaInterface.MaaInterfaceSelectOption option, DragItemViewModel source)
    {
        if (MaaInterface.Instance?.Option?.TryGetValue(option.Name ?? string.Empty, out var interfaceOption) != true) return;

        var converter = FindResource("CustomIsEnabledConverter") as IMultiValueConverter;

        FrameworkElement control = interfaceOption.Cases.ShouldSwitchButton(out var yes, out var no)
            ? CreateToggleControl(option, yes, no, source, converter)
            : CreateComboControl(option, interfaceOption, source, converter);

        panel.Children.Add(control);
    }

    private Grid CreateToggleControl(
        MaaInterface.MaaInterfaceSelectOption option,
        int yesValue,
        int noValue,
        DragItemViewModel source,
        IMultiValueConverter? customConverter)
    {
        var button = new ToggleButton
        {
            IsChecked = option.Index == yesValue,
            Style = FindResource("ToggleButtonSwitch") as Style,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Right,
            Tag = option.Name,
            MinWidth = 60,
            Margin = new Thickness(0, 0, -12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var multiBinding = new MultiBinding
        {
            Converter = customConverter,
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
        button.SetBinding(IsEnabledProperty, multiBinding);


        button.Checked += (_, _) =>
        {
            option.Index = yesValue;
            SaveConfiguration();
        };

        button.Unchecked += (_, _) =>
        {
            option.Index = noValue;
            SaveConfiguration();
        };

        button.SetValue(ToolTipProperty, LanguageHelper.GetLocalizedString(option.Name));
        var textBlock = new TextBlock
        {
            Text = LanguageHelper.GetLocalizedString(option.Name),
            Margin = new Thickness(0, 0, 5, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };

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
            Margin = new Thickness(12, 5, 0, 5)
        };

        Grid.SetColumn(textBlock, 0);
        Grid.SetColumn(button, 2);
        grid.Children.Add(textBlock);
        grid.Children.Add(button);

        return grid;
    }

    private ComboBox CreateComboControl(
        MaaInterface.MaaInterfaceSelectOption option,
        MaaInterface.MaaInterfaceOption interfaceOption,
        DragItemViewModel source,
        IMultiValueConverter? customConverter)
    {
        var combo = new ComboBox
        {
            SelectedIndex = option.Index ?? 0,
            Style = FindResource("ComboBoxExtend") as Style,
            DisplayMemberPath = "Name",
            Margin = new Thickness(5),
            ItemsSource = interfaceOption.Cases?.Select(c => new
            {
                Name = LanguageHelper.GetLocalizedString(c.Name)
            })
        };
        
        var multiBinding = new MultiBinding
        {
            Converter = customConverter,
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
        combo.SetBinding(IsEnabledProperty, multiBinding);
        
        combo.SelectionChanged += (_, _) =>
        {
            option.Index = combo.SelectedIndex;
            SaveConfiguration();
        };
        
        combo.SetValue(TitleElement.TitleProperty, LanguageHelper.GetLocalizedString(option.Name));
        combo.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        return combo;
    }


    private void SaveConfiguration()
    {
        MFAConfiguration.SetConfiguration("TaskItems",
            Instances.TaskQueueViewModel.TaskItemViewModels.Select(m => m.InterfaceItem));
    }
}
