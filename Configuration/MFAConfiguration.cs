using CommunityToolkit.Mvvm.ComponentModel;
using MaaFramework.Binding;
using MFAWPF.Helper;
using MFAWPF.Helper.Converters;
using MFAWPF.Views;
using MFAWPF.Views.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Windows.Data;

namespace MFAWPF.Data

;

public static class MFAConfiguration
{
    public static Dictionary<string, object> Data = new();
    public static Dictionary<string, object> MaaConfig = new();
    public static ObservableCollection<MFAConfig> Configs = new();

    public static int ConfigIndex { get; set; } = 0;
    public static string ConfigName { get; set; } = "Default";

    public static string GetCurrentConfiguration() => ConfigName;

    public static string GetActualConfiguration()
    {
        if (ConfigName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return "config";
        return GetCurrentConfiguration();
    }

    public static void LoadConfig()
    {
        LoggerService.LogInfo("Loading configuration file...");
        ConfigName = GlobalConfiguration.GetConfiguration("DefaultConfig", ConfigName);

        var configPath = Path.Combine(AppContext.BaseDirectory, "config");
        var defaultConfigPath = Path.Combine(configPath, "config.json");
        if (!Directory.Exists(configPath))
            Directory.CreateDirectory(configPath);
        if (!File.Exists(defaultConfigPath))
            File.WriteAllText(defaultConfigPath, "{}");
        foreach (var file in Directory.GetFiles(configPath))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".json") && !fileName.Contains("maa", StringComparison.OrdinalIgnoreCase))
            {
                var name = fileName.Replace(".json", "");
                var config = JsonHelper.ReadFromConfigJsonFile(name, new Dictionary<string, object>());
                Configs.Add(new MFAConfig
                {
                    Name = name.Equals("config", StringComparison.OrdinalIgnoreCase) ? "Default" : name,
                    FileName = fileName,
                    Config = config
                });
            }
        }


        Data = Configs.FirstOrDefault(c
                => !string.IsNullOrWhiteSpace(c.Name)
                && c.Name.Equals(ConfigName, StringComparison.OrdinalIgnoreCase), null)?.Config
            ?? new Dictionary<string, object>();

        ConfigIndex = Configs.ToList().FindIndex(c => !string.IsNullOrWhiteSpace(c.Name)
            && c.Name.Equals(ConfigName, StringComparison.OrdinalIgnoreCase));
        MaaConfig = JsonHelper.ReadFromConfigJsonFile("maa_option", new Dictionary<string, object>());
    }

    public static void SetDefaultConfig(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        GlobalConfiguration.SetConfiguration("DefaultConfig", name);
    }

    public static MFAConfig AddNewConfig(string name)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config");
        var newConfigPath = Path.Combine(configPath, $"{name}.json");
        var newConfig = new MFAConfig
        {
            Name = name.Equals("config", StringComparison.OrdinalIgnoreCase) ? "Default" : name,
            FileName = name,
            Config = JsonHelper.ReadFromConfigJsonFile(name, new Dictionary<string, object>())
        };
        Configs.Add(newConfig);
        return newConfig;
    }

    public static void DeleteConfig(string name)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config");
        var deleteConfig = Path.Combine(configPath, $"{name}.json");
        if (File.Exists(deleteConfig))
        {
            File.Delete(deleteConfig);
        }
    }

    public static void SetConfig(this Dictionary<string, object>? config, string key, object? value)
    {
        if (config == null || value == null) return;
        config[key] = value;
        var fileName = config == Data ? GetActualConfiguration() : "maa_option";
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

    public static void SetConfiguration(string key, object? value)
    {
        Data.SetConfig(key, value);
    }

    public static bool TryGetConfiguration<T>(string key, out T value)
    {
        if (Data.TryGetValue(key, out var data))
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

    public static T GetConfiguration<T>(
        string key,
        T defaultValue,
        params JsonConverter[] valueConverters) where T : struct, Enum
    {
        return GetConfiguration(key, defaultValue, defaultValue, valueConverters);
    }
    
    public static T GetConfiguration<T>(string key, T defaultValue, T? noValue = default, params JsonConverter[] valueConverters)
    {
        if (Data.TryGetValue(key, out var data))
        {
            try
            {
                var settings = new JsonSerializerSettings();
                foreach (var converter in valueConverters)
                {
                    settings.Converters.Add(converter);
                }
                var result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data), settings) ?? defaultValue;
                if (result.Equals(noValue))
                    return defaultValue;
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data), settings) ?? defaultValue;
            }
            catch (Exception e)
            {
                LoggerService.LogError($"类型转换失败: {e.Message}");
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public static bool TryGetConfiguration<T>(string key, out T output, params JsonConverter[] valueConverters)
    {
        if (Data.TryGetValue(key, out var data))
        {
            try
            {
                var settings = new JsonSerializerSettings();
                foreach (var converter in valueConverters)
                {
                    settings.Converters.Add(converter);
                }
                output = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data), settings) ?? default;
                return true;
            }
            catch (Exception e)
            {
                LoggerService.LogError($"类型转换失败: {e.Message}");
            }
        }
        output = default;
        return false;
    }

    public static T GetConfiguration<T>(string key, T defaultValue)
    {
        return Data.GetConfig(key, defaultValue);
    }
}
