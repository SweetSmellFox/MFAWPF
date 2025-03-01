using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Views.UI;

namespace MFAWPF.Helper.ValueType;

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
    [ObservableProperty] private Func<Task>? _action;
    [ObservableProperty] private Dictionary<string, TaskModel> _tasks = new();
    

    public bool Run()
    {
        try
        {
            for (int i = 0; i < Count; i++)
            {
                if (Type == MFATaskType.MAAFW)
                    RootView.AddLogByKey("TaskStart", null, Name ?? string.Empty);
                Action?.Invoke().Wait();
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }

    }
}
