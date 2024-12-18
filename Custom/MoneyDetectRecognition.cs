using MFAWPF.Utils;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MFAWPF.Views;

namespace MFAWPF.Custom;

public class MoneyDetectRecognition : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyDetectRecognition);


    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        var imageBuffer = args.Image;
        var text = context.GetText(466, 299, 131, 63, imageBuffer);
        if (int.TryParse(text, out var currentMoney))
        {
            int getMoney = currentMoney - MaaProcessor.AllMoney;
            MaaProcessor.Money += getMoney;
            MaaProcessor.AllMoney = currentMoney;
            MainWindow.AddLog(
                $"已投资 {MaaProcessor.Money}(+{getMoney}),存款: {MaaProcessor.AllMoney}",
                "LightSeaGreen");
            if (MaaProcessor.AllMoney == 999)
                return false;
        }
        return true;
    }
}
