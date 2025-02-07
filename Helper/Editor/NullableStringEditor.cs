using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Helper.Converters;

namespace MFAWPF.Helper.Editor;

public class NullableStringEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem)
    {
        var ctrl = new System.Windows.Controls.TextBox
        {
            IsReadOnly = propertyItem.IsReadOnly
        };
        InfoElement.SetShowClearButton(ctrl, true);
        return ctrl;
    }

    public override DependencyProperty GetDependencyProperty() => System.Windows.Controls.TextBox.TextProperty;

    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new NullableStringConverter();
}