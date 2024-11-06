using System.IO;

namespace MFAWPF.Utils;

public static class LogCleaner
{
    public static void CleanupLargeDebugLogs(long maxSizeInBytes = 5 * 1024 * 1024)
    {
        try
        {
            string debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug");
            if (!Directory.Exists(debugPath))
            {
                return;
            }

            var logFiles = Directory.GetFiles(debugPath, "*.log");
            foreach (var logFile in logFiles)
            {
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.Length > maxSizeInBytes)
                {
                    try
                    {
                        File.Delete(logFile);
                        LoggerService.LogInfo($"已清理大型日志文件: {logFile}");
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError($"清理日志文件失败: {logFile}, 错误: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"清理日志文件时发生错误: {ex.Message}");
        }
    }
}
