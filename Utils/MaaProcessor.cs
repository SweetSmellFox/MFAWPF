using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MaaFramework.Binding.Notification;
using MFAWPF.Custom;
using MFAWPF.Data;
using MFAWPF.Utils.Converters;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System;
using System.CodeDom.Compiler;
using System.Runtime.Intrinsics.Arm;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Web;

namespace MFAWPF.Utils;

public class MaaProcessor
{
    private static MaaProcessor? _instance;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isStopped;

    public bool IsStopped
    {
        get => _isStopped;
        set => _isStopped = value;
    }

    private MaaTasker? _currentTasker;

    public static string Resource => AppDomain.CurrentDomain.BaseDirectory + "Resource";
    public static string ModelResource => $"{Resource}/model/";
    public static string ResourceBase => $"{Resource}/base";
    public static string ResourcePipelineFilePath => $"{ResourceBase}/pipeline/";

    public Queue<MFATask> TaskQueue { get; } = new();
    public static int Money { get; set; }
    public static int AllMoney { get; set; }
    public static Config Config { get; } = new();
    public static List<string>? CurrentResources { get; set; }
    public static AutoInitDictionary AutoInitDictionary { get; } = new();

    public event EventHandler? TaskStackChanged;

    public static MaaProcessor Instance
    {
        get => _instance ??= new MaaProcessor();
        set => _instance = value;
    }

    public MaaProcessor()
    {
    }

    public class TaskAndParam
    {
        public string? Name { get; set; }
        public string? Entry { get; set; }
        public int? Count { get; set; }
        public string? Param { get; set; }
    }

    private DateTime? _startTime;


    public void Start(List<DragItemViewModel>? tasks)
    {
        SetCurrentTasker();
        MainWindow.Data?.SetIdle(false);
        TaskQueue.Push(new MFATask
        {
            Name = "启动脚本",
            Type = MFATask.MFATaskType.MFA,
            Action = () =>
            {
                MainWindow.Instance.RunScript();
            }
        });

        _startTime = DateTime.Now;
        IsStopped = false;
        tasks ??= new List<DragItemViewModel>();
        var taskAndParams = tasks.Select(CreateTaskAndParam).ToList();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        TaskQueue.Push(new MFATask
        {
            Name = "计时",
            Type = MFATask.MFATaskType.MFA,
            Action = () =>
            {
                MainWindow.AddLogByKey("ConnectingTo", null, (MainWindow.Data?.IsAdb).IsTrue()
                    ? "Emulator"
                    : "Window");
                var instance = Task.Run(GetCurrentTasker, token);
                instance.Wait();
                if (instance.Result == null || !instance.Result.Initialized)
                {
                    Growls.Error("InitInstanceFailed".GetLocalizationString());
                    LoggerService.LogWarning("InitControllerFailed".GetLocalizationString());
                    MainWindow.AddLogByKey("InstanceInitFailedLog");
                    Stop();
                    throw new Exception();
                }
                if (!MainWindow.Instance.IsConnected())
                {
                    Growls.Warning("Warning_CannotConnect".GetLocalizationString()
                        .FormatWith((MainWindow.Data?.IsAdb).IsTrue()
                            ? "Emulator".GetLocalizationString()
                            : "Window".GetLocalizationString()));
                    throw new Exception();
                }
            }
        });

        TaskQueue.Push(new MFATask
        {
            Name = "计时",
            Type = MFATask.MFATaskType.MFA,
            Action = () =>
            {
                MeasureExecutionTime(() => _currentTasker?.Controller.Screencap().Wait());
            }
        });


        foreach (var task in taskAndParams)
            TaskQueue.Push(new MFATask
            {
                Name = task.Name,
                Type = MFATask.MFATaskType.MAAFW,
                Count = task.Count ?? 1,
                Action = () => { TryRunTasks(_currentTasker, task.Entry, task.Param); },
            });

        TaskQueue.Push(new MFATask
        {
            Name = "结束",
            Type = MFATask.MFATaskType.MFA,
            Action = () => { MainWindow.Instance?.RunScript("Post-script"); }
        });

        TaskManager.RunTaskAsync(async () =>
        {
            var run = await ExecuteTasks(token);
            if (run)
            {
                Stop(IsStopped);
            }
        }, null, "启动任务");
    }

