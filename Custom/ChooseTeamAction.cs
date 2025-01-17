using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MFAWPF.Custom;

public class ChooseTeamAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(ChooseTeamAction);


    public bool Run(in IMaaContext icontext, in RunArgs iargs)
    {
        var context = icontext;
        var args = iargs;
        var choosing = () =>
        {
            var image = context.GetImage();
            RecognitionDetail? detail;
            if (context.TemplateMatch("houqin.png", image, out detail, 0.81, 0, 244, 1279, 164))
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
        if (!choosing.Until(150, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            }))
            return true;
        var confirm = () =>
        {
            var image = context.GetImage();
            RecognitionDetail? detail;
            if (context.TemplateMatch("confirm.png", image, out detail, 0.8, 60, 550, 1100, 110))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }

            return false;
        };

        if (!confirm.Until(150, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            })) return true;
        int i = 0;
        var afterConfirm = () =>
        {
            var image = context.GetImage();
            RecognitionDetail? detail;
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
                    RecognitionDetail? detail;
                    if (context.TemplateMatch("Sarkaz@Roguelike@LastRewardConfirm.png", image, out detail, 0.7, 155, 340, 900, 510))
                    {
                        context.Click(detail.HitBox.X, detail.HitBox.Y);
                        return true;
                    }
                    return false;
                };

                if (!after.Until(errorAction: () =>
                    {
                        context.OverrideNext(args.TaskName, ["启动检测"]);
                    })) return true;
                Thread.Sleep(700);
            }
            return false;
        };
        if (!afterConfirm.Until(150, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            })) return true;

        var confirmZ = () =>
        {
            var image = context.GetImage();
            RecognitionDetail? detail;
            if (context.TemplateMatch("confirm.png", image, out detail, 0.7, 60, 550, 1100, 110))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }

            return false;
        };
        if (!confirmZ.Until(errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            })) return true;
        context.OverrideNext(args.TaskName, ["招募"]);
        return true;
    }
}
