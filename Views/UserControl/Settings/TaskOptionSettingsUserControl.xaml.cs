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

    private static readonly ConcurrentDictionary<string, StackPanel> CommonPanelCache = new();
    private static readonly ConcurrentDictionary<string, StackPanel> AdvancedPanelCache = new();
    private static readonly ConcurrentDictionary<string, RichTextBox> IntroductionsCache = new();

    public void SetOption(DragItemViewModel dragItem, bool value)
    {
        var cacheKey = $"{dragItem.Name}_{dragItem.InterfaceItem.GetHashCode()}";

        if (!value)
        {
            HideCurrentPanel(cacheKey);
            return;
        }

        var newPanel = CommonPanelCache.GetOrAdd(cacheKey, key =>
        {
            var p = new StackPanel();
            GeneratePanelContent(p, dragItem);
            CommonOptionSettings.Children.Add(p);
            return p;
        });
        
        var newIntroduction = IntroductionsCache.GetOrAdd(cacheKey, key =>
        {
            var richTextBox = new RichTextBox
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.Transparent,
                Margin = new Thickness(20, 10, 20, 20),
                BorderThickness = new Thickness(0),
                IsReadOnly = true
            };
            GenerateIntroductionContent(richTextBox, dragItem);
            Introductions.Children.Add(richTextBox);
            return richTextBox;
        });
        if (newPanel.Children.Count == 0)
            CommonPanelCache.Remove(cacheKey,out _);
        newPanel.Visibility = Visibility.Visible;
        newIntroduction.Visibility = Visibility.Visible;
    }

    private void GenerateIntroductionContent(RichTextBox richTextBox, DragItemViewModel dragItem)
    {
        if (dragItem.InterfaceItem?.Document?.Count > 0)
        {

            var docText = Regex.Unescape(string.Join("\\n", dragItem.InterfaceItem.Document));
            AddIntroduction(richTextBox, docText);
        }
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

    }

    private void HideCurrentPanel(string key)
    {
        if (CommonPanelCache.TryGetValue(key, out var oldPanel))
        {
            oldPanel.Visibility = Visibility.Collapsed;
        }
        if (IntroductionsCache.TryGetValue(key, out var oldIntroduction))
        {
            oldIntroduction.Visibility = Visibility.Collapsed;
        }
    }

    private void HideAllPanels()
    {
        foreach (var panel in CommonPanelCache.Values)
        {
            panel.Visibility = Visibility.Collapsed;
        }
        foreach (var panel in AdvancedPanelCache.Values)
        {
            panel.Visibility = Visibility.Collapsed;
        }
        foreach (var panel in IntroductionsCache.Values)
        {
            panel.Visibility = Visibility.Collapsed;
        }
    }
    
    private void AddIntroduction(RichTextBox richTextBox, string input = "")
    {
        input = LanguageHelper.GetLocalizedString(input);
        var flowDocument = new FlowDocument();
        foreach (var line in input.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 5)
            };
            var processedLine = line.Trim();
            var alignMatch = Regex.Match(processedLine, @"\[align:(?<value>left|center|right)\](?<content>.*?)\[/align\]");
            if (alignMatch.Success)
            {
                paragraph.TextAlignment = (TextAlignment)Enum.Parse(typeof(TextAlignment), alignMatch.Groups["value"].Value, true);
                processedLine = alignMatch.Groups["content"].Value ;
            }
            ProcessLine(processedLine, paragraph.Inlines);
            flowDocument.Blocks.Add(paragraph);
        }
        richTextBox.Document = flowDocument;
    }

    private static readonly Regex TagRegex = new(@"\[(?<tag>[^\]]+):?(?<value>[^\]]*)\](?<content>.*?)\[/\k<tag>\]", RegexOptions.Compiled);

    private void ProcessLine(string text, InlineCollection inlines)
    {
        var lastPos = 0;
        foreach (Match match in TagRegex.Matches(text))
        {
            if (match.Index > lastPos)
                inlines.Add(new Run(text.Substring(lastPos, match.Index - lastPos)));
            var span = new Span();
            ApplyStyle(span, match.Groups["tag"].Value.ToLower(), match.Groups["value"].Value);
            ProcessLine(match.Groups["content"].Value, span.Inlines);
            inlines.Add(span);
            lastPos = match.Index + match.Length;
        }
        if (lastPos < text.Length)
            inlines.Add(new Run(text.Substring(lastPos)));
    }

    private static readonly Dictionary<string, Action<Span, string>> StyleActions = new()
    {
        {
            "color", (s, v) => s.Foreground = BrushConverterHelper.ConvertToBrush(v)
        },
        {
            "size", (s, v) => s.FontSize = double.TryParse(v, out var size) ? size - 2 : 12
        },
        {
            "b", (s, _) => s.FontWeight = FontWeights.Bold
        },
        {
            "i", (s, _) => s.FontStyle = FontStyles.Italic
        },
        {
            "u", (s, _) => s.TextDecorations = TextDecorations.Underline
        },
        {
            "s", (s, _) => s.TextDecorations = TextDecorations.Strikethrough
        }
    };

    private void ApplyStyle(Span span, string tag, string value)
    {
        if (StyleActions.TryGetValue(tag, out var action)) action(span, value);
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
            Height = 20,Width = 40,
            HorizontalAlignment = HorizontalAlignment.Right,
            Tag = option.Name,
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
