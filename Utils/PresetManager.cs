using HandyControl.Controls;
using System.IO;
using MFAWPF.Utils.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Utils
{
    public class PresetManager
    {
        private const string PRESET_FOLDER = "presets";
        private readonly string _presetPath;
        public string GetPresetPath()
        {
            return _presetPath;
        }
        public PresetManager()
        {
            LoggerService.LogInfo("初始化 PresetManager");
            _presetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PRESET_FOLDER);
            LoggerService.LogInfo($"预设路径: {_presetPath}");
            
            if (!Directory.Exists(_presetPath))
            {
                try
                {
                    Directory.CreateDirectory(_presetPath);
                    LoggerService.LogInfo($"创建预设目录: {_presetPath}");
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"创建预设目录失败: {ex.Message}");
                    throw;
                }
            }
            else
            {
                LoggerService.LogInfo("预设目录已存在");
            }
        }

        public async Task SavePreset(string name)
        {
            try
            {
                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
                LoggerService.LogInfo($"源文件路径: {sourcePath}");
                
                // 检查源文件
                if (!File.Exists(sourcePath))
                {
                    string errorMsg = $"源配置文件不存在: {sourcePath}";
                    LoggerService.LogError(errorMsg);
                    Growl.Error(errorMsg);
                    return;
                }

                // 检查源文件是否可读
                try
                {
                    using (File.OpenRead(sourcePath)) { }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"无法读取源文件: {ex.Message}";
                    LoggerService.LogError(errorMsg);
                    Growl.Error(errorMsg);
                    return;
                }

                // 检查并创建预设目录
                if (!Directory.Exists(_presetPath))
                {
                    try
                    {
                        Directory.CreateDirectory(_presetPath);
                        LoggerService.LogInfo($"创建预设目录: {_presetPath}");
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"创建预设目录失败: {ex.Message}";
                        LoggerService.LogError(errorMsg);
                        Growl.Error(errorMsg);
                        return;
                    }
                }

                string destPath = Path.Combine(_presetPath, $"{name}.json");
                LoggerService.LogInfo($"目标文件路径: {destPath}");

                // 检查目标文件是否被占用
                if (File.Exists(destPath))
                {
                    try
                    {
                        using (File.OpenWrite(destPath)) { }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"目标文件被占用或无法写入: {ex.Message}";
                        LoggerService.LogError(errorMsg);
                        Growl.Error(errorMsg);
                        return;
                    }
                }

                File.Copy(sourcePath, destPath, overwrite: true);
                
                string successMsg = $"预设已保存到: {destPath}";
                LoggerService.LogInfo(successMsg);
                Growl.Success($"预设 {name} 保存成功");
            }
            catch (Exception ex)
            {
                string errorMsg = $"保存预设失败: {ex.Message}";
                LoggerService.LogError(errorMsg);
                Growl.Error(errorMsg);
                throw;
            }
        }

        public async Task<MaaInterface?> LoadPreset(string name)
        {
            try
            {
                string presetPath = Path.Combine(_presetPath, $"{name}.json");
                LoggerService.LogInfo($"开始加载预设: {presetPath}");
                
                // 先读取预设文件内容
                string presetContent = await File.ReadAllTextAsync(presetPath);
                LoggerService.LogInfo("已读取预设文件内容");
                
                // 将内容写入到 config.json
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
                LoggerService.LogInfo($"写入预设到配置文件: {configPath}");
                await File.WriteAllTextAsync(configPath, presetContent);
                
                // 从预设内容直接反序列化 MaaInterface 实例
                var maaInterface = JsonConvert.DeserializeObject<MaaInterface>(presetContent);
                LoggerService.LogInfo("预设加载成功");
                
                return maaInterface;
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"加载预设失败: {ex.Message}");
                Growl.Error($"加载预设失败: {ex.Message}");
                return null;
            }
        }

        public List<string> GetPresetNames()
        {
            return Directory.GetFiles(_presetPath, "*.json")
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToList();
        }

        public void DeletePreset(string name)
        {
            string filePath = Path.Combine(_presetPath, $"{name}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Growl.Success($"预设 {name} 已删除");
            }
            else
            {
                Growl.Warning($"预设 {name} 不存在");
            }
        }
    }
}