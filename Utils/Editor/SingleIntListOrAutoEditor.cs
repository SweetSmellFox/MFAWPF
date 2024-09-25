using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Controls;
using MFAWPF.ViewModels;
using MFAWPF.Views;

namespace MFAWPF.Utils.Converters;

public class SingleIntListOrAutoEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem)
    {
        var ctrl = new CustomAutoCompleteTextBox
        {
            IsReadOnly = propertyItem.IsReadOnly, DataList = GetItemsSource(propertyItem),
            DisplayMemberPath = GetDisplayMemberPath(propertyItem)
        };
        InfoElement.SetShowClearButton(ctrl, true);
        return ctrl;
    }

    // 动态设置 ItemsSource，根据字段名称定制选项
    public static List<string> AutoProperty()
    {
        return ["Roi", "Begin", "End", "Target"];
    }

    private IEnumerable? GetItemsSource(PropertyItem propertyItem)
    {
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

        return new ObservableCollection<string>();
    }

    // 根据属性的字段名称，动态返回不同的 DisplayMemberPath
    private string GetDisplayMemberPath(PropertyItem propertyItem)
    {
        if (AutoProperty().Contains(propertyItem.PropertyName))
        {
            return "Name";
        }

        return string.Empty;
    }

    public override DependencyProperty GetDependencyProperty() => CustomAutoCompleteTextBox.TextProperty;

    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new SingleIntListOrAutoConverter();
}