using NLog;

namespace MFAWPF.Utils;

public class LoggerService
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static LoggerService Logger = new ();
    
    public static void LogInfo(string message)
    {
        logger.Info(message);
    }

    public static void LogError(Exception e)
    {
        logger.Error(e.ToString());
    }
    
    public static void LogError(string message)
    {
        logger.Error(message);
    }

    public static void LogWarning(string message)
    {
        logger.Warn(message);
    }
}