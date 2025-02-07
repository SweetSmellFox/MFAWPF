using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MFAWPF.ViewModels;

public class AddTaskDialogViewModel: ViewModel
{
    private ObservableCollection<DragItemViewModel> _dataList = new ();

    public ObservableCollection<DragItemViewModel> DataList
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