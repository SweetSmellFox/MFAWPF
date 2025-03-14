using System.Windows;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl;

public class PinButton : Button
{
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(PinButton),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsCheckedChanged));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public event RoutedPropertyChangedEventHandler<bool> CheckedChanged;

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PinButton pinButton)
        {
            pinButton.OnCheckedChanged(pinButton.IsChecked, (bool)e.NewValue);
        }
    }

    private void OnCheckedChanged(bool oldValue, bool newValue)
    {
        
        CheckedChanged?.Invoke(this, new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue));
    }

    static PinButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PinButton), new FrameworkPropertyMetadata(typeof(PinButton)));
    }

    // Constructor
    public PinButton()
    {
        // Set the default content to 📌
        Content = "📌";
        Click += (_, _) => { IsChecked = !IsChecked; };
    }
}