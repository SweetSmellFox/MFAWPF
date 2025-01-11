using MFAWPF.Utils;
using MFAWPF.Views;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MFAWPF.Data;
using System.Threading;

namespace MFAWPF.Custom;

public class AnomalyDetectionAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(AnomalyDetectionAction);


    public bool Run(in IMaaContext icontext, in RunArgs iargs)
    {
        var context = icontext;
        var args = iargs;
        context.StartApp(DataSet.GetData("ResourceIndex", 0) == 0 ? "com.hypergryph.arknights" : "com.hypergryph.arknights.bilibili");
        var handle = () =>
        {
            var image = context.GetImage();
            RecognitionDetail? detail;
            if (context.TemplateMatch("Sarkaz@Roguelike@Abandon.png", image, out detail, 0.75, 1110, 302, 130, 130))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                context.OverrideNext(args.TaskName, ["确认放弃本次探索"]);
                return true;
            }

            if (context.TemplateMatch("Sarkaz@Roguelike@StartExplore.png", image, out detail, 0.8, 958, 530, 322, 189))
            {
                context.OverrideNext(args.TaskName, ["开始肉鸽"]);
                return true;
            }

            if (context.TemplateMatch("Roguelike@ExitThenAbandon.png", image, out detail, 0.75, 0, 0, 130, 60))
            {
                context.OverrideNext(args.TaskName, ["离开肉鸽"]);
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
                context.OverrideNext(args.TaskName, ["游戏外检测"]);
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
                context.OverrideNext(args.TaskName, ["进入长期探索"]);
                return true;
            }

            context.Click(631, 610);
            return false;
        };
        handle.Until();
        return true;
    }

    public void Abort()
    {
    }
}
