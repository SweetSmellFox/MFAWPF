using MFAWPF.Utils;
using MFAWPF.Views;
using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using System.Collections.Generic;
using System.Linq;
namespace MFAWPF.Custom;

public class MoneyRecognition : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyRecognition);

    private const string DiffEntry = "TemplateMatch";

    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        var imageBuffer = args.Image;
        var text = context.GetText(578, 342, 131, 57, imageBuffer).Replace("b", "6").Replace("B", "8");
        if (int.TryParse(text, out var i))
        {
            MaaProcessor.AllMoney = i;
            LoggerService.LogInfo($"存钱前余额：{i}");
        }

        while (true)
        {
            context.Click(980, 495);
            context.Screencap();
            context.GetCachedImage(imageBuffer);
            TaskModel t1 = new()
                {
                    Name = DiffEntry,
                    Recognition = "TemplateMatch",
                    Roi = new List<int>
                    {
                        454,
                        191,
                        355,
                        279
                    },
                    Template = ["Roguelike@StageTraderInvestSystemError.png"],
                    Threshold = 0.9
                },
                t2 = new()
                {
                    Name = DiffEntry,
                    Recognition = "TemplateMatch",
                    Roi = new List<int>
                    {
                        787,
                        457,
                        413,
                        80
                    },
                    Template = ["notEnoughMoney.png"],
                    Threshold = 0.9
                },
                t3 = new()
                {
                    Name = DiffEntry,
                    Recognition = "TemplateMatch",
                    Roi = new List<int>
                    {
                        728,
                        49,
                        551,
                        475
                    },
                    Template = ["fullOfMoney.png"],
                    Threshold = 0.9
                };
            var rd1 = context.RunRecognition(t1, imageBuffer);
            if (rd1.IsHit())
            {
                break;
            }
            context.Click(980, 495);

            var rd2 = context.RunRecognition(t2, imageBuffer);
            if (rd2.IsHit())
            {
                break;
            }
            context.Click(980, 495);

            var rd3 = context.RunRecognition(t3, imageBuffer);
            if (rd3.IsHit())
            {
                break;
            }
            context.Click(980, 495);

        }

        return true;
    }
}
