using System.IO;
using System.Timers;

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
            if (!Directory.Exists(debugPath))
            {
                return;
            }

            var logFiles = Directory.GetFiles(debugPath, "*.log");
            foreach (var logFile in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.Length > MaxSizeInBytes)
                    {
                        // 尝试移动而不是直接删除
                        string backupName = Path.Combine(
                            debugPath, 
                            $"old_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(logFile)}");
                            
                        File.Move(logFile, backupName);
                        LoggerService.LogInfo($"已备份大型日志文件: {logFile} -> {backupName}");
                        
                        // 创建新的日志文件
                        File.Create(logFile).Dispose();
                    }
                }
                catch (IOException)
                {
                    // 文件正在使用，跳过
                    continue;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"处理日志文件失败: {logFile}, 错误: {ex.Message}");
                }
            }

            // 清理旧的备份文件
            var oldFiles = Directory.GetFiles(debugPath, "old_*.log")
                                  .OrderByDescending(f => File.GetCreationTime(f))
                                  .Skip(5); // 保留最近5个备份

            foreach (var oldFile in oldFiles)
            {
                try
                {
                    File.Delete(oldFile);
                    LoggerService.LogInfo($"已删除旧的备份日志文件: {oldFile}");
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"删除旧备份文件失败: {oldFile}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"清理日志文件时发生错误: {ex.Message}");
        }
    }
}
