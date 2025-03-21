﻿using System.IO;
using HandyControl.Tools.Extension;
using Newtonsoft.Json;

namespace MFAWPF.Helper;

public static class JsonHelper
{
    /// <summary>
    /// 将对象序列化为指定的文件名
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="file"></param>
    /// <param name="defaultS"></param>
    /// <param name="show"></param>
    /// <returns></returns>
    ///    //序列化到文件
    public static T ReadFromConfigJsonFile<T>(string file, T defaultS = default, bool show = false, params JsonConverter[] converters)
    {
        // 从文件中读取 JSON 字符串
        string directory = $"{AppContext.BaseDirectory}/config/{file}.json";
        // 将 JSON 字符串转换为对象
        try
        {
            if (!Directory.Exists($"{AppContext.BaseDirectory}/config"))
                Directory.CreateDirectory($"{AppContext.BaseDirectory}/config");
            string jsonString = File.ReadAllText(directory);
            var settings = new JsonSerializerSettings();
            if (converters is { Length: > 0 })
            {
                settings.Converters.AddRange(converters);
            }

            T result = JsonConvert.DeserializeObject<T>(jsonString,settings) ?? defaultS;
            return result;
        }
        catch (Exception e)
        {
            if (show)
                LoggerService.LogError(e);
            return defaultS;
        }
    }


    public static void WriteToConfigJsonFile(string file, object content, params JsonConverter[] converters)
    {
        if (content == null) return;
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        if (converters is { Length: > 0 })
        {
            settings.Converters.AddRange(converters);
        }
        // var dir = Directory.GetCurrentDirectory();
        string jsonString = JsonConvert.SerializeObject(content, settings);
        string directory = $"{AppContext.BaseDirectory}/config/{file}.json";
        if (!Directory.Exists($"{AppContext.BaseDirectory}/config"))
            Directory.CreateDirectory($"{AppContext.BaseDirectory}/config");
        directory = Path.GetFullPath(directory);
        // 将 JSON 字符串写入文件
        File.WriteAllText(directory, jsonString);
    }

    public static T ReadFromJsonFile<T>(string file, T defaultS = default)
    {
        string directory = $"{AppContext.BaseDirectory}/{file}.json";
        try
        {
            if (!Directory.Exists($"{AppContext.BaseDirectory}"))
                Directory.CreateDirectory($"{AppContext.BaseDirectory}");
            string jsonString = File.ReadAllText(directory);
            return JsonConvert.DeserializeObject<T>(jsonString) ?? defaultS;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            LoggerService.LogError(e);
            return defaultS;
        }
    }

    public static void WriteToJsonFile(string file, object content)
    {
        // var dir = Directory.GetCurrentDirectory();
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        string jsonString = JsonConvert.SerializeObject(content, settings);
        string directory = $"{AppContext.BaseDirectory}/{file}.json";
        if (!Directory.Exists($"{AppContext.BaseDirectory}"))
            Directory.CreateDirectory($"{AppContext.BaseDirectory}");
        directory = Path.GetFullPath(directory);
        // 将 JSON 字符串写入文件
        File.WriteAllText(directory, jsonString);
    }

    public static T ReadFromJsonFilePath<T>(string path, string file, T defaultS = default, Action action = null,
        params JsonConverter[] converters)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = AppContext.BaseDirectory;
        string directory = $"{path}/{file}.json";
        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string jsonString = File.ReadAllText(directory);
            var settings = new JsonSerializerSettings();
            if (converters is { Length: > 0 })
            {
                settings.Converters.AddRange(converters);
            }

            return JsonConvert.DeserializeObject<T>(jsonString, settings) ?? defaultS;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            action?.Invoke();
            return defaultS;
        }
    }

    public static void WriteToJsonFilePath(string path, string file, object content, params JsonConverter[] converters)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = AppContext.BaseDirectory;
        // var dir = Directory.GetCurrentDirectory();
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        if (converters is { Length: > 0 })
        {
            settings.Converters.AddRange(converters);
        }

        string jsonString = JsonConvert.SerializeObject(content, settings);
        string directory = $"{path}/{file}.json";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        directory = Path.GetFullPath(directory);
        // 将 JSON 字符串写入文件
        File.WriteAllText(directory, jsonString);
    }
}