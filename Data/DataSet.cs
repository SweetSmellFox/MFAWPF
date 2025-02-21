using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.Views;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Data

;

public static class DataSet
{
    public static Dictionary<string, object> Data = new();
    public static Dictionary<string, object> MaaConfig = new();


    public static void SetConfig(this Dictionary<string, object>? config, string key, object? value)
    {
        if (config == null || value == null) return;
        config[key] = value;
        if (key == "LangIndex" && MainWindow.ViewModel is not null)
            SettingsView.ViewModel.LanguageIndex = Convert.ToInt32(value);
        var fileName = config == Data ? "config" : "maa_option";
        if (config == MaaConfig)
            MainWindow.ViewModel.IsDebugMode = MFAExtensions.IsDebugMode();
        JsonHelper.WriteToConfigJsonFile(fileName, config, new MaaInterfaceSelectOptionConverter(false));
    }

    public static T GetConfig<T>(this Dictionary<string, object> Config, string key, T defaultValue)
    {
        if (Config?.TryGetValue(key, out var data) == true)
        {
            try
            {
                if (data is long longValue && typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(longValue);
                }

                if (data is JArray jArray)
                {
                    // 将 JArray 转换为目标类型
                    return jArray.ToObject<T>();
                }

                if (data is T t)
                {
                    return t;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogError("在进行类型转换时发生错误!");
                LoggerService.LogError(e);
            }
        }

        return defaultValue;
    }

    public static void SetData(string key, object value)
    {
        Data.SetConfig(key, value);
    }

    public static bool TryGetData<T>(string key, out T value)
    {
        if (Data?.TryGetValue(key, out var data) == true)
        {
            try
            {
                // Handle conversion between int and long
                if (data is long longValue && typeof(T) == typeof(int))
                {
                    value = (T)(object)Convert.ToInt32(longValue); // Safe conversion
                    return true;
                }

                if (data is JArray jArray)
                {
                    // 将 JArray 转换为目标类型
                    value = jArray.ToObject<T>();
                    return true;
                }

                if (data is T t)
                {
                    value = t;
                    return true;
                }
            }
            catch (Exception e)
            {
                LoggerService.LogError("在进行类型转换时发生错误!");
                LoggerService.LogError(e);
            }
        }

        value = default;
        return false;
    }

    public static T GetData<T>(string key, T defaultValue)
    {
        return Data.GetConfig(key, defaultValue);
    }
}
