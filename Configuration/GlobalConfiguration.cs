using MFAWPF.Helper;
using System.Configuration;

namespace MFAWPF.Data;

public static class GlobalConfiguration
{
    private static Configuration GetConfigurationInstance()
    {
        return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    }

    public static void SetConfiguration(string key, string value)
    {
        var config = GetConfigurationInstance();
        if (config.AppSettings.Settings[key] == null)
        {
            config.AppSettings.Settings.Add(key, value);
        }
        else
        {
            config.AppSettings.Settings[key].Value = value;
        }

        try
        {
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch (ConfigurationErrorsException ex)
        {
            LoggerService.LogError(new InvalidOperationException("Failed to save configuration.", ex));
        }
    }

    public static string GetConfiguration(string key, string defaultValue = "")
    {
        var config = GetConfigurationInstance();
        return config.AppSettings.Settings[key]?.Value ?? defaultValue;
    }

    public static string GetTimer(int i, string defaultValue)
    {
        return GetConfiguration($"Timer.Timer{i + 1}", defaultValue);
    }

    public static void SetTimer(int i, string value)
    {
        SetConfiguration($"Timer.Timer{i + 1}", value);
    }

    public static string GetTimerHour(int i, string defaultValue)
    {

        return GetConfiguration($"Timer.Timer{i + 1}Hour", defaultValue);
    }

    public static void SetTimerHour(int i, string value)
    {
        SetConfiguration($"Timer.Timer{i + 1}Hour", value);
    }

    public static string GetTimerMin(int i, string defaultValue)
    {
        return GetConfiguration($"Timer.Timer{i + 1}Min", defaultValue);
    }

    public static void SetTimerMin(int i, string value)
    {
        SetConfiguration($"Timer.Timer{i + 1}Min", value);
    }

    public static string GetTimerConfig(int i, string defaultValue)
    {
        var result = GetConfiguration($"Timer.Timer{i + 1}.Config", defaultValue);
        LoggerService.LogInfo($"Timer.Timer{i + 1}.Config:{result} , 默认:{defaultValue}");
        return result;
    }

    public static void SetTimerConfig(int i, string value)
    {
        LoggerService.LogInfo($"Timer.Timer{i + 1}.Config设置为:{value}");
        SetConfiguration($"Timer.Timer{i + 1}.Config", value);
    }
}
