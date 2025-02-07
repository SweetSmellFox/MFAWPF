using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MFAWPF.Custom;

public class InputParameterAction : MaaCustomAction
{
    public override string Name { get; set; } = nameof(InputParameterAction);


    protected override void ErrorHandle(IMaaContext context, RunArgs args)
    {
        context.OverrideNext(args.NodeName, ["启动检测"]);
    }

    protected override bool Execute(IMaaContext context, RunArgs args)
    {
        var shouldRun = false;
        var enter1 = () =>
        {
            var image = context.GetImage();
            RecognitionDetail detail;
            if (context.OCR("启用参数", image, out detail, 847, 570, 98, 28))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }
            if (context.OCR("参数已启用", image, out detail, 852, 569, 104, 29))
            {
                shouldRun = true;
                return true;
            }
            return false;
        };
        enter1.Until();

        if (shouldRun)
        {
            context.OverrideNext(args.NodeName, ["开始探险"]);
            return true;
        }
        var enter2 = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("fh2.png", image, out var detail, 0.8, 185, 124, 72, 71))
            {
                context.Click(701, 226);
                return true;
            }
            return false;
        };
       enter2.Until();
        var enter3 = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("editconfirm.png", image, out var detail, 0.8, 686, 399, 562, 171))
            {
                context.Click(258, 332);
                return true;
            }
            return false;
        };
        enter3.Until();


        Thread.Sleep(150);
        var confirm = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("editconfirm.png", image, out var detail, 0.8, 693, 379, 551, 218))
            {
                context.InputText("[U1eIA1I29844Ae925i,rogue_4,6]");
                Thread.Sleep(300);
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                Thread.Sleep(600);
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }
            return false;
        };
        confirm.Until();
        var leaving = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("fh2.png", image, out var detail, 0.8, 185, 124, 72, 71))
            {
                context.Click(966, 322);
                Thread.Sleep(700);
                context.Click(1167, 338);
                return true;
            }
            return false;
        };
        leaving.Until();
        context.OverrideNext(args.NodeName, ["开始探险"]);
        return true;
    }
}
