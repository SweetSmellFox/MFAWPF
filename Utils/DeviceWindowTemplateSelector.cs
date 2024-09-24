using System.Windows;
using System.Windows.Controls;
using MaaFramework.Binding;

namespace MFAWPF.Utils;

public class DeviceWindowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DeviceInfoTemplate { get; set; }
    public DataTemplate? WindowInfoTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is AdbDeviceInfo)
            return DeviceInfoTemplate ?? base.SelectTemplate(item, container);
        
        if (item is DesktopWindowInfo)
            return WindowInfoTemplate ?? base.SelectTemplate(item, container);
        

        return base.SelectTemplate(item, container);
    }
}
