using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MFAWPF.Custom;

public class ChooseTeamAction : MaaCustomAction
{
    public override string Name { get; set; } = nameof(ChooseTeamAction);

    protected override void ErrorHandle(IMaaContext context, RunArgs args)
    {
        context.OverrideNext(args.NodeName, ["启动检测"]);
    }

    protected override bool Execute(IMaaContext context, RunArgs args)
    {
        var choosing = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("houqin.png", image, out var detail, 0.81, 0, 244, 1279, 164))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }

            context.TouchDown(1, 801, 495, 100);
            context.SmoothTouchMove(1, 801, 495, 284, 495, 200, 10);
            context.SmoothTouchMove(1, 284, 495, 284, 545, 100);
            context.TouchUp(1);

            return false;
        };
        choosing.Until(700);
        var confirm = () =>
        {
            var image = context.GetImage();

            if (context.TemplateMatch("confirm.png", image, out var detail, 0.8, 60, 550, 1100, 110))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }

            return false;
        };

        confirm.Until(150);
        int i = 0;
        var afterConfirm = () =>
        {
            var image = context.GetImage();
            RecognitionDetail detail;
            if (context.TemplateMatch("sniper.png", image, out detail, 0.8, 339, 182, 307, 387))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                i = 2;
                return true;
            }
            if (i == 0 && context.TemplateMatch("Sarkaz@Roguelike@LastReward.png", image, out detail, 0.8, 205, 140, 895, 410))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                i = 1;
            }

            if (i == 0 && context.TemplateMatch("Sarkaz@Roguelike@LastReward4.png", image, out detail, 0.8, 205, 140, 895, 410))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                i = 1;
            }

            if (i == 0 && context.TemplateMatch("Sarkaz@Roguelike@LastReward3.png.png", image, out detail, 0.8, 205, 140, 895, 410))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                i = 1;
            }

            if (i == 1)
            {
                var after = () =>
                {
                    var image = context.GetImage();
                    if (context.TemplateMatch("Sarkaz@Roguelike@LastRewardConfirm.png", image, out var detail, 0.7, 155, 340, 900, 510))
                    {
                        context.Click(detail.HitBox.X, detail.HitBox.Y);
                        return true;
                    }
                    return false;
                };

                after.Until();
                Thread.Sleep(700);
            }
            return false;
        };
        afterConfirm.Until(150);
        Thread.Sleep(700);
        var confirmZ = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("confirm.png", image, out var detail, 0.7, 60, 550, 1100, 110))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }

            return false;
        };
        confirmZ.Until();
        context.OverrideNext(args.NodeName, ["招募"]);
        return true;
    }
}
