using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Views;

namespace MFAWPF.Helper;

public partial class MFATask : ObservableObject
{
    public enum MFATaskType
    {
        MFA,
        MAAFW
    }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private MFATaskType _type = MFATaskType.MFA;
    [ObservableProperty] private int _count = 1;
    [ObservableProperty] private Action _action;
    [ObservableProperty] private Dictionary<string, TaskModel> _tasks = new();


    public bool Run()
    {
        try
        {
            for (int i = 0; i < Count; i++)
            {
                if (Type == MFATaskType.MAAFW)
                    MainWindow.AddLogByKey("TaskStart", null, Name ?? string.Empty);
                Action();
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }

    }
}
