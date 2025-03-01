using HandyControl.Controls;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.Helper.Exceptions;
using MFAWPF.Helper.ValueType;
using MFAWPF.ViewModels.Tool;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;
using LocalizationViewModel = MFAWPF.ViewModels.Tool.LocalizationViewModel;
using MFATask = MFAWPF.Helper.ValueType.MFATask;

namespace MFAWPF.Extensions;

public static class MFAExtensions
{
    public static Dictionary<TKey, TaskModel> MergeTaskModels<TKey>(
        this IEnumerable<KeyValuePair<TKey, TaskModel>>? taskModels,
        IEnumerable<KeyValuePair<TKey, TaskModel>>? additionalModels) where TKey : notnull
    {
        if (additionalModels == null)
            return taskModels?.ToDictionary() ?? new Dictionary<TKey, TaskModel>();
        return taskModels?
                .Concat(additionalModels)
                .GroupBy(pair => pair.Key)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var mergedModel = group.First().Value;
                        foreach (var taskModel in group.Skip(1))
                        {
                            mergedModel.Merge(taskModel.Value);
                        }

                        return mergedModel;
                    }
                )
            ?? new Dictionary<TKey, TaskModel>();
    }

    public static void BindLocalization(this FrameworkElement control,
        string resourceKey,
        DependencyProperty? property = null)
    {
        property ??= TitleElement.TitleProperty;
        var locExtension = new LocExtension(resourceKey);
        locExtension.SetBinding(control, property);
    }

    public static string ToLocalization(this string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        return LocalizeDictionary.Instance.GetLocalizedObject(key, null, null) as string ?? key;
    }

    public static string ToLocalizationFormatted(this string? key, params string[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        var formatArgs = args.Select(a => a.ToLocalization()).ToArray();

        var content = string.Empty;
        try
        {
            content = Regex.Unescape(
                key.ToLocalization().FormatWith(formatArgs));
        }
        catch
        {
            content = key.ToLocalization().FormatWith(formatArgs);
        }

        return content;
    }

    public static string FormatWith(this string format, params object[] args)
    {
        return string.Format(format, args);
    }

    public static void AddRange<T>(this ObservableCollection<T>? collection, IEnumerable<T>? items)
    {
        if (collection == null || items == null)
            return;
        foreach (var item in items)
            collection.Add(item);
    }

#nullable enable
    public static bool IsTrue(this bool? value)
    {
        return value == true;
    }

    public static bool IsFalse(this bool? value)
    {
        return value == false;
    }

    public static bool IsHit(
        this RecognitionDetail detail)
    {
        if (detail is null || detail.HitBox.IsDefaultHitBox())
            return false;
        return true;
    }

    private static bool IsDefaultHitBox(this IMaaRectBuffer hitBox)
    {
        return hitBox is null or { X: 0, Y: 0, Width: 0, Height: 0 };
    }
    
    public static MaaTaskJob AppendTask(this IMaaTasker maaTasker, TaskItemViewModel task)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        return maaTasker.AppendTask(task.Name, task.ToString());
    }
    public static MaaTaskJob AppendTask(this IMaaTasker maaTasker, TaskModel taskModel)
    {
        return maaTasker.AppendTask(new TaskItemViewModel
        {
            Name = taskModel.Name,
            Task = taskModel,
        });
    }

    public static void Click(this IMaaTasker maaTasker, int x, int y)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.Click(x, y).Wait();
    }

    public static void Swipe(this IMaaTasker maaTasker, int x1, int y1, int x2, int y2, int duration)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.Swipe(x1, y1, x2, y2, duration).Wait();
    }

    public static void TouchDown(this IMaaTasker maaTasker, int contact, int x, int y, int pressure)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.TouchDown(contact, x, y, pressure).Wait();
    }

    public static void TouchMove(this IMaaTasker maaTasker, int contact, int x, int y, int pressure)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.TouchMove(contact, x, y, pressure).Wait();

    }
    public static void TouchUp(this IMaaTasker maaTasker, int contact)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.TouchUp(contact).Wait();
    }

    public static void PressKey(this IMaaTasker maaTasker, int key)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.PressKey(key).Wait();
    }
    public static void InputText(this IMaaTasker maaTasker, string text)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.InputText(text).Wait();
    }

    public static void Screencap(this IMaaTasker maaTasker)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.Screencap().Wait();
    }

    public static bool GetCachedImage(this IMaaTasker maaTasker, IMaaImageBuffer imageBuffer)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        return maaTasker.Controller.GetCachedImage(imageBuffer);
    }

    public static void StartApp(this IMaaTasker maaTasker, string intent)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.StartApp(intent).Wait();
    }

    public static void StopApp(this IMaaTasker maaTasker, string intent)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        maaTasker.Controller.StopApp(intent).Wait();
    }
    //
    public static void Click(this IMaaContext maaContext, int x, int y)
    {
        maaContext.Tasker.Click(x, y);
    }

    public static void Swipe(this IMaaContext maaContext, int x1, int y1, int x2, int y2, int duration)
    {
        maaContext.Tasker.Swipe(x1, y1, x2, y2, duration);
    }

    public static void TouchDown(this IMaaContext maaContext, int contact, int x, int y, int pressure)
    {
        maaContext.Tasker.TouchDown(contact, x, y, pressure);
    }

    public static void TouchMove(this IMaaContext maaContext, int contact, int x, int y, int pressure)
    {
        maaContext.Tasker.TouchMove(contact, x, y, pressure);

    }
    public static void SmoothTouchMove(this IMaaContext maaContext, int contact, int startX, int startY, int endX, int endY, int durationMs, int steps = 1)
    {
        if (steps <= 0)
        {
            throw new ArgumentException("步数必须大于0", nameof(steps));
        }

        if (durationMs <= 0)
        {
            throw new ArgumentException("持续时间必须大于0", nameof(durationMs));
        }

        var xStep = (endX - startX) / steps;
        var yStep = (endY - startY) / steps;

        for (var i = 0; i < steps; i++)
        {
            var currentX = startX + i * xStep;
            var currentY = startY + i * yStep;
            maaContext.TouchMove(contact, currentX, currentY, 100);
            int sleepTime = durationMs / steps;
            Thread.Sleep(sleepTime);
        }
    }
    public static void TouchUp(this IMaaContext maaContext, int contact)
    {
        maaContext.Tasker.TouchUp(contact);
    }

    public static void PressKey(this IMaaContext maaContext, int key)
    {
        maaContext.Tasker.PressKey(key);
    }
    public static void InputText(this IMaaContext maaContext, string text)
    {
        maaContext.Tasker.InputText(text);
    }

    public static void Screencap(this IMaaContext maaContext)
    {
        maaContext.Tasker.Screencap();
    }

    public static bool GetCachedImage(this IMaaContext maaContext, IMaaImageBuffer imageBuffer)
    {
        return maaContext.Tasker.GetCachedImage(imageBuffer);
    }
    public static IMaaImageBuffer GetImage(this IMaaContext maaContext)
    {
        maaContext.Screencap();
        IMaaImageBuffer imageBuffer = new MaaImageBuffer();
        if (!maaContext.GetCachedImage(imageBuffer))
            return null;
        return imageBuffer;
    }

    public static IMaaImageBuffer GetImage(this IMaaContext maaContext, ref IMaaImageBuffer buffer)
    {
        maaContext.Screencap();
        if (!maaContext.GetCachedImage(buffer))
            return null;
        return buffer;
    }
    public static void StartApp(this IMaaContext maaContext, string intent)
    {
        maaContext.Tasker.StartApp(intent);
    }

    public static void StopApp(this IMaaContext maaContext, string intent)
    {
        maaContext.Tasker.StopApp(intent);
    }

    public static bool TemplateMatch(this IMaaTasker maaTasker, string template, double threshold = 0.8D, int x = 0, int y = 0, int w = 0, int h = 0)
    {
        var job = maaTasker.AppendTask(new TaskModel
        {
            Template = [template],
            Recognition = "TemplateMatch",
            Threshold = threshold,
            Roi = new[]
            {
                x,
                y,
                w,
                h
            }
        });
        if (job.WaitFor(MaaJobStatus.Succeeded) == null)
            return false;
        return job.QueryRecognitionDetail().IsHit();
    }

    public static bool OCR(this IMaaTasker maaTasker, string text, int x = 0, int y = 0, int w = 0, int h = 0)
    {
        var job = maaTasker.AppendTask(new TaskModel
        {
            Expected = [text],
            Recognition = "OCR",
            Roi = new[]
            {
                x,
                y,
                w,
                h
            }
        });
        if (job.WaitFor(MaaJobStatus.Succeeded) == null)
            return false;
        return job.QueryRecognitionDetail().IsHit();
    }

    public static bool TemplateMatch(this IMaaContext maaContext, string template, IMaaImageBuffer imageBuffer, out RecognitionDetail detail, double threshold = 0.8D, int x = 0, int y = 0, int w = 0, int h = 0, bool greenmask = false)
    {
        detail = maaContext.RunRecognition(new TaskModel
        {
            Template = [template],
            GreenMask = greenmask,
            Recognition = "TemplateMatch",
            Threshold = threshold,
            OrderBy = "Score",
            Roi = new[]
            {
                x,
                y,
                w,
                h
            },
        }, imageBuffer);
        LoggerService.LogInfo(detail.Detail);
        LoggerService.LogInfo($"TemplateMatch: {template} ,Hit: {detail.IsHit()}");
        return detail.IsHit();
    }

    public static bool OCR(this IMaaContext maaContext, string text, IMaaImageBuffer imageBuffer, out RecognitionDetail detail, int x = 0, int y = 0, int w = 0, int h = 0)
    {
        detail = maaContext.RunRecognition(new TaskModel
        {
            Expected = [text],
            Recognition = "OCR",
            Roi = new[]
            {
                x,
                y,
                w,
                h
            },
        }, imageBuffer);
        LoggerService.LogInfo($"OCR: {text} ,Hit: {detail.IsHit()}");
        return detail.IsHit();
    }

    public static RecognitionDetail RunRecognition(this IMaaContext maaContext, TaskModel taskModel, IMaaImageBuffer imageBuffer)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        return maaContext.RunRecognition(taskModel.Name, taskModel.ToJson(), imageBuffer);
    }


    public static string GetText(this IMaaContext maaContext, int x, int y, int w, int h, IMaaImageBuffer imageBuffer)
    {
        if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
        {
            throw new MaaStopException();
        }
        return OCRHelper.ReadTextFromMAAContext(maaContext, imageBuffer, x, y, w, h);
    }

    public static void Push(this Queue<MFATask> queue, MFATask task)
    {
        queue.Enqueue(task);
        MaaProcessor.Instance.OnTaskQueueChanged();
    }

    public static bool Until(
        this Func<bool> action,
        int sleepMilliseconds = 500,
        bool condition = true,
        int maxCount = 12,
        Action? errorAction = null
    )
    {

        int count = 0;
        while (true)
        {
            if (MaaProcessor.Instance.CancellationTokenSource?.IsCancellationRequested == true)
            {
                throw new MaaStopException();
            }

            if (action() == condition)
                break;

            if (++count >= maxCount)
            {
                errorAction?.Invoke();
                throw new MaaErrorHandleException();
            }

            if (sleepMilliseconds >= 0)
                Thread.Sleep(sleepMilliseconds);
        }

        return true;
    }

    public static int ToInt(this string str)
    {
        string numberStr = new string(str.Replace(" ", "").Replace('b', '6').Replace('B', '8')
            .Where(char.IsDigit).ToArray());
        if (int.TryParse(numberStr, out int result))
        {
            return result;
        }
        return 0;
    }
    public static bool ContainsKey(this IEnumerable<LocalizationViewModel> settingViewModels, string key)
    {
        return settingViewModels.Any(vm => vm.ResourceKey == key);
    }
    public static string?[] ToStringArray(this IEnumerable<LocalizationViewModel> settingViewModels)
    {
        return settingViewModels
            .Where(vm => vm.ResourceKey != null)
            .Select(vm => vm.ResourceKey).ToArray();
    }
    public static string ToKeyString(this IEnumerable<LocalizationViewModel> settingViewModels)
    {
        return string.Join(",", settingViewModels
            .Where(vm => vm.ResourceKey != null)
            .Select(vm => vm.ResourceKey));
    }
    public static bool ShouldSwitchButton(this List<MaaInterface.MaaInterfaceOptionCase>? cases, out int yes, out int no)
    {
        yes = -1;
        no = -1;

        if (cases == null || cases.Count != 2)
            return false;

        var yesItem = cases
            .Select((c, index) => new
            {
                c.Name,
                Index = index
            })
            .Where(x => x.Name?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true).ToList();

        var noItem = cases
            .Select((c, index) => new
            {
                c.Name,
                Index = index
            })
            .Where(x => x.Name?.Equals("no", StringComparison.OrdinalIgnoreCase) == true).ToList();

        if (yesItem.Count == 0 || noItem.Count == 0 )
            return false;

        yes = yesItem[0].Index;
        no = noItem[0].Index;

        return true;
    }
    
   
    // public static bool IsDebugMode()
    // {
    //     if (MFAConfiguration.MaaConfig.GetConfig("recording", false) || MFAConfiguration.MaaConfig.GetConfig("save_draw", false) || MFAConfiguration.MaaConfig.GetConfig("show_hit_draw", false)) return true;
    //     return false;
    // }
}
