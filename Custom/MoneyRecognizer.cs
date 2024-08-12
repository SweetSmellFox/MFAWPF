using MFAWPF.Utils;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;

namespace MFAWPF.Actions;

public class MoneyRecognizer : IMaaCustomRecognizer
{
    public string Name { get; set; } = nameof(MoneyRecognizer);

    private const string DiffEntry1 = "TemplateMatch";
    private const string DiffEntry2 = "TemplateMatch";
    private const string DiffEntry3 = "TemplateMatch";

    private const string DiffParam1 = $$"""
                                        {
                                            "{{DiffEntry1}}": {
                                                "recognition": "TemplateMatch",
                                                "template": "Roguelike@StageTraderInvestSystemError.png",
                                                "threshold": 0.9
                                            }
                                        }
                                        """;

    private const string DiffParam2 = $$"""
                                        {
                                            "{{DiffEntry2}}": {
                                                "recognition": "TemplateMatch",
                                                "template": "notEnoughMoney.png",
                                                "threshold": 0.9
                                            }
                                        }
                                        """;

    private const string DiffParam3 = $$"""
                                        {
                                            "{{DiffEntry3}}": {
                                                "recognition": "TemplateMatch",
                                                "template": "fullOfMoney.png",
                                                "threshold": 0.9
                                            }
                                        }
                                        """;

    public bool Analyze(in IMaaSyncContext syncContext, IMaaImageBuffer image, string taskName,
        string customRecognitionParam, in IMaaRectBuffer outBox, in IMaaStringBuffer outDetail)
    {
        MaaImageBuffer? imageBuffer = image as MaaImageBuffer;
        string text = OCRHelper.ReadTextFromMAASyncContext(syncContext, imageBuffer ?? new MaaImageBuffer(), 578,
            342,
            131,
            57);
        if (int.TryParse(text, out var i))
        {
            MaaProcessor.AllMoney = i;
            Console.WriteLine($"存钱前余额：{i}");
        }

        bool r1, r2, r3;

        while (true)
        {
            syncContext.Click(980, 495);
            imageBuffer ??= new MaaImageBuffer();
            syncContext.Screencap(imageBuffer);
            r1 = syncContext.RunRecognizer(imageBuffer, DiffEntry1, DiffParam1, new MaaRectBuffer()
            {
                X = 454, Y = 191, Width = 355, Height = 279
            }, outDetail);
            if (r1)
                break;
            syncContext.Click(980, 495);
            r2 = syncContext.RunRecognizer(imageBuffer, DiffEntry2, DiffParam2, new MaaRectBuffer()
            {
                X = 787, Y = 457, Width = 413, Height = 80
            }, outDetail);
            if (r2)
                break;
            syncContext.Click(980, 495);

            r3 = syncContext.RunRecognizer(imageBuffer, DiffEntry3, DiffParam3, new MaaRectBuffer()
            {
                X = 728, Y = 49, Width = 551, Height = 475
            }, outDetail);
            if (r3)
                break;
            syncContext.Click(980, 495);
        }

        return true;
    }
}