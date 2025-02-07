using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Controls;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels;
using MFAWPF.Views;

namespace MFAWPF.Helper.Editor;

public class ListAutoStringEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem)
    {
        var autoListControl = new CustomAutoListControl
        {
            MinHeight = 50, TaskDialogDataList = GetItemsSource(propertyItem),
            DisplayMemberPath = GetDisplayMemberPath(propertyItem)
        };
        
        return autoListControl;
    }

    // 实现抽象方法，返回 ItemsProperty 作为绑定的 DependencyProperty
    public override DependencyProperty GetDependencyProperty() => CustomAutoListControl.ItemsProperty;

    // 动态设置 ItemsSource，根据字段名称定制选项
    public static List<string> AutoProperty()
    {
        return ["Roi", "Next", "OnError", "Interrupt", "Begin", "End", "Target"];
    }

    private IEnumerable GetItemsSource(PropertyItem propertyItem)
    {
        if (propertyItem.PropertyName == "Roi" || propertyItem.PropertyName == "Next" ||
            propertyItem.PropertyName == "OnError" || propertyItem.PropertyName == "Interrupt")
        {
            return MainWindow.TaskDialog?.Data?.DataList;
        }

        if (AutoProperty().Contains(propertyItem.PropertyName))
        {
            var originalDataList = MainWindow.TaskDialog?.Data?.DataList;
            if (originalDataList != null)
            {
                var newDataList = new ObservableCollection<TaskItemViewModel>(originalDataList);
                newDataList.Add(new TaskItemViewModel { Name = "True" });
                return newDataList;
            }

            return null;
        }

        if (propertyItem.PropertyName.Contains("Color"))
        {
            return MainWindow.TaskDialog?.Data?.Colors;
        }

        return new ObservableCollection<string>();
    }

    // 根据属性的字段名称，动态返回不同的 DisplayMemberPath
    private string GetDisplayMemberPath(PropertyItem propertyItem)
    {
        if (AutoProperty().Contains(propertyItem.PropertyName))
        {
            return "Name";
        }

        if (propertyItem.PropertyName.Contains("Color"))
        {
            return "Name";
        }

        return string.Empty; // 默认的 DisplayMemberPath
    }


    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new ListStringConverter();
}