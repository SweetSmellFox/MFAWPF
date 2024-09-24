using MFAWPF.Utils;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MFAWPF.Views;

namespace MFAWPF.Custom;

public class MoneyDetectRecognizer : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyDetectRecognizer);

    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        MaaImageBuffer? imageBuffer = args.Image as MaaImageBuffer;
        string text =
            OCRHelper.ReadTextFromMAASyncContext(context, imageBuffer ?? new MaaImageBuffer(), 466, 299, 131, 63);
        if (int.TryParse(text, out var currentMoney))
        {
            Console.WriteLine($"存钱后余额：{currentMoney}");
            int getMoney = currentMoney - MaaProcessor.AllMoney;
            MaaProcessor.Money += getMoney;
            MaaProcessor.AllMoney = currentMoney;
            MainWindow.Data?
                .AddLog(
                    $"已投资 {MaaProcessor.Money}(+{getMoney}),存款: {MaaProcessor.AllMoney}",
                    System.Windows.Media.Brushes.LightSeaGreen);
            if (MaaProcessor.AllMoney == 999)
                return false;
        }

        return true;
    }
}