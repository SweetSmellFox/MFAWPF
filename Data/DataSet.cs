using System;
using System.Collections.Generic;
using MFAWPF.Utils;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Data
{
    public class DataSet
    {
        public static Dictionary<string, object>? Data = new();

        public static void SetData(string key, object? value)
        {
            if (Data == null || value == null) return;
            Data[key] = value; // 如果 key 不存在，将自动添加条目；如果存在，将更新值

            JSONHelper.WriteToConfigJsonFile("config", Data);
        }


        public static bool TryGetData<T>(string key, out T? value)
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
                    Console.WriteLine("在进行类型转换时发生错误！");
                    LoggerService.LogError("在进行类型转换时发生错误!");
                    LoggerService.LogError(e);
                }
            }

            value = default;
            return false;
        }

        public static T? GetData<T>(string key, T defaultValue)
        {
            if (Data?.TryGetValue(key, out var data) == true)
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
                    Console.WriteLine("在进行类型转换时发生错误！");
                    LoggerService.LogError("在进行类型转换时发生错误!");
                    LoggerService.LogError(e);
                }
            }

            return defaultValue;
        }
    }
}