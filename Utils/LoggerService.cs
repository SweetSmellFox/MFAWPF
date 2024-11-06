using NLog;

namespace MFAWPF.Utils;

public static class LoggerService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string ArchivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug", "archive");

    static LoggerService()
    {
        // 确保存档目录存在
        if (!Directory.Exists(ArchivePath))
        {
            Directory.CreateDirectory(ArchivePath);
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