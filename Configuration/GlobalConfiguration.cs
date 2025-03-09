using MFAWPF.Helper;
using System.Configuration;


namespace MFAWPF.Configuration;

public static class GlobalConfiguration
{
    private static System.Configuration.Configuration GetConfigurationInstance()
    {
        return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
    }

    public static void SetValue(string key, string value)
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

    public static string GetValue(string key, string defaultValue = "")
    {
        var config = GetConfigurationInstance();
        return config.AppSettings.Settings[key]?.Value ?? defaultValue;
    }

    public static string GetTimer(int i, string defaultValue)
    {
        return GetValue($"Timer.Timer{i + 1}", defaultValue);
    }

    public static void SetTimer(int i, string value)
    {
        SetValue($"Timer.Timer{i + 1}", value);
    }

    public static string GetTimerHour(int i, string defaultValue)
    {

        return GetValue($"Timer.Timer{i + 1}Hour", defaultValue);
    }

    public static void SetTimerHour(int i, string value)
    {
        SetValue($"Timer.Timer{i + 1}Hour", value);
    }

    public static string GetTimerMin(int i, string defaultValue)
    {
        return GetValue($"Timer.Timer{i + 1}Min", defaultValue);
    }

    public static void SetTimerMin(int i, string value)
    {
        SetValue($"Timer.Timer{i + 1}Min", value);
    }

    public static string GetTimerConfig(int i, string defaultValue)
    {
        return GetValue($"Timer.Timer{i + 1}.Config", defaultValue);
    }

    public static void SetTimerConfig(int i, string value)
    {
        SetValue($"Timer.Timer{i + 1}.Config", value);
    }
}
