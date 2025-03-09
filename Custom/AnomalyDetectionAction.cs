using MFAWPF.Helper;
using MFAWPF.Views;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using System.Threading;

namespace MFAWPF.Custom;

public class AnomalyDetectionAction : MaaCustomAction
{
    public override string Name { get; set; } = nameof(AnomalyDetectionAction);

    protected override void ErrorHandle(IMaaContext context, RunArgs args)
    {
        context.OverrideNext(args.NodeName, ["启动检测"]);
    }

    protected override bool Execute(IMaaContext context, RunArgs args)
    {
        var shouldWaiting = false;
        context.OverrideNext(args.NodeName, ["启动检测"]);
        context.StartApp(ConfigurationHelper.GetValue("ResourceIndex", 0) == 0 ? "com.hypergryph.arknights" : "com.hypergryph.arknights.bilibili");
        var handle = () =>
        {
            var image = context.GetImage();
            RecognitionDetail detail;
            if (context.TemplateMatch("Sarkaz@Roguelike@Abandon.png", image, out detail, 0.75, 1110, 302, 130, 130))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                context.OverrideNext(args.NodeName, ["确认放弃本次探索"]);
                return true;
            }

            if (context.TemplateMatch("Sarkaz@Roguelike@StartExplore.png", image, out detail, 0.8, 958, 530, 322, 189))
            {
                context.OverrideNext(args.NodeName, ["开始肉鸽"]);
                return true;
            }

            if (context.TemplateMatch("money.png", image, out _, 0.8, 0, 429, 78, 72))
            {
                shouldWaiting = true;
                return true;
            }

            var tryReturn = () =>
            {
                if (context.TemplateMatch("Return.png", image, out detail, x: 10, y: 10, w: 160, h: 60))
                {
                    context.Click(detail.HitBox.X, detail.HitBox.Y);
                    context.GetImage(ref image);
                    return false;
                }
                return true;
            };

            if (context.TemplateMatch("back2.png", image, out detail, 0.8, 0, 0, 167, 65))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return false;
            }

            if (context.TemplateMatch("Roguelike@ExitThenAbandon.png", image, out detail, 0.75, 0, 0, 130, 60))
            {
                context.OverrideNext(args.NodeName, ["离开肉鸽"]);
                return true;
            }


            tryReturn.Until(700);

            var inStartUI = context.TemplateMatch("startGame.png", image, out detail, 0.75, 568, 624, 149, 95);
            if (inStartUI)
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
            }

            if (inStartUI)
            {
                Thread.Sleep(1500);
                context.GetImage(ref image);
            }
            if (context.OCR("开始唤醒", image, out detail, 580, 490, 140, 50))
            {
                context.OverrideNext(args.NodeName, ["游戏外检测"]);
                return true;
            }

            if (context.TemplateMatch("close.png", image, out detail, 0.75, 1122, 1, 157, 162))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return false;
            }

            if (context.TemplateMatch("originiums.png", image, out detail, 0.75, 1033, 19, 86, 69, true))
            {
                context.Click(935, 189);
                context.OverrideNext(args.NodeName, ["进入长期探索"]);
                return true;
            }

            context.Click(631, 610);
            return false;
        };
        handle.Until();
        if (shouldWaiting)
        {
            var waiting = () =>
            {
                var image = context.GetImage();
                if (context.TemplateMatch("Sarkaz@Roguelike@StartExplore.png", image, out _, 0.8, 958, 530, 322, 189))
                {
                    context.OverrideNext(args.NodeName, ["开始肉鸽"]);
                    return true;
                }
                return false;
            };

            waiting.Until(3000);
        }
        return true;
    }
}
