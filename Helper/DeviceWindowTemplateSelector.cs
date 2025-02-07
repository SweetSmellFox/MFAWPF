using System.Windows;
using System.Windows.Controls;
using MaaFramework.Binding;

namespace MFAWPF.Helper;

public class DeviceWindowTemplateSelector : DataTemplateSelector
{
    public DataTemplate DeviceInfoTemplate { get; set; }
    public DataTemplate WindowInfoTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is AdbDeviceInfo)
            return DeviceInfoTemplate ;
        
        if (item is DesktopWindowInfo)
            return WindowInfoTemplate;
        

        return base.SelectTemplate(item, container);
    }
}
