# Custom Recognition/Action

> Note: This document is about how to use C# to register the corresponding custom recognition/action.

## Custom Recognition

```json
{
    "Task": {
        "recognition": "Custom",
        "custom_recognition": "MoneyRecognition",
        "custom_recognition_param": {
            "msg": "Hello world!"
        }
    }
}
```

```csharp
using MFAWPF.Utils;
using MFAWPF.Views;
using MaaFramework.Binding;
using MaaFramework.Binding.Custom;

namespace MFAWPF.Custom;

public class MoneyRecognition : IMaaCustomRecognition
{
    public string Name { get; set; } = nameof(MoneyRecognition);

    private const string DiffEntry = "TemplateMatch";

    public bool Analyze(in IMaaContext context, in AnalyzeArgs args, in AnalyzeResults results)
    {
        //   var param = args.RecognitionParam;  Get the parameters passed in by custom_recognition_param, which is a String
        //   var imageBuffer = args.Image;  Get the screenshot image
        //   var roi = args.Roi; Get the coordinate array
        //   LoggerService.LogInfo("Message"); Log output information
        //   LoggerService.LogWarning("Warning"); Log output warning
        //   LoggerService.LogError("Error"); Log output error
        //   Growls.Info("Message"); Prompt message
        //   Growls.InfoGlobal("Global message"); Global prompt message
        //   Growls.Warning("Warning"); Prompt warning
        //   Growls.WarningGlobal("Global warning"); Global prompt warning
        //   Growls.Error("Error"); Prompt error
        //   Growls.ErrorGlobal("Global error"); Global prompt error
        //   context.Click(980, 495); Click
        //   context.Swipe(980, 495, 0, 0, 1000); Swipe
        //   context.TouchDown(1, 980, 495, 5); Press down
        //   context.TouchMove(1); Move
        //   context.PressKey(0); Press a key
        //   context.Screencap(); Take another screenshot
        //   context.GetCachedImage(imageBuffer); Get the image after screenshot
        //   var text = context.GetText(578, 342, 131, 57, imageBuffer); Recognize the text in the specified roi of the imageBuffer screenshot
        //   MainWindow.AddLog("Message","LightSeaGreen"); Output a message in the right log of MFA. The second parameter is the color of the message, which can be omitted and defaults to Gray
        
        //   TaskModel t1 = new()
        //       {
        //            Name = DiffEntry,
        //            Recognition = "TemplateMatch",
        //            Roi = new List<int>
        //           {
        //                454,
        //               191,
        //                355,
        //                279
        //           },
        //            Template = ["Roguelike@StageTraderInvestSystemError.png"],
        //            Threshold = 0.9
        //        }
        //    var rd1 = context.RunRecognition(t1, imageBuffer);
        //    if (rd1.IsHit())
        //     {
        //        break;
        //    }
        //  Execute a specific recognizer. IsHit() is used to determine whether it is recognized

        return true;  
    }
}
```

## Custom Action

```json
{
    "Task": {
        "action": "Custom",
        "custom_action": "MoneyAction",
        "custom_action_param": {
            "msg": "Hello world!"
        }
    }
}
```

```csharp
using MFAWPF.Utils;
using MFAWPF.Views;
using MaaFramework.Binding;
using MaaFramework.Binding.Custom;

namespace MFAWPF.Custom;

public class MoneyAction : IMaaCustomAction
{
    public string Name { get; set; } = nameof(MoneyAction);

    public bool Run(in IMaaContext context, in RunArgs args)
    {
        // var param = args.ActionParam; Get custom_action_param
        return false;
    }

    public void Abort()
    {
    }
}
```