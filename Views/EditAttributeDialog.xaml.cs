using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MFAWPF.Controls;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using HandyControl.Controls;
using HandyControl.Data;
using Newtonsoft.Json;
using Attribute = MFAWPF.Utils.Attribute;
using ComboBox = HandyControl.Controls.ComboBox;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace MFAWPF.Views;

public partial class EditAttributeDialog : CustomWindow
{
    public Attribute Attribute { get; private set; }
    private bool IsEdit = true;
    private UIElement? Control;
    private CustomWindow? ParentDialog;

    public EditAttributeDialog(CustomWindow? parentDialog, Attribute? attribute = null, bool isEdit = false) :
        base()
    {
        InitializeComponent();
        ParentDialog = parentDialog;
        IsEdit = isEdit;
        Attribute = new Attribute()
        {
            Key = attribute?.Key, Value = attribute?.Value
        };
        save_Button.Content = IsEdit ? "保存" : "添加";
        var Types = new List<string>()
        {
            "recognition",
            "action",
            "next",
            "is_sub",
            "inverse",
            "enabled",
            "timeout",
            "timeout_next",
            "times_limit",
            "runout_next",
            "pre_delay",
            "post_delay",
            "pre_wait_freezes",
            "post_wait_freezes",
            "focus",
            "focus_tip",
            "focus_tip_color",
            "expected",
            "only_rec",
            "labels",
            "model",
            "target",
            "target_offset",
            "begin",
            "begin_offset",
            "end",
            "end_offset",
            "duration",
            "key",
            "input_text",
            "package",
            "custom_recognition",
            "custom_recognition_param",
            "custom_action",
            "custom_action_param",
            "order_by",
            "index",
            "method",
            "count",
            "green_mask",
            "detector",
            "ratio",
            "template",
            "roi",
            "threshold",
            "lower",
            "upper",
            "connected"
        };


        typeComboBox.ItemsSource = Types;
        if (attribute?.Key != null)
        {
            typeComboBox.SelectedValue = attribute.Key;
            SwitchByType(attribute.Key, attribute.Value);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Attribute.Key = typeComboBox.SelectedValue.ToString();
        if (Attribute.Key != null)
            ReadValue(Attribute.Key);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SwitchString(object? defaultValue)
    {
        AttributePanel.Children.Clear();

        Grid dynamicGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 10, 4)
        };

        TextBlock textBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            Width = 55,
            Text = "属性值：",
            Foreground = (Brush)Application.Current.Resources["GrayColor1"],
            HorizontalAlignment = HorizontalAlignment.Left
        };

        dynamicGrid.Children.Add(textBlock);

        AttributePanel.Children.Add(dynamicGrid);