    public void Stop(bool setIsStopped = true)
    {
        if (_emulatorCancellationTokenSource != null)
        {
            _emulatorCancellationTokenSource?.Cancel();
        }
        if (_cancellationTokenSource != null)
        {
            IsStopped = setIsStopped;
            _cancellationTokenSource?.Cancel();
            TaskManager.RunTaskAsync(() =>
            {
                if (IsStopped)
                    MainWindow.AddLogByKey("Stopping");
                if (_currentTasker == null || _currentTasker?.Abort().Wait() == MaaJobStatus.Succeeded)
                {
                    DisplayTaskCompletionMessage();
                    MainWindow.Data?.SetIdle(true);
                }
                else
                {
                    Growls.Error("StoppingFailed".GetLocalizationString());
                }
            }, null, "停止任务");
            TaskQueue.Clear();
            OnTaskQueueChanged();
            _cancellationTokenSource = null;
        }
        else
        {
            if (setIsStopped)
            {
                Growls.Warning("NoTaskToStop".GetLocalizationString());
                TaskQueue.Clear();
                OnTaskQueueChanged();
            }
        }
    }

    public void HandleAfterTaskOperation()
    {
        if (IsStopped) return;
        int afterTaskIndex = DataSet.GetData("AfterTaskIndex", 0);
        switch (afterTaskIndex)
        {
            case 1:
                CloseMFA();
                break;
            case 2:
                CloseSoftware();
                break;
            case 3:
                CloseSoftwareAndMFA();
                break;
            case 4:
                ShutDown();
                break;
            case 5:
                CloseSoftwareAndRestartMFA();
                break;
            case 6:
                Restart();
                break;
            case 7:
                DingTalkMessageAsync();
                break;
        }
    }


    private CancellationTokenSource? _emulatorCancellationTokenSource;

    private Process? _softwareProcess;

    public void StartSoftware()
    {
        _emulatorCancellationTokenSource = new CancellationTokenSource();
        StartRunnableFile(DataSet.GetData("SoftwarePath", string.Empty) ?? string.Empty,
            DataSet.GetData("WaitSoftwareTime", 60.0), _emulatorCancellationTokenSource.Token);
    }

