using NLog;

namespace MFAWPF.Utils;

public static class LoggerService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public static void LogInfo(string message)
    {
        Logger.Info(message);
        Console.WriteLine("[INFO]" + message);
    }

    public static void LogError(object? e)
    {
        Logger.Error(e?.ToString() ?? string.Empty);

        Console.WriteLine("[ERROR]" + e);
    }

    public static void LogError(string message)
    {
        Logger.Error(message);
        Console.WriteLine("[ERROR]" + message);
    }

    public static void LogWarning(string message)
    {
        Logger.Warn(message);
        Console.WriteLine("[WARN]" + message);
    }
}