        TextBox textBox = new TextBox()
        {
            Style = FindResource("TextBoxExtend") as Style, Margin = new Thickness(0, 5, 0, 15),
            Text = defaultValue != null ? defaultValue.ToString() : ""
        };
        Control = textBox;
        AttributePanel.Children.Add(Control);
    }

    private void SwitchBool(object? defaultValue)
    {
        AttributePanel.Children.Clear();
        // 创建一个新的 Grid
        Grid dynamicGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 10, 4)
        };

        TextBlock textBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            Width = 55,
            Text = "属性值：",
            Foreground = (Brush)Application.Current.Resources["GrayColor1"],
            HorizontalAlignment = HorizontalAlignment.Left
        };

        dynamicGrid.Children.Add(textBlock);

        AttributePanel.Children.Add(dynamicGrid);
        var Types = new List<bool>()
        {
            true,
            false
        };
        ComboBox comboBox = new ComboBox()
        {
            Margin = new Thickness(0, 5, 0, 15), ItemsSource = Types
        };
        if (defaultValue == null)
            comboBox.SelectedIndex = 0;
        else comboBox.SelectedValue = defaultValue;
        Control = comboBox;
        AttributePanel.Children.Add(Control);
    }

    private void SwitchCombo(string key, object? defaultValue)
    {
        AttributePanel.Children.Clear();

        Grid dynamicGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 10, 4)
        };

        TextBlock textBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            Width = 55,
            Text = "属性值：",
            Foreground = (Brush)Application.Current.Resources["GrayColor1"],
            HorizontalAlignment = HorizontalAlignment.Left
        };

        dynamicGrid.Children.Add(textBlock);

        AttributePanel.Children.Add(dynamicGrid);
        var Types = new List<string>();
        switch (key)
        {
            case "recognition":
                Types = new List<string>()
                {
                    "DirectHit",
                    "TemplateMatch",
                    "FeatureMatch",
                    "ColorMatch",
                    "OCR",
                    "NeuralNetworkClassify",
                    "NeuralNetworkDetect",
                    "Custom"
                };
                break;
            case "action":
                Types = new List<string>()
                {
                    "DoNothing",
                    "Click",
                    "Swipe",
                    "Key",
                    "Text",
                    "StartApp",
                    "StopApp",
                    "StopTask",
                    "Custom"
                };
                break;
            case "order_by":
                Types = new List<string>()
                {
                    "Horizontal",
                    "Vertical",
                    "Score",
                    "Random",
                    "Area",
                    "Length"
                };
                break;
            case "detector":
                Types = new List<string>()
                {
                    "SIFT",
                    "KAZE",
                    "AKAZE",
                    "BRISK",
                    "ORB"
                };
                break;
        }

        ComboBox comboBox = new ComboBox()
        {
            Margin = new Thickness(0, 5, 0, 15), ItemsSource = Types
        };
        if (defaultValue == null)
            comboBox.SelectedIndex = 0;
        else comboBox.SelectedValue = defaultValue;
        Control = comboBox;
        AttributePanel.Children.Add(comboBox);
    }

    private void SwitchAutoList(object? defaultValue)
    {
        AttributePanel.Children.Clear();

        Grid dynamicGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 10, 4)
        };

        TextBlock textBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            Width = 55,
            Text = "属性值：",
            Foreground = (Brush)Application.Current.Resources["GrayColor1"],
            HorizontalAlignment = HorizontalAlignment.Left
        };

        Button button = new Button
        {
            Style = (Style)Application.Current.Resources["textBoxButton"],
            ToolTip = "添加属性",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 15,
            Height = 15
        };

        Path path = new Path
        {
            Data = (Geometry)Application.Current.Resources["AddGeometry"],
            Fill = (Brush)Application.Current.Resources["GrayColor4"],
            MaxWidth = 12,
            Stretch = Stretch.Uniform
        };

        button.Resources = new ResourceDictionary();
        Style buttonStyle = new Style(typeof(Button));
        buttonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Arrow));
        Trigger trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
        buttonStyle.Triggers.Add(trigger);
        button.Resources.Add(typeof(Button), buttonStyle);

        button.Content = path;
        button.Click += AddAutoAttribute;
        dynamicGrid.Children.Add(textBlock);
        dynamicGrid.Children.Add(button);

        AttributePanel.Children.Add(dynamicGrid);

        Border border = new Border
        {
            Height = 120,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(5, 5, 5, 0)
        };

        ScrollViewer scrollViewer = new ScrollViewer();

        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        Control = stackPanel;
        scrollViewer.Content = Control;
        border.Child = scrollViewer;

        AttributePanel.Children.Add(border);

        if (defaultValue is bool b)
        {
            if (b)
                AddAutoAttribute(stackPanel, true);
        }
        else if (defaultValue is string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                AddAutoAttribute(stackPanel, s);
        }
        else if (defaultValue is List<string> ls)
        {
            foreach (var VARIABLE in ls)
            {
                AddAutoAttribute(stackPanel, VARIABLE);
            }
        }
        else if (defaultValue is List<int> li)
        {
            foreach (var VARIABLE in li)
            {
                AddAutoAttribute(stackPanel, VARIABLE);
            }
        }
    }

    private void SwitchList(object? defaultValue)
    {
        AttributePanel.Children.Clear();

        Grid dynamicGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 10, 4)
        };

        TextBlock textBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            Width = 55,
            Text = "属性值：",
            Foreground = (Brush)Application.Current.Resources["GrayColor1"],
            HorizontalAlignment = HorizontalAlignment.Left
        };

        Button button = new Button
        {
            Style = (Style)Application.Current.Resources["textBoxButton"],
            ToolTip = "添加属性",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 15,
            Height = 15
        };

        Path path = new Path
        {
            Data = (Geometry)Application.Current.Resources["AddGeometry"],
            Fill = (Brush)Application.Current.Resources["GrayColor4"],
            MaxWidth = 12,
            Stretch = Stretch.Uniform
        };

        button.Resources = new ResourceDictionary();
        Style buttonStyle = new Style(typeof(Button));
        buttonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Arrow));
        Trigger trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
        buttonStyle.Triggers.Add(trigger);
        button.Resources.Add(typeof(Button), buttonStyle);

        button.Content = path;
        button.Click += AddAttribute;
        dynamicGrid.Children.Add(textBlock);
        dynamicGrid.Children.Add(button);

        AttributePanel.Children.Add(dynamicGrid);

        Border border = new Border
        {
            Height = 120,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(5, 5, 5, 0)
        };

        ScrollViewer scrollViewer = new ScrollViewer();
        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        Control = stackPanel;
        scrollViewer.Content = Control;
        border.Child = scrollViewer;

        AttributePanel.Children.Add(border);

        // // Set up drag-and-drop handlers
        // stackPanel.PreviewMouseLeftButtonDown += StackPanel_PreviewMouseLeftButtonDown;
        // stackPanel.PreviewMouseMove += StackPanel_PreviewMouseMove;
        // stackPanel.AllowDrop = true;
        // stackPanel.Drop += StackPanel_Drop;

        if (defaultValue is bool b)
        {
            if (b)
                AddAttribute(stackPanel, true);
        }
        else if (defaultValue is string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                AddAttribute(stackPanel, s);
        }
        else if (defaultValue is List<string> ls)
        {
            foreach (var VARIABLE in ls)
            {
                AddAttribute(stackPanel, VARIABLE);
            }
        }
        else if (defaultValue is List<int> li)
        {
            foreach (var VARIABLE in li)
            {
                AddAttribute(stackPanel, VARIABLE);
            }
        }
    }

    private void SwitchListList(object? defaultValue)
    {
        AttributePanel.Children.Clear();

        Grid dynamicGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 10, 4)
        };

        TextBlock textBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            Width = 55,
            Text = "属性值：",
            Foreground = (Brush)Application.Current.Resources["GrayColor1"],
            HorizontalAlignment = HorizontalAlignment.Left
        };

        Button button = new Button
        {
            Style = (Style)Application.Current.Resources["textBoxButton"],
            ToolTip = "添加属性",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 15,
            Height = 15
        };

        Path path = new Path
        {
            Data = (Geometry)Application.Current.Resources["AddGeometry"],
            Fill = (Brush)Application.Current.Resources["GrayColor4"],
            MaxWidth = 12,
            Stretch = Stretch.Uniform
        };

        button.Resources = new ResourceDictionary();
        Style buttonStyle = new Style(typeof(Button));
        buttonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Arrow));
        Trigger trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
        buttonStyle.Triggers.Add(trigger);
        button.Resources.Add(typeof(Button), buttonStyle);

        button.Content = path;
        button.Click += AddAttribute;
        dynamicGrid.Children.Add(textBlock);
        dynamicGrid.Children.Add(button);

        AttributePanel.Children.Add(dynamicGrid);

        Border border = new Border
        {
            Height = 120,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(5, 5, 5, 0)
        };

        ScrollViewer scrollViewer = new ScrollViewer();

        StackPanel stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        Control = stackPanel;
        scrollViewer.Content = Control;
        border.Child = scrollViewer;

        AttributePanel.Children.Add(border);
        if (defaultValue is List<List<int>> ls)
        {
            foreach (var VARIABLE in ls)
            {
                AddAttribute(stackPanel, string.Join(",", VARIABLE));
            }
        }
        else if (defaultValue is List<int> li)
        {
            foreach (var VARIABLE in li)
            {
                AddAttribute(stackPanel, VARIABLE);
            }
        }
    }

    private void SwitchByType(string selectedType, object? defaultValue)
    {
        if (selectedType != null)
        {
            switch (selectedType)
            {
                //List<string>
                case "next":
                case "timeout_next":
                case "runout_next":
                    SwitchAutoList(defaultValue);
                    break;
                case "expected":
                case "template":
                case "labels":
                    SwitchList(defaultValue);
                    break;
                //List<int>
                case "target_offset":
                case "begin_offset":
                case "end_offset":
                case "key":
                case "upper":
                case "lower":
                    SwitchList(defaultValue);
                    break;
                //List<List<int>>
                case "roi":
                    SwitchListList(defaultValue);
                    break;
                //bool
                case "is_sub":
                case "inverse":
                case "enabled":
                case "focus":
                case "only_rec":
                case "green_mask":
                case "connected":
                    SwitchBool(defaultValue);
                    break;
                //uint
                case "timeout":
                case "times_limit":
                case "pre_delay":
                case "post_delay":
                case "duration":
                    SwitchString(defaultValue);
                    break;
                //int
                case "index":
                case "method":
                case "count":
                    SwitchString(defaultValue);
                    break;
                //double
                case "ratio":
                case "threshold":
                    SwitchString(defaultValue);
                    break;
                //coombo
                case "recognition":
                case "action":
                case "order_by":
                case "detector":
                    SwitchCombo(selectedType, defaultValue);
                    break;
                //object
                case "target":
                case "begin":
                case "end":
                    SwitchAutoList(defaultValue);
                    break;
                //string
                default:
                    SwitchString(defaultValue);
                    break;
            }
        }
    }

    private void ReadValue(string selectedType)
    {
        if (selectedType != null)
        {
            switch (selectedType)
            {
                //List<string>
                case "next":
                case "timeout_next":
                case "runout_next":
                    if (Control is StackPanel p0)
                    {
                        List<string> list = new List<string>();
                        foreach (var VARIABLE in p0.Children)
                        {
                            if (VARIABLE is SAutoCompleteTextBox textBox)
                            {
                                if (!string.IsNullOrEmpty(textBox.Text))
                                    list.Add(textBox.Text);
                            }
                        }

                        Attribute.Value = list;
                    }

                    break;
                case "expected":
                case "template":
                case "labels":
                    if (Control is StackPanel p1)
                    {
                        List<string> list = new List<string>();
                        foreach (var VARIABLE in p1.Children)
                        {
                            if (VARIABLE is TextBox textBox)
                            {
                                if (!string.IsNullOrEmpty(textBox.Text))
                                    list.Add(textBox.Text);
                            }
                        }

                        Attribute.Value = list;
                    }

                    break;
                //List<int>
                case "target_offset":
                case "begin_offset":
                case "end_offset":
                case "key":
                    if (Control is StackPanel p2)
                    {
                        List<int> list = new List<int>();
                        foreach (var VARIABLE in p2.Children)
                        {
                            if (VARIABLE is TextBox textBox)
                            {
                                if (int.TryParse(textBox.Text, out var i))
                                    list.Add(i);
                            }
                        }

                        Attribute.Value = list;
                    }

                    break;
                //List<List<int>>
                case "upper":
                case "lower":
                case "roi":
                    if (Control is StackPanel p3)
                    {
                        if (p3.Children.Count > 0)
                        {
                            if (p3.Children[0] is TextBox tb && tb.Text.Contains(","))
                            {
                                List<List<int>> list = new List<List<int>>();
                                foreach (var VARIABLE in p3.Children)
                                {
                                    if (VARIABLE is TextBox textBox)
                                    {
                                        try
                                        {
                                            list.Add(textBox.Text.Split(",")
                                                .Select(s => int.Parse(s)) // 将每个字符串转换为整数
                                                .ToList());
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e);
                                            Growls.WarningGlobal("读取数字数组时出现错误！");
                                            return;
                                        }
                                    }
                                }

                                Attribute.Value = list;
                            }
                            else
                            {
                                List<int> list = new List<int>();
                                foreach (var VARIABLE in p3.Children)
                                {
                                    if (VARIABLE is TextBox textBox)
                                    {
                                        if (int.TryParse(textBox.Text, out var i))
                                            list.Add(i);
                                    }
                                }

                                Attribute.Value = list;
                            }
                        }
                    }

                    break;
                //bool
                case "is_sub":
                case "inverse":
                case "enabled":
                case "focus":
                case "only_rec":
                case "green_mask":
                case "connected":
                    if (Control is ComboBox cb1)
                        Attribute.Value = cb1.SelectedValue;
                    break;
                //uint
                case "timeout":
                case "times_limit":
                case "pre_delay":
                case "post_delay":
                case "duration":
                    if (Control is TextBox t1)
                    {
                        if (uint.TryParse(t1.Text, out var ui))
                        {
                            Attribute.Value = ui;
                        }
                        else
                        {
                            Attribute.Value = 0;
                        }
                    }

                    break;
                //int
                case "index":
                case "method":
                case "count":
                    if (Control is TextBox t2)
                    {
                        if (int.TryParse(t2.Text, out var i))
                        {
                            Attribute.Value = i;
                        }
                        else
                        {
                            Attribute.Value = 0;
                        }
                    }

                    break;
                //double
                case "ratio":
                case "threshold":
                    if (Control is TextBox t3)
                    {
                        if (double.TryParse(t3.Text, out var d))
                        {
                            Attribute.Value = d;
                        }
                        else
                        {
                            Attribute.Value = 0;
                        }
                    }

                    break;
                //coombo
                case "recognition":
                case "action":
                case "order_by":
                case "detector":
                    if (Control is ComboBox cb2)
                        Attribute.Value = cb2.SelectedValue;
                    break;
                //object
                case "target":
                case "begin":
                case "end":
                    if (Control is StackPanel p4)
                    {
                        if (p4.Children.Count == 1)
                        {
                            if (p4.Children[0] is SAutoCompleteTextBox textBox)
                            {
                                if (bool.TryParse(textBox.Text, out var b))
                                {
                                    if (b)
                                        Attribute.Value = true;
                                }
                                else
                                {
                                    Attribute.Value = textBox.Text;
                                }
                            }
                        }
                        else
                        {
                            if (p4.Children.Count > 0)
                            {
                                if (p4.Children[0] is SAutoCompleteTextBox tb && tb.Text.Contains(","))
                                {
                                    List<List<int>> list = new List<List<int>>();
                                    foreach (var VARIABLE in p4.Children)
                                    {
                                        if (VARIABLE is TextBox textBox)
                                        {
                                            try
                                            {
                                                list.Add(textBox.Text.Split(",")
                                                    .Select(s => int.Parse(s)) // 将每个字符串转换为整数
                                                    .ToList());
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e);
                                                Growls.WarningGlobal("读取数字数组时出现错误！");
                                                return;
                                            }
                                        }
                                    }

                                    Attribute.Value = list;
                                }
                                else
                                {
                                    List<int> list = new List<int>();
                                    foreach (var VARIABLE in p4.Children)
                                    {
                                        if (VARIABLE is TextBox textBox)
                                        {
                                            if (int.TryParse(textBox.Text, out var i))
                                                list.Add(i);
                                        }
                                    }

                                    Attribute.Value = list;
                                }
                            }
                        }
                    }

                    break;
                //string
                default:
                    if (Control is TextBox t4)
                        Attribute.Value = t4.Text;
                    break;
            }
        }
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedType = (string)typeComboBox.SelectedValue;
        // var selectedAttributeType = (string)attributeTypeComboBox.SelectedItem;
        SwitchByType(selectedType, null);
    }

    private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child != null && child is T t)
            {
                return t;
            }

            if (child != null)
            {
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private void AddAutoAttribute(object sender, RoutedEventArgs e)
    {
        AddAutoAttribute(sender);
    }

    private void AddAutoAttribute(object sender, object? content = null)
    {
        if (sender is Button button)
        {
            // 找到根 Grid
            Grid grid = (Grid)VisualTreeHelper.GetParent(button);
            StackPanel rootkPanel = (StackPanel)VisualTreeHelper.GetParent(grid);

            // 找到 ScrollViewer
            ScrollViewer? scrollViewer = FindVisualChild<ScrollViewer>(rootkPanel);
            if (scrollViewer != null)
            {
                // 找到 ScrollViewer 内部的 StackPanel
                StackPanel? stackPanel = FindVisualChild<StackPanel>(scrollViewer);
                AddAutoAttribute(stackPanel, content);
            }
        }
    }

    private void AddAutoAttribute(Panel? panel, object? content)
    {
        if (panel != null)
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem deleteItem = new MenuItem { Header = "删除" };
            deleteItem.Click += DeleteAttribute;
            contextMenu.Items.Add(deleteItem);
            EditTaskDialog? taskDialog = ParentDialog as EditTaskDialog;
            if (taskDialog == null)
                return;
            SAutoCompleteTextBox newTextBox = new SAutoCompleteTextBox
            {
                Margin = new Thickness(5, 2, 5, 2), DisplayMemberPath = "Name",
                DataList = taskDialog.Data?.DataList, ItemsSource = taskDialog.Data?.DataList
            };
            if (content != null)
                newTextBox.Text = content.ToString();
            newTextBox.ContextMenu = contextMenu;

            panel.Children.Add(newTextBox);
        }
    }


    private void AddAttribute(Panel? panel, object? content)
    {
        if (panel != null)
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem deleteItem = new MenuItem { Header = "删除" };
            deleteItem.Click += DeleteAttribute;
            contextMenu.Items.Add(deleteItem);

            TextBox newTextBox = new TextBox
            {
                Margin = new Thickness(5, 2, 5, 2)
            };
            if (content != null)
                newTextBox.Text = content.ToString();
            newTextBox.ContextMenu = contextMenu;
            panel.Children.Add(newTextBox);
        }
    }

    private void AddAttribute(object sender, object? content = null)
    {
        if (sender is Button button)
        {
            // 找到根 Grid
            Grid grid = (Grid)VisualTreeHelper.GetParent(button);
            StackPanel rootkPanel = (StackPanel)VisualTreeHelper.GetParent(grid);

            // 找到 ScrollViewer
            ScrollViewer? scrollViewer = FindVisualChild<ScrollViewer>(rootkPanel);
            if (scrollViewer != null)
            {
                // 找到 ScrollViewer 内部的 StackPanel
                StackPanel? stackPanel = FindVisualChild<StackPanel>(scrollViewer);
                AddAttribute(stackPanel, content);
            }
        }
    }

    private void AddAttribute(object sender, RoutedEventArgs e)
    {
        AddAttribute(sender);
    }

    private void DeleteAttribute(object sender, RoutedEventArgs e)
    {
        MenuItem? menuItem = sender as MenuItem;
        ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
        if (contextMenu?.PlacementTarget is Control control)
        {
            var parentPanel = control.Parent as Panel;
            if (parentPanel != null)
            {
                parentPanel.Children.Remove(control);
            }
        }
    }
}