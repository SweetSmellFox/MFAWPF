using MFAWPF.ViewModels;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using Newtonsoft.Json;

// using PaddleOCRSharp;

namespace MFAWPF.Helper;

public class OCRHelper
{
    public class RecognitionQuery
    {
        [JsonProperty("all")] public List<RecognitionResult>? All;
        [JsonProperty("best")] public RecognitionResult? Best;
        [JsonProperty("filtered")] public List<RecognitionResult>? Filtered;
    }

    public class RecognitionResult
    {
        [JsonProperty("box")] public List<int>? Box;
        [JsonProperty("score")] public double? Score;
        [JsonProperty("text")] public string? Text;
    }

    public static void Initialize()
    {
    }

    public static string ReadTextFromMAATasker(int x, int y, int width, int height, IMaaTasker tasker = null)
    {
        tasker ??= MaaProcessor.Instance.GetCurrentTasker();
        string result = string.Empty;

        var job = tasker.AppendTask(new TaskModel
        {
            Recognition = "OCR",
            Roi = new []
            {
                x,
                y,
                width,
                height
            }
        });
        
        if (job.WaitFor(MaaJobStatus.Succeeded) != null)
        {
            var query =
                JsonConvert.DeserializeObject<RecognitionQuery>(job.QueryRecognitionDetail()?
                        .Detail
                    ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(query?.Best?.Text))
                result = query.Best.Text;
        }
        else
        {
            GrowlHelper.Error("识别失败！");
        }

        Console.WriteLine($"识别结果: {result}");
        return result;
    }

    public static string ReadTextFromMAAContext(in IMaaContext context,
        IMaaImageBuffer image,
        int x,
        int y,
        int width,
        int height)
    {
        var result = string.Empty;
        var taskModel = new TaskModel
        {
            Name = "AppendOCR",
            Recognition = "OCR",
            Roi = new List<int>
            {
                x,
                y,
                width,
                height
            },
        };
        var detail = context.RunRecognition(taskModel, image);

        if (detail != null)
        {
            var query = JsonConvert.DeserializeObject<RecognitionQuery>(detail.Detail);
            if (!string.IsNullOrWhiteSpace(query?.Best?.Text))
                result = query.Best.Text;
        }
        else
        {
            GrowlHelper.Error("识别失败！");
        }

        Console.WriteLine($"识别结果: {result}");

        return result;
    }

    // public static string ReadTextFromBitmapImage(BitmapImage bitmapImage, int x, int y, int width, int height)
    // {
    //     string tempFilePath = Path.GetTempFileName() + ".png";
    //
    //     // 将 BitmapImage 转换为 System.Drawing.Bitmap
    //     using (MemoryStream memoryStream = new MemoryStream())
    //     {
    //         PngBitmapEncoder encoder = new PngBitmapEncoder();
    //         encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
    //         encoder.Save(memoryStream);
    //         memoryStream.Position = 0;
    //
    //         using (Bitmap bitmap = new Bitmap(memoryStream))
    //         {
    //             // 裁剪图像区域
    //             Rectangle cropRect = new Rectangle(x, y, width, height);
    //             using (Bitmap croppedBitmap = bitmap.Clone(cropRect, bitmap.PixelFormat))
    //             {
    //                 // 保存裁剪后的图像
    //                 croppedBitmap.Save(tempFilePath, ImageFormat.Png);
    //             }
    //         }
    //     }
    //
    //     // 文字识别
    //     var Text = "";
    //     // 清理临时文件
    //     File.Delete(tempFilePath);
    //
    //     // 拼接结果文本
    //     return Text ? string.Empty;
    // }
}
