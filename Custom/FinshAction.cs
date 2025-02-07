using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MFAWPF.Custom;

public class FinshAction : MaaCustomAction
{
    public override string Name { get; set; } = nameof(FinshAction);


    protected override void ErrorHandle(IMaaContext context, RunArgs args)
    {
        context.OverrideNext(args.NodeName, ["启动检测"]);
    }

    protected override bool Execute(IMaaContext context, RunArgs args)
    {
        Thread.Sleep(1000);
        var confirm = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("money.png", image, out _, 0.92, 0, 429, 78, 72))
            {
                return true;
            }
            context.Click(631, 610);
            return false;
        };
        confirm.Until(150);
        context.OverrideNext(args.NodeName, ["启动检测"]);
        return true;
    }
}