    private void StartRunnableFile(string exePath, double waitTimeInSeconds, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return;
        var processName = Path.GetFileNameWithoutExtension(exePath);
        if (Process.GetProcessesByName(processName).Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(DataSet.GetData("EmulatorConfig", string.Empty)))
                _softwareProcess =
                    Process.Start(exePath, DataSet.GetData("EmulatorConfig", string.Empty) ?? string.Empty);
            else
                _softwareProcess = Process.Start(exePath);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(DataSet.GetData("EmulatorConfig", string.Empty)))
                Process.Start(exePath, DataSet.GetData("EmulatorConfig", string.Empty) ?? string.Empty);
            else
                Process.Start(exePath);
        }

        for (double remainingTime = waitTimeInSeconds; remainingTime > 0; remainingTime -= 1)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (remainingTime % 10 == 0)
            {
                MainWindow.AddLogByKey("WaitSoftwareTime", null,
                    (MainWindow.Data?.IsAdb).IsTrue()
                        ? "Emulator"
                        : "Window",
                    remainingTime.ToString()
                );
            }

            try
            {
                Thread.Sleep(1000);
            }
            catch
            {
            }
        }

    }

    private static string GetCommandLine(Process process)
    {
        return GetCommandLine(process.Id); // 这里可能需要用 WMI 方法获取参数
    }

    private static string GetCommandLine(int processId)
    {
        var commandLine = string.Empty;

        // 使用 WMI 查询命令行参数
        var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}";
        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
        {
            foreach (ManagementObject obj in searcher.Get())
            {
                commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
            }
        }

        return commandLine;
    }

    private void CloseSoftware()
    {
        if (_softwareProcess != null)
        {
            _softwareProcess.Kill();
            _softwareProcess = null;
        }
        else
        {
            var softwarePath = DataSet.GetData("SoftwarePath", string.Empty);

            if (!string.IsNullOrEmpty(softwarePath) && (MainWindow.Data?.IsAdb).IsTrue())
            {
                string processName = Path.GetFileNameWithoutExtension(softwarePath);
                var emulatorConfig = DataSet.GetData("EmulatorConfig", string.Empty);

                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    var commandLine = GetCommandLine(process);
                    if (string.IsNullOrEmpty(emulatorConfig) || MainWindow.ExtractNumberFromEmulatorConfig(emulatorConfig) == 0 && commandLine.Split(" ").Length == 1 || commandLine.ToLower().Contains(emulatorConfig.ToLower()))
                    {
                        process.Kill();
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(Config.AdbDevice.Name) && (MainWindow.Data?.IsAdb).IsTrue())
            {
                var windowName = Config.AdbDevice.Name;
                if (windowName.Contains("MuMu"))
                    windowName = "MuMuPlayer";
                else if (windowName.Contains("Nox"))
                    windowName = "Nox";
                else if (windowName.Contains("LDPlayer"))
                    windowName = "LDPlayer";
                else if (windowName.Contains("XYAZ"))
                    windowName = "MEmu";
                else if (windowName.Contains("BlueStacks"))
                    windowName = "HD-Player";

                var emulatorConfig = DataSet.GetData("EmulatorConfig", string.Empty);

                var processes = Process.GetProcesses().Where(p =>
                    p.ProcessName.StartsWith(windowName));

                foreach (var process in processes)
                {
                    try
                    {
                        var commandLine = GetCommandLine(process);

                        if (string.IsNullOrEmpty(emulatorConfig) || commandLine.ToLower().Contains(emulatorConfig.ToLower()))
                        {
                            process.Kill();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"关闭进程时出错: {ex.Message}");
                    }
                }
            }
        }
    }

    private void CloseMFA()
    {
        Growls.Process(Application.Current.Shutdown);
    }


    private void CloseSoftwareAndMFA()
    {
        CloseSoftware();
        CloseMFA();
    }

    private void ShutDown()
    {
        CloseSoftware();
        Process.Start("shutdown", "/s /t 0");
    }

    private void CloseSoftwareAndRestartMFA()
    {
        CloseSoftware();
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        Growls.Process(Application.Current.Shutdown);
    }

    private void Restart()
    {
        CloseSoftware();
        Process.Start("shutdown", "/r /t 0");
    }

    private static async Task DingTalkMessageAsync()
    {
        // 从文件中读取配置信息
        string configFilePath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "config"), "auth.txt");
        var config = ReadConfigFile(configFilePath);
        // 配置文件样例：
        // accessToken=65ff4c133
        // secret=SEC9eca40f06x

        // 钉钉机器人配置信息
        string accessToken = config["accessToken"];
        string secret = config["secret"];

        // 生成时间戳（Unix时间戳，单位为秒）
        string timestamp = GetTimestamp();

        string sign = CalculateSignature(timestamp, secret);

        // 要发送的消息
        var message = new
        {
            msgtype = "text",
            text = new
            {
                content = "任务已全部完成"
            }
        };

        // 发送消息
        try
        {
            string apiUrl =
                $"https://oapi.dingtalk.com/robot/send?access_token={accessToken}&timestamp={timestamp}&sign={sign}";
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8,
                    "application/json");
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("消息发送成功");
                }
                else
                {
                    Console.WriteLine($"消息发送失败：{response.StatusCode} {await response.Content.ReadAsStringAsync()}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送消息出错：{ex.Message}");
        }
    }

    static string GetTimestamp()
    {
        return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
    }

    private static string CalculateSignature(string timestamp, string secret)
    {
        string stringToSign = $"{timestamp}\n{secret}";

        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        byte[] stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);

        byte[] hmacCode = ComputeHmacSha256(secretBytes, stringToSignBytes);
        string base64Encoded = Convert.ToBase64String(hmacCode);
        //string sign = HttpUtility.UrlEncode(Convert.ToBase64String(hmacCode));
        string sign = WebUtility.UrlEncode(base64Encoded).Replace("+", "%20").Replace("/", "%2F").Replace("=", "%3D");
        return sign;
        // Console.WriteLine(timestamp);
        // Console.WriteLine(sign);
    }

    static byte[] ComputeHmacSha256(byte[] key, byte[] data)
    {
        using (var hmacsha256 = new HMACSHA256(key))
        {
            return hmacsha256.ComputeHash(data);
        }
    }

    static Dictionary<string, string> ReadConfigFile(string filePath)
    {
        var config = new Dictionary<string, string>();
        string[] lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var parts = line.Split('=');
            if (parts.Length == 2)
            {
                config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return config;
    }

    private TaskAndParam CreateTaskAndParam(DragItemViewModel task)
    {
        var taskModels = task.InterfaceItem?.PipelineOverride ?? new Dictionary<string, TaskModel>();

        UpdateTaskDictionary(ref taskModels, task.InterfaceItem?.Option);

        var taskParams = SerializeTaskParams(taskModels);

        return new TaskAndParam
        {
            Name = task.InterfaceItem?.Name,
            Entry = task.InterfaceItem?.Entry,
            Count = task.InterfaceItem?.Repeatable == true ? (task.InterfaceItem?.RepeatCount ?? 1) : 1,
            Param = taskParams
        };
    }

    private void UpdateTaskDictionary(ref Dictionary<string, TaskModel> taskModels,
        List<MaaInterface.MaaInterfaceSelectOption>? options)
    {
        if (MainWindow.Instance?.TaskDictionary != null)
            MainWindow.Instance.TaskDictionary = MainWindow.Instance.TaskDictionary.MergeTaskModels(taskModels);

        if (options == null) return;

        foreach (var selectOption in options)
        {
            if (MaaInterface.Instance?.Option?.TryGetValue(selectOption.Name ?? string.Empty,
                    out var interfaceOption)
                == true
                && MainWindow.Instance != null
                && selectOption.Index is int index
                && interfaceOption.Cases is { } cases
                && cases[index]?.PipelineOverride != null)
            {
                var param = interfaceOption.Cases[selectOption.Index.Value].PipelineOverride;
                MainWindow.Instance.TaskDictionary = MainWindow.Instance.TaskDictionary.MergeTaskModels(param);
                taskModels = taskModels.MergeTaskModels(param);
            }
        }
    }

    private string SerializeTaskParams(Dictionary<string, TaskModel> taskModels)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        try
        {
            return JsonConvert.SerializeObject(taskModels, settings);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "{}";
        }
    }

    static void MeasureExecutionTime(Action methodToMeasure)
    {
        var stopwatch = Stopwatch.StartNew();

        methodToMeasure();

        stopwatch.Stop();
        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        MainWindow.AddLogByKey("ScreenshotTime", null, elapsedMilliseconds.ToString(),
            MainWindow.Instance?.ScreenshotType() ?? string.Empty);
    }

    static async Task MeasureExecutionTimeAsync(Func<Task> methodToMeasure)
    {
        var stopwatch = Stopwatch.StartNew();

        await methodToMeasure();

        stopwatch.Stop();
        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        MainWindow.AddLogByKey("ScreenshotTime", null, elapsedMilliseconds.ToString(),
            MainWindow.Instance?.ScreenshotType() ?? string.Empty);
    }

    private async Task<bool> ExecuteTasks(CancellationToken token)
    {
        while (TaskQueue.Count > 0)
        {
            if (token.IsCancellationRequested) return false;
            var task = TaskQueue.Dequeue();

            if (!task.Run())
            {
                if (IsStopped) return false;
                break;
            }

            OnTaskQueueChanged();
        }

        return true;
    }

    private void DisplayTaskCompletionMessage()
    {
        if (IsStopped)
        {
            Growl.Info("TaskStopped".GetLocalizationString());
            MainWindow.AddLogByKey("TaskAbandoned");
            IsStopped = false;
        }
        else
        {
            Growl.Info("TaskCompleted".GetLocalizationString());
            if (_startTime != null)
            {
                TimeSpan elapsedTime = DateTime.Now - (DateTime)_startTime;
                MainWindow.AddLogByKey("TaskAllCompletedWithTime", null, ((int)elapsedTime.TotalHours).ToString(),
                    ((int)elapsedTime.TotalMinutes % 60).ToString(), ((int)elapsedTime.TotalSeconds % 60).ToString());
            }
            else
            {
                MainWindow.AddLogByKey("TaskAllCompleted");
            }

            HandleAfterTaskOperation();
        }

        _startTime = null;
    }

    public void OnTaskQueueChanged()
    {
        TaskStackChanged?.Invoke(this, EventArgs.Empty);
    }

    public MaaTasker? GetCurrentTasker()
    {
        return _currentTasker ??= InitializeMaaTasker();
    }

    public void SetCurrentTasker(MaaTasker? tasker = null)
    {
        _currentTasker = tasker;
    }

    public static string HandleStringsWithVariables(string content)
    {
        try
        {
            return Regex.Replace(content, @"\{(\+\+|\-\-)?(\w+)(\+\+|\-\-)?([\+\-\*/]\w+)?\}", match =>
            {
                var prefix = match.Groups[1].Value;
                var counterKey = match.Groups[2].Value;
                var suffix = match.Groups[3].Value;
                var operation = match.Groups[4].Value;

                int value = AutoInitDictionary.GetValueOrDefault(counterKey, 0);

                // 前置操作符7
                if (prefix == "++")
                {
                    value = ++AutoInitDictionary[counterKey];
                }
                else if (prefix == "--")
                {
                    value = --AutoInitDictionary[counterKey];
                }

                // 后置操作符
                if (suffix == "++")
                {
                    value = AutoInitDictionary[counterKey]++;
                }
                else if (suffix == "--")
                {
                    value = AutoInitDictionary[counterKey]--;
                }

                // 算术操作
                if (!string.IsNullOrEmpty(operation))
                {
                    string operationType = operation[0].ToString();
                    string operandKey = operation.Substring(1);

                    if (AutoInitDictionary.TryGetValue(operandKey, out var operandValue))
                    {
                        value = operationType switch
                        {
                            "+" => value + operandValue,
                            "-" => value - operandValue,
                            "*" => value * operandValue,
                            "/" => value / operandValue,
                            _ => value
                        };
                    }
                }

                return value.ToString();
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            ErrorView.ShowException(e);
            return content;
        }
    }

    private MaaTasker? InitializeMaaTasker()
    {
        AutoInitDictionary.Clear();

        LoggerService.LogInfo("LoadingResources".GetLocalizationString());
        MaaResource maaResource;
        try
        {
            LoggerService.LogInfo(string.Join(",", CurrentResources ?? Array.Empty<string>().ToList()));
            maaResource = new MaaResource(CurrentResources ?? Array.Empty<string>().ToList());
        }
        catch (Exception e)
        {
            HandleInitializationError(e, "LoadResourcesFailed".GetLocalizationString());
            return null;
        }

        LoggerService.LogInfo("Resources initialized successfully".GetLocalizationString());
        LoggerService.LogInfo("LoadingController".GetLocalizationString());
        MaaController controller;
        try
        {
            controller = InitializeController();
        }
        catch (Exception e)
        {
            HandleInitializationError(e,
                "ConnectingEmulatorOrWindow".GetLocalizationString()
                    .FormatWith((MainWindow.Data?.IsAdb).IsTrue()
                        ? "Emulator".GetLocalizationString()
                        : "Window".GetLocalizationString()), true,
                "InitControllerFailed".GetLocalizationString());
            return null;
        }

        LoggerService.LogInfo("InitControllerSuccess".GetLocalizationString());


        try
        {
            var tasker = new MaaTasker
            {
                Controller = controller,
                Resource = maaResource,
                DisposeOptions = DisposeOptions.All,
            };
            RegisterCustomRecognitionsAndActions(tasker);
            if (!DataSet.GetData("EnableGPU", true))
            {
                tasker.Resource.SetOptionInferenceDevice(InferenceDevice.CPU);
                LoggerService.LogInfo("已禁用GPU加速！");
            }
            tasker.Utility.SetOptionSaveDraw(DataSet.GetData("EnableSaveDraw", false));
            return tasker;
        }
        catch (Exception e)
        {
            LoggerService.LogError(e);
            return null;
        }
    }

    private MaaController InitializeController()
    {
        if ((MainWindow.Data?.IsAdb).IsTrue())
        {
            LoggerService.LogInfo($"AdbPath: {Config.AdbDevice.AdbPath}");
            LoggerService.LogInfo($"AdbSerial: {Config.AdbDevice.AdbSerial}");
            LoggerService.LogInfo($"ScreenCap: {Config.AdbDevice.ScreenCap}");
            LoggerService.LogInfo($"Input: {Config.AdbDevice.Input}");
            LoggerService.LogInfo($"Config: {Config.AdbDevice.Config}");
        }
        else
        {
            LoggerService.LogInfo($"HWnd: {Config.DesktopWindow.HWnd}");
            LoggerService.LogInfo($"ScreenCap: {Config.DesktopWindow.ScreenCap}");
            LoggerService.LogInfo($"Input: {Config.DesktopWindow.Input}");
            LoggerService.LogInfo($"Link: {Config.DesktopWindow.Link}");
            LoggerService.LogInfo($"Check: {Config.DesktopWindow.Check}");
        }
        return (MainWindow.Data?.IsAdb).IsTrue()
            ? new MaaAdbController(
                Config.AdbDevice.AdbPath,
                Config.AdbDevice.AdbSerial,
                Config.AdbDevice.ScreenCap, Config.AdbDevice.Input,
                !string.IsNullOrWhiteSpace(Config.AdbDevice.Config) ? Config.AdbDevice.Config : "{}")
            //!string.IsNullOrWhiteSpace(Config.AdbDevice.Config) && Config.AdbDevice.Config != "{}" &&
            //(DataSet.GetData("AdbConfig", "{\"extras\":{}}") == "{\"extras\":{}}" ||
            //string.IsNullOrWhiteSpace(DataSet.GetData("AdbConfig", "{\"extras\":{}}")))
            //   ? Config.AdbDevice.Config
            //   : DataSet.GetData("AdbConfig", "{\"extras\":{}}"))
            : new MaaWin32Controller(
                Config.DesktopWindow.HWnd,
                Config.DesktopWindow.ScreenCap, Config.DesktopWindow.Input,
                Config.DesktopWindow.Link,
                Config.DesktopWindow.Check);
    }

    static List<MetadataReference>? MetadataReferences;

    static List<MetadataReference> GetMetadataReferences()
    {
        if (MetadataReferences == null)
        {
            var domainAssemblys = AppDomain.CurrentDomain.GetAssemblies();
            MetadataReferences = new List<MetadataReference>();

            foreach (var assembly in domainAssemblys)
            {
                if (!assembly.IsDynamic)
                {
                    unsafe
                    {
                        assembly.TryGetRawMetadata(out byte* blob, out int length);
                        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
                        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                        var metadataReference = assemblyMetadata.GetReference();
                        MetadataReferences.Add(metadataReference);
                    }
                }
            }

            unsafe
            {
                typeof(System.Linq.Expressions.Expression).Assembly.TryGetRawMetadata(out byte* blob, out int length);
                MetadataReferences.Add(AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length)).GetReference());
            }
        }
        return MetadataReferences;
    }

    public static IEnumerable<CustomValue<object>> LoadAndInstantiateCustomClasses(string directory, string[] interfacesToImplement)
    {
        var customClasses = new List<CustomValue<object>>();
        if (Path.Exists(directory))
        {
            var csFiles = Directory.GetFiles(directory, "*.cs");

            var references = GetMetadataReferences();

            foreach (var filePath in csFiles)
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                LoggerService.LogInfo("Trying to parse " + name);
                string code = File.ReadAllText(filePath);

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("DynamicAssembly")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddSyntaxTrees(syntaxTree)
                    .AddReferences(references);

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        var failures = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                        foreach (var diagnostic in failures)
                        {
                            LoggerService.LogError($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                        continue;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());

                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var iface in interfacesToImplement)
                        {
                            if (type.GetInterfaces().Any(i => i.Name == iface))
                            {
                                var instance = Activator.CreateInstance(type);
                                if (instance != null)
                                    customClasses.Add(new CustomValue<object>(name, instance));
                            }
                        }
                    }
                }
            }
        }
        return customClasses;
    }

    private void RegisterCustomRecognitionsAndActions(MaaTasker instance)
    {
        if (MaaInterface.Instance == null) return;
        LoggerService.LogInfo("RegisteringCustomRecognizer".GetLocalizationString());
        LoggerService.LogInfo("RegisteringCustomAction".GetLocalizationString());
        // instance.Resource.Register(new MoneyDetectRecognition());
        // instance.Resource.Register(new MoneyRecognition());
        var customClasses = LoadAndInstantiateCustomClasses($"{Resource}/custom", [
            "IMaaCustomRecognition",
            "IMaaCustomAction",
        ]);

        foreach (var customClass in customClasses)
        {
            if (customClass.Value is IMaaCustomRecognition recognition)
            {
                instance.Resource.Register(recognition);
                LoggerService.LogInfo("Registering IMaaCustomRecognition " + customClass.Name);
            }
            else if (customClass.Value is IMaaCustomAction action)
            {
                instance.Resource.Register(action);
                LoggerService.LogInfo("Registering IMaaCustomAction " + customClass.Name);
            }
        }
        instance.Callback += (_, args) =>
        {
            var jObject = JObject.Parse(args.Details);
            var name = jObject["name"]?.ToString() ?? string.Empty;
            if (args.Message.Equals(MaaMsg.Task.Action.Succeeded))
            {
                if (MainWindow.Instance?.TaskDictionary.TryGetValue(name, out var taskModel) == true)
                {
                    DisplayFocusTip(taskModel);
                }
            }
        };
    }

    private void DisplayFocusTip(TaskModel taskModel)
    {
        var converter = new BrushConverter();

        if (taskModel.FocusTip != null)
        {
            for (int i = 0; i < taskModel.FocusTip.Count; i++)
            {
                Brush? brush = null;
                var tip = taskModel.FocusTip[i];
                try
                {
                    if (taskModel.FocusTipColor != null && taskModel.FocusTipColor.Count > i)
                        brush = converter.ConvertFromString(taskModel.FocusTipColor[i]) as Brush;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    LoggerService.LogError(e);
                }

                MainWindow.AddLog(HandleStringsWithVariables(tip), brush);
            }
        }
    }

    private void HandleInitializationError(Exception e,
        string message,
        bool hasWarning = false,
        string waringMessage = "")
    {
        Console.WriteLine(e);
        TaskQueue.Clear();
        OnTaskQueueChanged();
        if (MainWindow.Data != null)
            MainWindow.Data.Idle = true;
        Growls.Error(message);
        if (hasWarning)
            LoggerService.LogWarning(waringMessage);
        LoggerService.LogError(e.ToString());
    }

    public BitmapImage? GetBitmapImage()
    {
        using var buffer = GetImage(GetCurrentTasker()?.Controller);
        if (buffer == null) return null;

        var encodedDataHandle = buffer.GetEncodedData(out var size);
        if (encodedDataHandle == IntPtr.Zero)
        {
            Growls.Error("Handle为空！");
            return null;
        }

        var imageData = new byte[size];
        Marshal.Copy(encodedDataHandle, imageData, 0, (int)size);

        if (imageData.Length == 0)
            return null;

        return CreateBitmapImage(imageData);
    }

    private static BitmapImage CreateBitmapImage(byte[] imageData)
    {
        var bitmapImage = new BitmapImage();
        using (var ms = new MemoryStream(imageData))
        {
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
        }

        bitmapImage.Freeze();
        return bitmapImage;
    }

    private void TryRunTasks(MaaTasker? maa, string? task, string? taskParams)
    {
        if (maa == null || task == null) throw new NullReferenceException();
        if (string.IsNullOrWhiteSpace(taskParams)) taskParams = "{}";
        maa.AppendPipeline(task, taskParams).Wait().ThrowIfNot(MaaJobStatus.Succeeded);
    }

    private static MaaImageBuffer GetImage(IMaaController? maaController)
    {
        var buffer = new MaaImageBuffer();
        if (maaController == null)
            return buffer;
        var status = maaController.Screencap().Wait();
        Console.WriteLine(status);
        if (status != MaaJobStatus.Succeeded)
            return buffer;
        maaController.GetCachedImage(buffer);
        return buffer;
    }
}
