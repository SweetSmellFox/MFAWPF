using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;

namespace MFAWPF.Custom;

public class MoneyAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(MoneyAction);

    public bool Run<T>(in IMaaContext context, in RunArgs<T> args) where T : IMaaImageBuffer
    {
        return false;
    }

    public void Abort()
    {
    }
}