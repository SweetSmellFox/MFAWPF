using NLog;
using System.IO;
using NLog.Config;

namespace MFAWPF.Utils;

public static class LoggerService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string ArchivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug", "archive");

    static LoggerService()
    {
        if (!Directory.Exists(ArchivePath))
        {
            Directory.CreateDirectory(ArchivePath);
        }

        // 使用 XmlLoggingConfiguration 替代 XmlLoaderAccessor
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config");
        if (File.Exists(configPath))
        {
            LogManager.Configuration = new XmlLoggingConfiguration(configPath);
        }
    }

    public static void LogInfo(string message)
    {
        Logger.Info(message);
    }

    public static void LogError(object? e)
    {
        Logger.Error(e?.ToString() ?? string.Empty);
    }

    public static void LogError(string message)
    {
        Logger.Error(message);
    }

    public static void LogWarning(string message)
    {
        Logger.Warn(message);
    }
}