using System.IO;
using Timer = System.Timers.Timer;

namespace MFAWPF.Utils;

public static class LogCleaner
{
    private static readonly Timer CleanupTimer;
    private const int CheckIntervalHours = 3; // 每3小时检查一次
    private const long MaxSizeInBytes = 5 * 1024 * 1024; // 5MB

    static LogCleaner()
    {
        CleanupTimer = new Timer(CheckIntervalHours * 60 * 60 * 1000); // 转换为毫秒
        CleanupTimer.Elapsed += (_, _) => CleanupLargeDebugLogs();
        CleanupTimer.AutoReset = true;
        CleanupTimer.Start();
    }

    public static void CleanupLargeDebugLogs()
    {
        try
        {
            string debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug");
            string archivePath = Path.Combine(debugPath, "archive");
            
            if (!Directory.Exists(debugPath) || !Directory.Exists(archivePath))
            {
                return;
            }

            // 处理当前目录中的大文件
            var logFiles = Directory.GetFiles(debugPath, "*.log")
                                  .Where(f => !Path.GetFileName(f).StartsWith("old_"));
                              
            foreach (var logFile in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.Length > MaxSizeInBytes)
                    {
                        string archiveName = Path.Combine(
                            archivePath,
                            $"log.{DateTime.Now:yyyy-MM-dd}.txt");

                        // 复制到归档目录
                        File.Copy(logFile, archiveName, true);
                        // 清空原文件
                        File.WriteAllText(logFile, string.Empty);
                        LoggerService.LogInfo($"已归档大型日志文件: {logFile} -> {archiveName}");
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"处理日志文件失败: {logFile}, 错误: {ex.Message}");
                }
            }

            // 清理旧的归档文件
            var archiveFiles = Directory.GetFiles(archivePath, "log.*.txt")
                                      .OrderByDescending(f => File.GetCreationTime(f))
                                      .Skip(2); // 保持与 nlog.config 中的 maxArchiveFiles 一致

            foreach (var oldFile in archiveFiles)
            {
                try
                {
                    File.Delete(oldFile);
                    LoggerService.LogInfo($"已删除旧的归档文件: {oldFile}");
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"删除归档文件失败: {oldFile}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"清理日志文件时发生错误: {ex.Message}");
        }
    }
}
