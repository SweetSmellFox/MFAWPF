using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Views.Controls;
using MFAWPF.Helper.Converters;
using MFAWPF.Views.Controls;

namespace MFAWPF.Helper.Editor;

public class ListStringEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem) => new CustomListControl
    {
        MinHeight = 50
    };

    public override BindingMode GetBindingMode(PropertyItem propertyItem) => BindingMode.TwoWay;

    public override DependencyProperty GetDependencyProperty() => CustomListControl.ItemsProperty;

    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new ListStringConverter();
}