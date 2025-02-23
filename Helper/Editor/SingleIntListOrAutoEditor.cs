using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Views.UserControl;
using MFAWPF.Helper.Converters;
using MFAWPF.ViewModels;
using MFAWPF.Views.UI;
using ComboBox = System.Windows.Controls.ComboBox;

namespace MFAWPF.Helper.Editor;

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

    public static List<string> AutoProperty()
    {
        return ["Roi", "Begin", "End", "Target"];
    }

    private IEnumerable GetItemsSource(PropertyItem propertyItem)
    {
        if (AutoProperty().Contains(propertyItem.PropertyName))
        {
            var originalDataList = RootView.TaskDialog?.Data?.DataList;
            if (originalDataList != null)
            {
                var newDataList = new ObservableCollection<TaskItemViewModel>(originalDataList);
                if (propertyItem.PropertyName != "Roi")
                    newDataList.Add(new TaskItemViewModel { Name = "True" });
                return newDataList;
            }

            return null;
        }

        return new ObservableCollection<string>();
    }
    
    private string GetDisplayMemberPath(PropertyItem propertyItem)
    {
        if (AutoProperty().Contains(propertyItem.PropertyName))
        {
            return "Name";
        }

        return string.Empty;
    }

    public override DependencyProperty GetDependencyProperty() => ComboBox.TextProperty;

    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new SingleIntListOrAutoConverter();
}