using MFAWPF.Utils;
using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.ViewModels;

namespace MFAWPF.Custom;

public class MoneyRecognition : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyRecognition);

    private const string DiffEntry = "TemplateMatch";

    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        var imageBuffer = args.Image;
        var text = context.GetText(578, 342, 131, 57, imageBuffer);
        if (int.TryParse(text, out var i))
        {
            MaaProcessor.AllMoney = i;
            Console.WriteLine($"存钱前余额：{i}");
        }

        while (true)
        {
            context.Tasker.Controller.Click(980, 495);
            context.Tasker.Controller.Screencap().Wait();
            context.Tasker.Controller.GetCachedImage(imageBuffer);
            TaskItemViewModel t1 = new()
                {
                    Task = new TaskModel
                    {
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
                    Name = DiffEntry,
                },
                t2 = new()
                {
                    Task = new TaskModel
                    {
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
                    Name = DiffEntry,
                },
                t3 = new()
                {
                    Task = new TaskModel
                    {
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
                    },
                    Name = DiffEntry,
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
