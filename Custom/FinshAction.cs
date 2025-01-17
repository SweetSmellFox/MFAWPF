using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MFAWPF.Custom;

public class FinshAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(FinshAction);


    public bool Run(in IMaaContext icontext, in RunArgs iargs)
    {
        var context = icontext;
        var args = iargs;
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
        if (!confirm.Until(150, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            }))
            return true;
        context.OverrideNext(args.TaskName, ["启动检测"]);
        return true;
    }
}
