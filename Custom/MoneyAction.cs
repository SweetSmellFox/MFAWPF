
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;

namespace MFAWPF.Actions;

public class MoneyAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(MoneyAction);

    public bool Run(in IMaaSyncContext syncContext, string taskName, string customActionParam,
        IMaaRectBuffer curBox,
        string curRecDetail)
    {
        return true;
    }

    public void Abort()
    {
    }
}