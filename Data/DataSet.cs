using System;
using System.Collections.Generic;
using MFAWPF.Utils;

namespace MFAWPF.Data
{
    public class DataSet
    {
        public static Dictionary<string, object>? Data = new();

        public static void SetData(string key, object value)
        {
            if (Data == null) return;
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
                    if (data is long && typeof(T) == typeof(int))
                    {
                        value = (T)(object)Convert.ToInt32((long)data); // Safe conversion
                        return true;
                    }

                    if (data is T t)
                    {
                        value = t;
                        return true;
                    }
                }
                catch
                {
                    Console.WriteLine("在进行类型转换时发生错误！");
                    // Handle conversion errors
                }
            }

            value = default;
            return false;
        }

        public static T GetData<T>(string key, T defaultValue)
        {
            if (Data?.TryGetValue(key, out var data) == true)
            {
                try
                {
                    // Handle conversion between int and long
                    if (data is long && typeof(T) == typeof(int))
                    {
                        return (T)(object)Convert.ToInt32((long)data); // Safe conversion
                    }

                    if (data is T t)
                    {
                        return t;
                    }
                }
                catch
                {
                    Console.WriteLine("在进行类型转换时发生错误！");
                    // Handle conversion errors
                }
            }

            return defaultValue;
        }
    }
}