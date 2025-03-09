using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAWPF.Extensions.Maa;
using MFAWPF.Views.UI;

namespace MFAWPF.Helper.ValueType;

public partial class MFATask : ObservableObject
{
    public enum MFATaskType
    {
        MFA,
        MAAFW
    }

    [ObservableProperty] private string? _name = string.Empty;
    [ObservableProperty] private MFATaskType _type = MFATaskType.MFA;
    [ObservableProperty] private int _count = 1;
    [ObservableProperty] private Func<Task> _action;
    [ObservableProperty] private Dictionary<string, TaskModel> _tasks = new();


    public async Task<bool> Run(CancellationToken token)
    {
        try
        {
            for (int i = 0; i < Count; i++)
            {
                token.ThrowIfCancellationRequested();
                if (Type == MFATaskType.MAAFW)
                    RootView.AddLogByKey("TaskStart", null,true, Name ?? string.Empty);
                await Action();
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"捕获异常: {ex.Message}");
            return false;
        }
    }
}
