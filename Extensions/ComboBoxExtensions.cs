using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MFAWPF.Extensions;

public static class ComboBoxExtensions
{
    public static readonly DependencyProperty DisableNavigationOnLostFocusProperty =
        DependencyProperty.RegisterAttached(
            "DisableNavigationOnLostFocus",
            typeof(bool),
            typeof(ComboBoxExtensions),
            new PropertyMetadata(false, OnDisableNavigationOnLostFocusChanged));

    public static bool GetDisableNavigationOnLostFocus(DependencyObject obj) =>
        (bool)obj.GetValue(DisableNavigationOnLostFocusProperty);

    public static void SetDisableNavigationOnLostFocus(DependencyObject obj, bool value) =>
        obj.SetValue(DisableNavigationOnLostFocusProperty, value);

    private static void OnDisableNavigationOnLostFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComboBox comboBox)
        {
            comboBox.PreviewKeyDown -= HandlePreviewKeyDown;
            comboBox.PreviewMouseWheel -= HandlePreviewMouseWheel;

            if ((bool)e.NewValue)
            {
                comboBox.PreviewKeyDown += HandlePreviewKeyDown;
                comboBox.PreviewMouseWheel += HandlePreviewMouseWheel;
            }
        }
    }
    
    private static bool IsInputAllowed(ComboBox comboBox) =>
        comboBox.IsDropDownOpen;
    
    private static void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        if (!IsInputAllowed(comboBox) && (e.Key == Key.Up || e.Key == Key.Down))
        {
            e.Handled = true; 
        }
    }
    
    private static void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        if (!IsInputAllowed(comboBox))
        {
            e.Handled = true;
        }
    }
}