using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UI.Dialog;

public class AddTaskDialogViewModel: ViewModel
{
    private ObservableCollection<Tool.DragItemViewModel> _dataList = new ();

    public ObservableCollection<Tool.DragItemViewModel> DataList
    {
        get => _dataList;
        set => SetProperty(ref _dataList, value);
    }

    private int _selectedIndex ;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set =>
            SetProperty(ref _selectedIndex, value);
    }


    public AddTaskDialogViewModel()
    {
        SelectedIndex = -1;
    }
}