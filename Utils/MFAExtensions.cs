using System.Windows;
using HandyControl.Controls;
using WPFLocalizeExtension.Deprecated.Extensions;
using WPFLocalizeExtension.Extensions;

namespace MFAWPF.Utils;

public static class MFAExtensions
{
    public static Dictionary<TKey, TaskModel> MergeTaskModels<TKey>(
        this IEnumerable<KeyValuePair<TKey, TaskModel>> taskModels,
        IEnumerable<KeyValuePair<TKey, TaskModel>> additionalModels)
    {
        if (additionalModels == null)
            return taskModels.ToDictionary();
        return taskModels
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
            );
    }

    public static void BindLocalization(this UIElement control, string resourceKey, DependencyProperty property = null)
    {
        if (property == null)
            property = InfoElement.TitleProperty;
        var locExtension = new LocTextExtension(resourceKey);
        locExtension.SetBinding(control, property);
    }

    public static string GetLocalizationString(this string key)
    {
        return LocExtension.GetLocalizedValue<string>(key);
    }
    public static string GetLocalizedFormattedString(this string key, params object[] args)
    {
        string localizedString = LocTextExtension.GetLocalizedValue<string>(key) as string ?? key;
        return string.Format(localizedString, args);
    }
    
    public static string FormatWith(this string format, params object?[] args)
    {
        return string.Format(format, args);
    }
}