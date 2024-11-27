using HandyControl.Tools.Extension;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using WPFLocalizeExtension.Engine;


namespace MFAWPF.Utils;

public static class LanguageManager
{
    public static event EventHandler? LanguageChanged;

    public static void ChangeLanguage(CultureInfo newCulture)
    {
        // 设置应用程序的文化
        LocalizeDictionary.Instance.Culture = newCulture;

        _localizedStrings = newCulture.Name.ToLower() == "zh-cn" ? _cn : _en;

        // 触发语言变化事件
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    // 存储语言的字典
    private static Dictionary<string, string> _cn = new();
    private static Dictionary<string, string> _en = new();
    private static Dictionary<string, string> _localizedStrings = new();
    public static void Initialize()
    {
        Console.WriteLine("Initializing LanguageManager...");
        LoadLanguage("zh-cn", ref _cn);
        LoadLanguage("en-us", ref _en);
    }

    // 初始化时加载语言文件
    public static void LoadLanguage(string languageName, ref Dictionary<string, string> localizedStrings)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang", $"{languageName}.json");

        if (File.Exists(filePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var parsedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

                if (parsedData != null)
                {
                    localizedStrings = parsedData;
                }

            }
            catch (Exception ex)
            {
                // 处理读取文件时的错误
                Console.WriteLine($"Error loading localization file: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Localization file for language '{languageName}' not found.");
        }
    }
    
    private static Dictionary<string, string> GetLocalizedStrings()
    {
        if (_localizedStrings.Count == 0)
            return _cn;
        return _localizedStrings;
    }
    
    public static string GetLocalizedString(string key)
    {
        return GetLocalizedStrings().GetValueOrDefault(key, key);
    }
}
