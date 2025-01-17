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

public class SaveMoneyAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(SaveMoneyAction);


    public bool Run(in IMaaContext icontext, in RunArgs iargs)
    {
        var context = icontext;
        var args = iargs;
        RecognitionDetail detail;
        context.OverrideNext(args.TaskName, []);
        var enter1 = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("Roguelike@StageTraderInvestSystem.png", image, out detail, 0.75, 400, 172, 230, 150))
            {
                context.Click(detail.HitBox.X, detail.HitBox.Y);
                return true;
            }
            return false;
        };
        if (!enter1.Until(150, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            }))
            return true;
        Thread.Sleep(300);
        var enter2 = () =>
        {
            var image = context.GetImage();
            if (context.TemplateMatch("Roguelike@StageTraderInvestSystemEnter.png", image, out detail, 0.9, 460, 273, 420, 200))
            {
                LoggerService.LogInfo($"{detail.HitBox.X}, {detail.HitBox.Y}");
                context.Click(640, 360);
                return true;
            }
            return false;
        };
        if (!enter2.Until(200, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            })) return true;
        Thread.Sleep(1000);
        var imageBuffer = context.GetImage();

        var text = context.GetText(543,334,191,65, imageBuffer);
        var before = text.ToInt();
        if (before == 999)
            return true;
        var result = 0;
        var saving = () =>
        {
            var image = context.GetImage();
            RecognitionDetail detail;
            if (context.TemplateMatch("Roguelike@StageTraderInvestSystemError.png", image, out detail, 0.9, 454,
                    191,
                    355,
                    279))
            {
                result = 1;
                return true;
            }
            if (context.TemplateMatch("notEnoughMoney.png", image, out detail, 0.9, 787,
                    457,
                    413,
                    80))
            {
                result = 2;
                return true;
            }
            if (context.TemplateMatch("fullOfMoney.png", image, out detail, 0.9, 728,
                    49,
                    551,
                    475))
            {
                result = 0;
                return true;
            }
            context.Click(980, 495);
            Thread.Sleep(70);
            context.Click(980, 495);
            Thread.Sleep(70);
            context.Click(980, 495);
            return false;
        };
        if (!saving.Until(150, maxCount: 100, errorAction: () =>
            {
                context.OverrideNext(args.TaskName, ["启动检测"]);
            })) return true;
        int after = 0;
        switch (result)
        {
            case 0:
                after = 999;
                break;
            case 1:
                // var leave1 = () =>
                // {
                //     var image = context.GetImage();
                //     if (context.TemplateMatch("Roguelike@StageTraderInvestSystemLeave.png", image, out var detail, 0.75, 480, 472, 300, 70))
                //     {
                //         context.Click(detail.HitBox.X, detail.HitBox.Y);
                //         return true;
                //     }
                //     return false;
                // };
                // leave1.Until(150, errorAction: () =>
                // {
                //     context.OverrideNext(args.TaskName, ["启动检测"]);
                // });
                //
                // var leave2 = () =>
                // {
                //     var image = context.GetImage();
                //     if (context.OCR("算了", image, out var detail, 732, 472, 145, 43))
                //     {
                //         context.Click(detail.HitBox.X, detail.HitBox.Y);
                //         return true;
                //     }
                //     return false;
                // };
                // leave2.Until(150, errorAction: () =>
                // {
                //     context.OverrideNext(args.TaskName, ["启动检测"]);
                // });
                Thread.Sleep(500);
                context.GetImage(ref imageBuffer);

                var text1 = context.GetText(632,197,78,54, imageBuffer);
                after = text1.ToInt();

                break;
            case 2:
                Thread.Sleep(500);
                context.GetImage(ref imageBuffer);

                var text2 = context.GetText(543,334,191,65, imageBuffer);
                after = text2.ToInt();

                break;
        }

        MaaProcessor.Money += after - before;
        MainWindow.AddLogByColor(
            $"已投资 {MaaProcessor.Money}(+{after - before}),存款: {after}",
            "LightSeaGreen");
        if (result != 0 && after != 999)
            context.OverrideNext(args.TaskName, ["离开肉鸽"]);
        return true;
    }
}
