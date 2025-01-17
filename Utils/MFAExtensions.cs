﻿using System.Collections.ObjectModel;
using System.Windows;
using HandyControl.Controls;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MFAWPF.ViewModels;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;

namespace MFAWPF.Utils;

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

    public static string GetLocalizationString(this string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        return LocalizeDictionary.Instance.GetLocalizedObject(key, null, null) as string ?? key;
    }

    public static string GetLocalizedFormattedString(this string? key, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        string localizedString = LocalizeDictionary.Instance.GetLocalizedObject(key, null, null) as string ?? key;
        return string.Format(localizedString, args);
    }

    public static string FormatWith(this string format, params object?[] args)
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

    public static bool IsTrue(this bool? value)
    {
        return value == true;
    }

    public static bool IsFalse(this bool? value)
    {
        return value == false;
    }

    public static bool IsHit(
        this RecognitionDetail? detail)
    {
        if (detail is null || detail.HitBox.IsDefaultHitBox())
            return false;
        return true;
    }

    private static bool IsDefaultHitBox(this IMaaRectBuffer? hitBox)
    {
        return hitBox is null or { X: 0, Y: 0, Width: 0, Height: 0 };
    }
    public static MaaTaskJob AppendPipeline(this IMaaTasker maaTasker, TaskItemViewModel task)
    {
        return maaTasker.AppendPipeline(task.Name, task.ToString());
    }
    public static MaaTaskJob AppendPipeline(this IMaaTasker maaTasker, TaskModel taskModel)
    {
        return maaTasker.AppendPipeline(new TaskItemViewModel
        {
            Name = taskModel.Name,
            Task = taskModel,
        });
    }

    public static void Click(this IMaaTasker maaTasker, int x, int y)
    {
        maaTasker.Controller.Click(x, y).Wait();
    }

    public static void Swipe(this IMaaTasker maaTasker, int x1, int y1, int x2, int y2, int duration)
    {
        maaTasker.Controller.Swipe(x1, y1, x2, y2, duration).Wait();
    }

    public static void TouchDown(this IMaaTasker maaTasker, int contact, int x, int y, int pressure)
    {
        maaTasker.Controller.TouchDown(contact, x, y, pressure).Wait();
    }

    public static void TouchMove(this IMaaTasker maaTasker, int contact, int x, int y, int pressure)
    {
        maaTasker.Controller.TouchMove(contact, x, y, pressure).Wait();

    }
    public static void TouchUp(this IMaaTasker maaTasker, int contact)
    {
        maaTasker.Controller.TouchUp(contact).Wait();
    }

    public static void PressKey(this IMaaTasker maaTasker, int key)
    {
        maaTasker.Controller.PressKey(key).Wait();
    }
    public static void InputText(this IMaaTasker maaTasker, string text)
    {
        maaTasker.Controller.InputText(text).Wait();
    }

    public static void Screencap(this IMaaTasker maaTasker)
    {
        maaTasker.Controller.Screencap().Wait();
    }

    public static bool GetCachedImage(this IMaaTasker maaTasker, IMaaImageBuffer imageBuffer)
    {
        return maaTasker.Controller.GetCachedImage(imageBuffer);
    }

    public static void StartApp(this IMaaTasker maaTasker, string intent)
    {
        maaTasker.Controller.StartApp(intent).Wait();
    }

    public static void StopApp(this IMaaTasker maaTasker, string intent)
    {
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
    public static IMaaImageBuffer? GetImage(this IMaaContext maaContext)
    {
        maaContext.Tasker.Screencap();
        IMaaImageBuffer imageBuffer = new MaaImageBuffer();
        if (!maaContext.Tasker.GetCachedImage(imageBuffer))
            return null;
        return imageBuffer;
    }

    public static IMaaImageBuffer? GetImage(this IMaaContext maaContext, ref IMaaImageBuffer buffer)
    {
        maaContext.Tasker.Screencap();
        if (!maaContext.Tasker.GetCachedImage(buffer))
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
        var job = maaTasker.AppendPipeline(new TaskModel
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
        var job = maaTasker.AppendPipeline(new TaskModel
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

    public static RecognitionDetail? RunRecognition(this IMaaContext maaContext, TaskModel taskModel, IMaaImageBuffer imageBuffer)
    {
        return maaContext.RunRecognition(taskModel.Name, taskModel.ToJson(), imageBuffer);
    }


    public static string GetText(this IMaaContext maaContext, int x, int y, int w, int h, IMaaImageBuffer imageBuffer)
    {
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
        errorAction = null;
        try
        {
            int count = 0;
            while (true)
            {
                if (MaaProcessor.Instance.CancellationTokenSource.IsCancellationRequested)
                {
                    LoggerService.LogInfo("Operation was cancelled.");
                    return false;
                }

                if (action() == condition)
                    break;

                if (++count >= maxCount)
                {
                    errorAction?.Invoke();
                    return false;
                }

                if (sleepMilliseconds >= 0)
                    Thread.Sleep(sleepMilliseconds);
            }
        }
        catch (Exception)
        {
            return false;
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
}
