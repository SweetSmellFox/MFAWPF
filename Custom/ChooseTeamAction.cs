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
                context.OverrideNext(args.TaskName, ["确认分队"]);
                return true;
            }

            context.TouchDown(1, 801, 495, 100);
            context.SmoothTouchMove(1, 801, 495, 284, 495, 200, 10);
            context.SmoothTouchMove(1, 284, 495, 284, 545, 100);
            context.TouchUp(1);

            return false;
        };
        choosing.Until(150);
        return true;
    }
}
