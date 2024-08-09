using NLog;

namespace MFAWPF.Utils;

public class LoggerService
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static LoggerService Logger = new ();
    
    public void LogInfo(string message)
    {
        logger.Info(message);
    }

    public void LogError(string message)
    {
        logger.Error(message);
    }

    public void LogWarning(string message)
    {
        logger.Warn(message);
    }
}