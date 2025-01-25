using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PasswordBox = HandyControl.Controls.PasswordBox;

namespace MFAWPF.Controls;

[TemplatePart(Name = ElementPasswordBox, Type = typeof(System.Windows.Controls.PasswordBox))]
[TemplatePart(Name = ElementTextBox, Type = typeof(TextBox))]
public class CustomPasswordBox : PasswordBox
{
    private const string ElementPasswordBox = "PART_PasswordBox";

    private const string ElementTextBox = "PART_TextBox";


    private TextBox _textBox;

    public CustomPasswordBox()
    {
        Style = FindResource("PasswordBoxPlusBaseStyle") as Style;
    }

    public override void OnApplyTemplate()
    {
        Type type = typeof(PasswordBox);


        var textBoxField = type.GetField("_textBox", BindingFlags.NonPublic | BindingFlags.Instance);
        if (textBoxField != null)
        {
            if (textBoxField.GetValue(this) is TextBox textBox)
                _textBox = textBox;
        }
        if (ActualPasswordBox != null)
            ActualPasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;

        if (_textBox != null)
            _textBox.TextChanged -= TextBox_TextChanged;

        base.OnApplyTemplate();

        ActualPasswordBox = GetTemplateChild(ElementPasswordBox) as System.Windows.Controls.PasswordBox;
        _textBox = GetTemplateChild(ElementTextBox) as TextBox;
        if (ActualPasswordBox != null)
        {
            ActualPasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        if (_textBox != null)
        {
            _textBox.TextChanged += TextBox_TextChanged;
        }
    }


    public static readonly RoutedEvent PasswordChangedEvent = EventManager.RegisterRoutedEvent(
        "PasswordChanged", // Event name
        RoutingStrategy.Bubble, //
        typeof(RoutedEventHandler), //
        typeof(CustomPasswordBox)); //

    public event RoutedEventHandler PasswordChanged
    {
        add =>
            AddHandler(PasswordChangedEvent, value);


        remove =>
            RemoveHandler(PasswordChangedEvent, value);
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(PasswordChangedEvent));
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(PasswordChangedEvent));
    }
}
