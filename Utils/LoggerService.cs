using NLog;

namespace MFAWPF.Utils;

public static class LoggerService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


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