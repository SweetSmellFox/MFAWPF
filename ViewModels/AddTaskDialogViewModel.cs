using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MFAWPF.ViewModels;

public class AddTaskDialogViewModel: ObservableObject
{
    private ObservableCollection<DragItemViewModel> dataList = new ();

    public ObservableCollection<DragItemViewModel> DataList
    {
        get => dataList;
        set => SetProperty(ref dataList, value);
    }

    private int selectedIndex ;

    public int SelectedIndex
    {
        get => selectedIndex;
        set =>
            SetProperty(ref selectedIndex, value);
    }


    public AddTaskDialogViewModel()
    {
        SelectedIndex = -1;
    }
}