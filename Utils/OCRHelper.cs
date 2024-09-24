using MFAWPF.ViewModels;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using Newtonsoft.Json;

// using PaddleOCRSharp;

namespace MFAWPF.Utils;

public class OCRHelper
{
    public class RecognitionQuery
    {
        public List<RecognitionResult>? all;
        public RecognitionResult? best;
        public List<RecognitionResult>? filtered;
    }

    public class RecognitionResult
    {
        public List<int>? box;
        public double? score;
        public string? text;
    }

    public static void Initialize()
    {
    }

    public static string ReadTextFromMAARecognition(int x, int y, int width, int height)
    {
        string result = string.Empty;
        TaskItemViewModel taskItemViewModel = new TaskItemViewModel()
        {
            Task = new TaskModel
            {
                Recognition = "OCR", Roi = new List<int>
                {
                    x, y,
                    width, height
                }
            },
            Name = "AppendOCR",
        };
        var job = MaaProcessor.Instance.GetCurrentTasker()?
            .AppendPipeline(taskItemViewModel.Name, taskItemViewModel.ToString());
        if (job != null && job.Wait() == MaaJobStatus.Succeeded)
        {
            RecognitionQuery? query =
                JsonConvert.DeserializeObject<RecognitionQuery>(job.QueryRecognitionDetail()?
                    .Detail ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(query?.best?.text))
                result = query.best.text;
        }
        else
        {
            Growls.ErrorGlobal("识别失败！");
        }

        Console.WriteLine($"识别结果: {result}");
        return result;
    }

    public static string ReadTextFromMAASyncContext(in IMaaContext syncContext, IMaaImageBuffer image, int x, int y,
        int width, int height)
    {
        string result = string.Empty;
        TaskItemViewModel taskItemViewModel = new TaskItemViewModel()
        {
            Task = new TaskModel
            {
                Recognition = "OCR", Roi = new List<int>
                {
                    x, y,
                    width, height
                }
            },
            Name = "AppendOCR",
        };
        IMaaStringBuffer outDetail = new MaaStringBuffer();
        syncContext.RunRecognition(taskItemViewModel.Name, taskItemViewModel.ToString(), image);
        string? json = outDetail.ToString();
        if (!string.IsNullOrWhiteSpace(json))
        {
            RecognitionQuery? query =
                JsonConvert.DeserializeObject<RecognitionQuery>(json);
            if (query?.best != null && !string.IsNullOrWhiteSpace(query.best?.text))
                result = query.best.text;
        }
        else
        {
            Growls.ErrorGlobal("识别失败！");
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
    //     return Text ?? string.Empty;
    // }
}