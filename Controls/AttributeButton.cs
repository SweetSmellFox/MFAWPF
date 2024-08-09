using System;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Data;
using MFAWPF.Views;
using Newtonsoft.Json;
using Attribute = MFAWPF.Utils.Attribute;

namespace MFAWPF.Controls;

public class AttributeButton : Button
{
    public Attribute Attribute
    {
        get => (Attribute)GetValue(AttributeProperty);
        set => SetValue(AttributeProperty, value);
    }

    public static readonly DependencyProperty AttributeProperty =
        DependencyProperty.Register("Attribute", typeof(Attribute), typeof(AttributeButton),
            new PropertyMetadata(null, OnAttributeValueChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(AttributeButton),
            new PropertyMetadata(false));

    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    public CustomWindow WindowParent { get; set; }

    static string ConvertListToString(List<List<int>> listOfLists)
    {
        var formattedLists = listOfLists
            .Select(innerList => $"[{string.Join(",", innerList)}]");
        return string.Join(",", formattedLists);
    }

    private static void OnAttributeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = d as AttributeButton;
        if (button != null)
        {
            if (e.NewValue == null)
            {
                var parentPanel = button.Parent as Panel;
                if (parentPanel != null)
                    parentPanel.Children.Remove(button);
            }
            else if (e.NewValue is Attribute attribute)
            {
                if (string.IsNullOrWhiteSpace(attribute.Value.ToString()))
                {
                    var parentPanel = button.Parent as Panel;
                    if (parentPanel != null)
                        parentPanel.Children.Remove(button);
                }
                else
                {
                    string contentText;
                    if (attribute.Value is List<List<int>> lli)
                    {
                        contentText = $"{button.Attribute.Key}: {ConvertListToString(lli)}";
                    }
                    else if (attribute.Value is List<int> li)
                    {
                        contentText = $"{button.Attribute.Key}: {string.Join(",", li)}";
                    }
                    else if (attribute.Value is List<string> ls)
                    {
                        contentText = $"{button.Attribute.Key}: {string.Join(",", ls)}";
                    }
                    else
                    {
                        contentText = $"{button.Attribute.Key}: {attribute.Value}";
                    }

                    // 使用 TextBlock 包装内容并设置 TextTrimming

                    var textBlock = new TextBlock
                    {
                        Text = contentText,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        MaxWidth = 310,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        TextWrapping = TextWrapping.NoWrap
                    };

                    button.Content = textBlock;
                }
            }
        }
    }


    static AttributeButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AttributeButton),
            new FrameworkPropertyMetadata(typeof(AttributeButton)));
    }

    public AttributeButton()
    {

        this.Click += (s, e) => { IsSelected = !IsSelected; };
        Style = FindResource("AttributeButtonStyle") as Style;

    }
  
}