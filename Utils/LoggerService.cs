using NLog;
using System.IO;

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

        // 确保NLog配置生效
        var config = new NLog.Config.XmlLoaderAccessor().Load("nlog.config");
        if (config != null)
        {
            LogManager.Configuration = config;
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