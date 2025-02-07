using System.Windows;
using System.Windows.Controls;
using Attribute = MFAWPF.Helper.Attribute;

namespace MFAWPF.Controls;

public class AttributeButton : Button
{
    public Attribute Attribute
    {
        get => (Attribute)GetValue(AttributeProperty);
        set => SetValue(AttributeProperty, value);
    }

    public static readonly DependencyProperty AttributeProperty =
        DependencyProperty.Register(nameof(Attribute), typeof(Attribute), typeof(AttributeButton),
            new PropertyMetadata(null, OnAttributeValueChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(AttributeButton),
            new PropertyMetadata(false));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public CustomWindow WindowParent { get; init; }

    static string ConvertListToString(List<List<int>> listOfLists)
    {
        var formattedLists = listOfLists
            .Select(innerList => $"[{string.Join(",", innerList)}]");
        return string.Join(",", formattedLists);
    }

    private static void OnAttributeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AttributeButton button)
        {
            if (e.NewValue == null)
            {
                if (button.Parent is Panel parentPanel)
                    parentPanel.Children.Remove(button);
            }
            else if (e.NewValue is Attribute attribute)
            {
                if (string.IsNullOrWhiteSpace(attribute.Value?.ToString()))
                {
                    if (button.Parent is Panel parentPanel)
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

                    // 使用 CustomTextBlock 包装内容并设置 TextTrimming

                    var textBlock = new CustomTextBlock
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
        Click += (_, _) => { IsSelected = !IsSelected; };
        Style = FindResource("AttributeButtonStyle") as Style;
    }
}