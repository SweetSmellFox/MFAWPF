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
using MaaFramework.Binding.Messages;
using MFAWPF.Custom;
using MFAWPF.Data;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System;
using System.Runtime.Intrinsics.Arm;
using System.Collections;
using System.Net;
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

    public Queue<TaskAndParam> TaskQueue { get; } = new();
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
        if (!Config.IsConnected)
        {
            Growls.Warning("Warning_CannotConnect".GetLocalizationString()
                .FormatWith((MainWindow.Data?.IsAdb).IsTrue()
                    ? "Emulator".GetLocalizationString()
                    : "Window".GetLocalizationString()));
            return;
        }

        _startTime = DateTime.Now;
        IsStopped = false;
        tasks ??= new List<DragItemViewModel>();
        var taskAndParams = tasks.Select(CreateTaskAndParam).ToList();

        foreach (var task in taskAndParams)
            TaskQueue.Enqueue(task);
        OnTaskQueueChanged();

        SetCurrentTasker();
        MainWindow.Data?.SetIdle(false);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        TaskManager.RunTaskAsync(async () =>
        {
            MainWindow.Data?.AddLogByKey("ConnectingTo", null, (MainWindow.Data?.IsAdb).IsTrue()
                ? "Emulator"
                : "Window");
            var instance = await Task.Run(GetCurrentTasker, token);

            if (instance == null || !instance.Initialized)
            {
                Growls.ErrorGlobal("InitInstanceFailed".GetLocalizationString());
                LoggerService.LogWarning("InitControllerFailed".GetLocalizationString());
                MainWindow.Data?.AddLogByKey("InstanceInitFailedLog");
                Stop();
                return;
            }

            bool run = await ExecuteTasks(token);
            if (run)
                Stop(IsStopped);
        }, null, "启动任务");
    }

    public void Stop(bool setIsStopped = true)
    {
        if (_emulatorCancellationTokenSource != null)
        {
            _emulatorCancellationTokenSource?.Cancel();
            MainWindow.Data?.AddLogByKey("Stopping");

            if (DataSet.GetData("AutoStartIndex", 0) != 1)
            {
                EndAutoStart();
            }
        }
        else if (_cancellationTokenSource != null)
        {
            IsStopped = setIsStopped;
            _cancellationTokenSource?.Cancel();
            TaskManager.RunTaskAsync(() =>
            {
                if (IsStopped)
                    MainWindow.Data?.AddLogByKey("Stopping");
                if (_currentTasker == null || (_currentTasker?.Abort()).IsTrue())
                {
                    DisplayTaskCompletionMessage();
                    MainWindow.Data?.SetIdle(true);
                }
                else
                {
                    Growls.ErrorGlobal("StoppingFailed".GetLocalizationString());
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
                CloseEmulator();
                break;
            case 3:
                CloseEmulatorAndMFA();
                break;
            case 4:
                ShutDown();
                break;
            case 5:
                CloseEmulatorAndRestartMFA();
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
    public bool ShouldEndStart => _emulatorCancellationTokenSource is { IsCancellationRequested: true };

    public void EndAutoStart(bool showTip = true)
    {
        _emulatorCancellationTokenSource = null;
        TaskQueue.Clear();
        OnTaskQueueChanged();
        if (showTip)
            MainWindow.Data?.AddLogByKey("TaskAbandoned");
        MainWindow.Data?.SetIdle(true);
    }

    public async Task StartEmulator()
    {
        _emulatorCancellationTokenSource = new CancellationTokenSource();
        MainWindow.Instance?.ToggleTaskButtonsVisibility(true);
        MainWindow.Data?.SetIdle(false);
        await StartRunnableFile(DataSet.GetData("EmulatorPath", string.Empty) ?? string.Empty,
            DataSet.GetData("WaitEmulatorTime", 60.0), _emulatorCancellationTokenSource.Token);
    }

    private Process? _emulatorProcess;

    private async Task StartRunnableFile(string exePath, double waitTimeInSeconds, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return;
        var processName = Path.GetFileNameWithoutExtension(exePath);
        if (Process.GetProcessesByName(processName).Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(DataSet.GetData("EmulatorConfig", string.Empty)))
                _emulatorProcess =
                    Process.Start(exePath, DataSet.GetData("EmulatorConfig", string.Empty) ?? string.Empty);
            else
                _emulatorProcess = Process.Start(exePath);
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
                MainWindow.Data?.AddLogByKey("WaitEmulatorTime", null, remainingTime.ToString());
            }

            try
            {
                await Task.Delay(1000, token);
            }
            catch
            {
            }
        }

        if (DataSet.GetData("AutoStartIndex", 0) == 0)
            EndAutoStart(false);
        else
            _emulatorCancellationTokenSource = null;
    }

// 获取进程的命令行参数的辅助方法
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

    private void CloseEmulator()
    {
        if (_emulatorProcess != null)
        {
            _emulatorProcess.Kill();
            _emulatorProcess = null;
        }
        else
        {
            var emulatorPath = DataSet.GetData("EmulatorPath", string.Empty);

            if (!string.IsNullOrEmpty(emulatorPath))
            {
                string processName = Path.GetFileNameWithoutExtension(emulatorPath);
                var emulatorConfig = DataSet.GetData("EmulatorConfig", string.Empty);

                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    var commandLine = GetCommandLine(process);
                    if (string.IsNullOrEmpty(emulatorConfig) ||
                        MainWindow.ExtractNumberFromEmulatorConfig(emulatorConfig) == 0 &&
                        commandLine.Split(" ").Length == 1 ||
                        commandLine.ToLower().Contains(emulatorConfig.ToLower()))
                    {
                        process.Kill();
                        break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(Config.AdbDevice.Name))
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

                        if (string.IsNullOrEmpty(emulatorConfig) ||
                            commandLine.ToLower().Contains(emulatorConfig.ToLower()))
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


    private void CloseEmulatorAndMFA()
    {
        CloseEmulator();
        CloseMFA();
    }

    private void ShutDown()
    {
        CloseEmulator();
        Process.Start("shutdown", "/s /t 0");
    }

    private void CloseEmulatorAndRestartMFA()
    {
        CloseEmulator();
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        Growls.Process(Application.Current.Shutdown);
    }

    private void Restart()
    {
        CloseEmulator();
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
                    out var interfaceOption) ==
                true &&
                MainWindow.Instance != null &&
                selectOption.Index is int index &&
                interfaceOption.Cases is { } cases &&
                cases[index]?.PipelineOverride != null)
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

        MainWindow.Data?.AddLogByKey("ScreenshotTime", null, elapsedMilliseconds.ToString(),
            MainWindow.Instance?.ScreenshotType() ?? string.Empty);
    }

    static async Task MeasureExecutionTimeAsync(Func<Task> methodToMeasure)
    {
        var stopwatch = Stopwatch.StartNew();

        await methodToMeasure();

        stopwatch.Stop();
        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        MainWindow.Data?.AddLogByKey("ScreenshotTime", null, elapsedMilliseconds.ToString(),
            MainWindow.Instance?.ScreenshotType() ?? string.Empty);
    }

    private async Task<bool> ExecuteTasks(CancellationToken token)
    {
        MeasureExecutionTime(() => _currentTasker?.Controller.Screencap().Wait());
        while (TaskQueue.Count > 0)
        {
            if (token.IsCancellationRequested) return false;

            var task = TaskQueue.Peek();
            for (var i = 0; i < task.Count; i++)
            {
                if (TaskQueue.Count > 0)
                {
                    var taskA = TaskQueue.Peek();
                    MainWindow.Data?.AddLogByKey("TaskStart", null, taskA.Name ?? string.Empty);
                    if (!TryRunTasks(_currentTasker, taskA.Entry, taskA.Param))
                    {
                        if (IsStopped) return false;
                        break;
                    }
                }
            }

            if (TaskQueue.Count > 0)
                TaskQueue.Dequeue();
            OnTaskQueueChanged();
        }

        return true;
    }

    private void DisplayTaskCompletionMessage()
    {
        if (IsStopped)
        {
            Growl.Info("TaskStopped".GetLocalizationString());
            MainWindow.Data?.AddLogByKey("TaskAbandoned");
            IsStopped = false;
        }
        else
        {
            Growl.Info("TaskCompleted".GetLocalizationString());
            if (_startTime != null)
            {
                TimeSpan elapsedTime = DateTime.Now - (DateTime)_startTime;
                MainWindow.Data?.AddLogByKey("TaskAllCompletedWithTime", null, ((int)elapsedTime.TotalHours).ToString(),
                    ((int)elapsedTime.TotalMinutes % 60).ToString(), ((int)elapsedTime.TotalSeconds % 60).ToString());
            }
            else
            {
                MainWindow.Data?.AddLogByKey("TaskAllCompleted");
            }

            HandleAfterTaskOperation();
        }

        _startTime = null;
    }

    protected virtual void OnTaskQueueChanged()
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
            Console.WriteLine(string.Join(",", CurrentResources ?? Array.Empty<string>().ToList()));
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
                tasker.Resource.SetOptionInferenceDevice(InferenceDevice.CPU);
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

    private void RegisterCustomRecognitionsAndActions(MaaTasker instance)
    {
        if (MaaInterface.Instance == null) return;
        LoggerService.LogInfo("RegisteringCustomRecognizer".GetLocalizationString());

        // foreach (var recognizer in MaaInterface.Instance.CustomRecognizerExecutors)
        // {
        //     LoggerService.LogInfo($"RegisterCustomRecognizer".GetLocalizationString().FormatWith(recognizer.Name));
        //     instance.Toolkit.ExecAgent.Register(instance, recognizer);
        // }
        //
        // LoggerService.LogInfo("RegisteringCustomAction".GetLocalizationString());
        // foreach (var action in MaaInterface.Instance.CustomActionExecutors)
        // {
        //     LoggerService.LogInfo("RegisterCustomAction".GetLocalizationString().FormatWith(action.Name));
        //     instance.Toolkit.ExecAgent.Register(instance, action);
        // }

        instance.Resource.Register(new MoneyRecognition());
        instance.Resource.Register(new MoneyDetectRecognition());

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

                MainWindow.Data?.AddLog(HandleStringsWithVariables(tip), brush);
            }
        }
    }

    private void HandleInitializationError(Exception e, string message, bool hasWarning = false,
        string waringMessage = "")
    {
        Console.WriteLine(e);
        TaskQueue.Clear();
        OnTaskQueueChanged();
        if (MainWindow.Data != null)
            MainWindow.Data.Idle = true;
        Growls.ErrorGlobal(message);
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
            Growls.ErrorGlobal("Handle为空！");
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

    private bool TryRunTasks(MaaTasker? maa, string? task, string? taskParams)
    {
        if (maa == null || task == null) return false;
        if (string.IsNullOrWhiteSpace(taskParams)) taskParams = "{}";
        return maa.AppendPipeline(task, taskParams).Wait() == MaaJobStatus.Succeeded;
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