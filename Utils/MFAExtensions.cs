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

    public static MaaJob Click(this IMaaContext maaContext, int x, int y)
    {
        return maaContext.Tasker.Controller.Click(x, y);
    }

    public static MaaJob Swipe(this IMaaContext maaContext, int x1, int y1, int x2, int y2, int duration)
    {
        return maaContext.Tasker.Controller.Swipe(x1, y1, x2, y2, duration);
    }

    public static MaaJob TouchDown(this IMaaContext maaContext, int contact, int x, int y, int pressure)
    {
        return maaContext.Tasker.Controller.TouchDown(contact, x, y, pressure);
    }

    public static MaaJob TouchMove(this IMaaContext maaContext, int contact, int x, int y, int pressure)
    {
        return maaContext.Tasker.Controller.TouchMove(contact, x, y, pressure);

    }
    public static MaaJob TouchUp(this IMaaContext maaContext, int contact)
    {
        return maaContext.Tasker.Controller.TouchUp(contact);
    }

    public static MaaJob PressKey(this IMaaContext maaContext, int key)
    {
        return maaContext.Tasker.Controller.PressKey(key);
    }
    public static MaaJob InputText(this IMaaContext maaContext, string text)
    {
        return maaContext.Tasker.Controller.InputText(text);
    }
    public static MaaJob Screencap(this IMaaContext maaContext)
    {
        return maaContext.Tasker.Controller.Screencap();
    }

    public static bool GetCachedImage(this IMaaContext maaContext, IMaaImageBuffer imageBuffer)
    {
        return maaContext.Tasker.Controller.GetCachedImage(imageBuffer);
    }
    
    public static void StartApp(this IMaaContext maaContext, string intent)
    {
        maaContext.Tasker.Controller.StartApp(intent).Wait();
    }
    
    public static void StopApp(this IMaaContext maaContext, string intent)
    {
        maaContext.Tasker.Controller.StopApp(intent).Wait();
    }
    
    public static bool TemplateMatch(this IMaaContext maaContext, string template, IMaaImageBuffer imageBuffer, double threshold = 0.8D, int x = 0, int y = 0, int w = 0, int h = 0)
    {
        return maaContext.RunRecognition(new TaskModel
        {
            Template = [template],
            Threshold = threshold,
            Roi = new List<int>[x, y, w, h]
        }, imageBuffer).IsHit();
    }

    public static bool OCRMatch(this IMaaContext maaContext, string text, IMaaImageBuffer imageBuffer, int x = 0, int y = 0, int w = 0, int h = 0)
    {
        return maaContext.RunRecognition(new TaskModel
        {
            Expected = [text],
            Roi = new List<int>[x, y, w, h]
        }, imageBuffer).IsHit();
    }

    public static RecognitionDetail? RunRecognition(this IMaaContext maaContext, TaskModel taskModel, IMaaImageBuffer imageBuffer)
    {
        return maaContext.RunRecognition(new TaskItemViewModel
        {
            Task = taskModel,
            Name = taskModel.Name
        }, imageBuffer);
    }


    public static RecognitionDetail? RunRecognition(this IMaaContext maaContext, TaskItemViewModel taskModel, IMaaImageBuffer imageBuffer)
    {
        return maaContext.RunRecognition(taskModel.Name, taskModel.ToString(), imageBuffer);
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
}
