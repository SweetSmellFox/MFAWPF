using MFAWPF.Utils;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MFAWPF.ViewModels;

namespace MFAWPF.Custom;

public class MoneyRecognition : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyRecognition);

    private const string DiffEntry = "TemplateMatch";

    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        MaaImageBuffer? imageBuffer = args.Image as MaaImageBuffer;
        string text = OCRHelper.ReadTextFromMAASyncContext(context, imageBuffer ?? new MaaImageBuffer(), 578,
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
            context.Tasker.Controller.GetCachedImage(imageBuffer);
            var rd1 =
                context.RunRecognition(DiffEntry, new TaskItemViewModel()
                {
                    Task = new TaskModel
                    {
                        Recognition = "TemplateMatch", Roi = new List<int>
                        {
                            454, 191,
                            355, 279
                        },
                        Template = new List<string> { "Roguelike@StageTraderInvestSystemError.png" },
                        Threshold = 0.9
                    },
                    Name = DiffEntry,
                }.ToString(), imageBuffer) as RecognitionDetail<MaaImageBuffer>;
            if (rd1 != null)
                break;
            context.Tasker.Controller.Click(980, 495);
            var rd2 = context.RunRecognition(DiffEntry, new TaskItemViewModel()
            {
                Task = new TaskModel
                {
                    Recognition = "TemplateMatch", Roi = new List<int>
                    {
                        787, 457,
                        413, 80
                    },
                    Template = new List<string> { "notEnoughMoney.png" },
                    Threshold = 0.9
                },
                Name = DiffEntry,
            }.ToString(), imageBuffer);
            if (rd2 != null)
                break;
            context.Tasker.Controller.Click(980, 495);

            var rd3 = context.RunRecognition(DiffEntry, new TaskItemViewModel()
            {
                Task = new TaskModel
                {
                    Recognition = "TemplateMatch", Roi = new List<int>
                    {
                        728, 49,
                        551, 475
                    },
                    Template = new List<string> { "fullOfMoney.png" },
                    Threshold = 0.9
                },
                Name = DiffEntry,
            }.ToString(), imageBuffer);
            if (rd3 != null)
                break;
            context.Tasker.Controller.Click(980, 495);
        }

        return true;
    }
}