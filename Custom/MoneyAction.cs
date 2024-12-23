using MFAWPF.Utils;
using MFAWPF.Views;
using MaaFramework.Binding;
using MaaFramework.Binding.Custom;

namespace MFAWPF.Custom;

public class MoneyAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(MoneyAction);

    public bool Run(in IMaaContext context, in RunArgs args)
    {
        
        return false;
    }

    public void Abort()
    {
    }
}