using Serilog;

namespace MFAWPF.Utils;

public static class LoggerService
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .WriteTo.File(
            $"logs/log-{DateTime.Now.ToString("yyyy-MM-dd")}.txt",
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

    public static void LogInfo(string message)
    {
        Logger.Information(message);
        Console.WriteLine("[INFO]" + message);
    }
    
    public static void LogInfo(object message)
    {
        Logger.Information(message.ToString());
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
        Logger.Warning(message);
        Console.WriteLine("[WARN]" + message);
    }
}
