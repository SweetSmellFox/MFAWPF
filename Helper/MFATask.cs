using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Views;

namespace MFAWPF.Helper;

public class MFATask : ObservableObject
{
    public enum MFATaskType
    {
        MFA,
        MAAFW
    }

    private string _name = string.Empty;
    private MFATaskType _type = MFATaskType.MFA;
    private int _count = 1;
    private Action _action;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public MFATaskType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public Action Action
    {
        get => _action;
        set => SetProperty(ref _action, value);
    }

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

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
