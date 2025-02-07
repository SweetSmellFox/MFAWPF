using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime;

namespace MFAWPF.Custom;

public class ChooseOperatorAciton : MaaCustomAction
{
    public override string Name { get; set; } = nameof(ChooseOperatorAciton);


    protected override void ErrorHandle(IMaaContext context, RunArgs args)
    {
        context.OverrideNext(args.NodeName, ["启动检测"]);
    }

    protected override bool Execute(IMaaContext context, RunArgs args)
    {
        int i = 0;
        context.OverrideNext(args.NodeName, []);
        var choosing = () =>
        {
            var image = context.GetImage();
            RecognitionDetail detail;
            if (context.TemplateMatch("Sarkaz@Roguelike@EnterAfterRecruit.png", image, out detail, 0.7, 1050, 226, 220, 250))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                var enterMap = () =>
                {
                    context.GetImage(ref image);
                    if (context.TemplateMatch("rougelikeInfo.png", image, out detail, 0.8, 53, 1, 178, 64))
                    {
                        return true;
                    }
                    context.Click(1142, 369);
                    return false;
                };
                enterMap.Until();
                return true;
            }
            bool catchO = false;
            if (i++ == 0)
            {
                if (args.RecognitionDetail.HitBox != null)
                {
                    context.Click(args.RecognitionDetail.HitBox.X, args.RecognitionDetail.HitBox.Y);
                    catchO = true;
                }
            }
            else
            {
                if (context.TemplateMatch("Sarkaz@Roguelike@Recruit.png", image, out detail, 0.7, 198, 468, 951, 84))
                {
                    context.Click(detail.HitBox.X, detail.HitBox.Y);
                    catchO = true;
                }
            }

            var abandonOp = () =>
            {
                context.GetImage(ref image);
                if (context.TemplateMatch("abandon.png", image, out detail, 0.8, 912, 654, 120, 50))
                {
                    context.Click(detail.HitBox.X, detail.HitBox.Y);
                    return true;
                }
                return false;
            };
            var confirmAbandon = () =>
            {
                context.GetImage(ref image);
                if (context.TemplateMatch("Sarkaz@Roguelike@StageTraderRefreshConfirm.png", image, out detail, 0.8, 640, 414, 504, 154))
                {
                    context.Click(detail.HitBox.X, detail.HitBox.Y);
                    return true;
                }
                return false;
            };

            if (catchO)
            {
                abandonOp.Until();
                confirmAbandon.Until();
            }
            return false;
        };
        choosing.Until();
        context.OverrideNext(args.NodeName, ["关卡检测"]);
        return true;
    }
}
