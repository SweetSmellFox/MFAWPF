using MFAWPF.Utils;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MaaFramework.Binding.Interop.Native;
using MFAWPF.ViewModels;

namespace MFAWPF.Custom;

public class MoneyRecognition : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyRecognition);

    private const string DiffEntry = "TemplateMatch";

    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        MaaImageBuffer? imageBuffer = args.Image as MaaImageBuffer;
        string text = OCRHelper.ReadTextFromMAAContext(context, imageBuffer ?? new MaaImageBuffer(), 578,
            342,
            131,
            57);
        if (int.TryParse(text, out var i))
        {
            MaaProcessor.AllMoney = i;
            Console.WriteLine($"存钱前余额：{i}");
        }


        while (true)
        {
            context.Tasker.Controller.Click(980, 495);
            imageBuffer ??= new MaaImageBuffer();
            context.Tasker.Controller.Screencap().Wait();
            context.Tasker.Controller.GetCachedImage(imageBuffer);
            TaskItemViewModel t1 = new()
                {
                    Task = new TaskModel
                    {
                        Recognition = "TemplateMatch", Roi = new List<int>
                        {
                            454, 191,
                            355, 279
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
                        Recognition = "TemplateMatch", Roi = new List<int>
                        {
                            787, 457,
                            413, 80
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
                        Recognition = "TemplateMatch", Roi = new List<int>
                        {
                            728, 49,
                            551, 475
                        },
                        Template = ["fullOfMoney.png"],
                        Threshold = 0.9
                    },
                    Name = DiffEntry,
                };
            var rd1 =
                context.RunRecognition(DiffEntry, t1.ToString(),
                    imageBuffer);
            if (rd1.IsHit())
            {
                break;
            }

            context.Tasker.Controller.Click(980, 495);
            var rd2 =
                context.RunRecognition(DiffEntry, t2.ToString(), imageBuffer);
            if (rd2.IsHit())
            {
                break;
            }


            context.Tasker.Controller.Click(980, 495);

            var rd3 =
                context.RunRecognition(DiffEntry, t3.ToString(), imageBuffer);
            if (rd3.IsHit())
            {
                break;
            }


            context.Tasker.Controller.Click(980, 495);
        }

        return true;
    }
}