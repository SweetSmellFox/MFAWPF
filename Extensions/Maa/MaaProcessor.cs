using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MaaFramework.Binding.Notification;
using MailKit.Net.Smtp;
using MailKit.Security;
using MFAWPF.Configuration;
using MFAWPF.Helper;
using MFAWPF.Helper.ValueType;
using MFAWPF.ViewModels.UserControl.Settings;
using MFAWPF.Views.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DragItemViewModel = MFAWPF.ViewModels.Tool.DragItemViewModel;
using MFATask = MFAWPF.Helper.ValueType.MFATask;


namespace MFAWPF.Extensions.Maa;

public class MaaProcessor
{
    private static MaaProcessor? _instance;
    private static Random Random = new();
    public static MaaUtility MaaUtility { get; } = new();
    public static MaaToolkit MaaToolkit { get; } = new(init: true);

    public CancellationTokenSource? CancellationTokenSource { get; set; } = new();

    private MaaTasker? _currentTasker;
    private MaaAgentClient? _agentClient;
    public static string Resource => AppContext.BaseDirectory + "Resource";
    public static string ResourceBase => $"{Resource}/base";

    public Queue<MFATask> TaskQueue { get; } = new();

    public static MaaFWConfiguration MaaFwConfiguration { get; } = new();

    public static AutoInitDictionary AutoInitDictionary { get; } = new();

    public event EventHandler? TaskStackChanged;

    public static MaaProcessor Instance
    {
        get => _instance ??= new MaaProcessor();
        set => _instance = value;
    }

    public class TaskAndParam
    {
        public string? Name { get; set; }
        public string? Entry { get; set; }
        public int? Count { get; set; }

        public Dictionary<string, TaskModel>? Tasks
        {
            get;
            set;
        }
        public string? Param { get; set; }
    }

    private DateTime? _startTime;

    public static int Money { get; set; } = 0;

    public async Task Start(List<DragItemViewModel>? tasks, bool onlyStart = false, bool checkUpdate = false)
    {
        CancellationTokenSource = new CancellationTokenSource();
        SetCurrentTasker();
        Instances.RootViewModel.SetIdle(false);

        _startTime = DateTime.Now;
        tasks ??= new List<DragItemViewModel>();


        var token = CancellationTokenSource.Token;
        var taskAndParams = tasks.Select(CreateTaskAndParam).ToList();

        if (!onlyStart)
        {
            await InitializeConnectionTasksAsync(token);
            await AddCoreTasksAsync(taskAndParams, token);
            await AddPostTasksAsync(checkUpdate, token);
        }

        await TaskManager.RunTaskAsync(async () =>
        {
            var runSuccess = await ExecuteTasks(token);
            if (runSuccess)
            {
                Stop(true, onlyStart);
            }
        }, token, name: "启动任务");

    }

    async private Task InitializeConnectionTasksAsync(CancellationToken token)
    {
        TaskQueue.Push(CreateMFATask("启动脚本", async () =>
        {
            await TaskManager.RunTaskAsync(() => Instances.RootView.RunScript(), token);
        }));

        TaskQueue.Push(CreateMFATask("连接设备", async () =>
        {
            await HandleDeviceConnectionAsync(token);
        }));

        TaskQueue.Push(CreateMFATask("性能基准", async () =>
        {
            await MeasureScreencapPerformanceAsync(token);
        }));
    }

    async private Task HandleDeviceConnectionAsync(CancellationToken token)
    {
        var controllerType = Instances.ConnectingViewModel.CurrentController;
        var isAdb = controllerType == MaaControllerTypes.Adb;

        RootView.AddLogByKey("ConnectingTo", null, true, isAdb ? "Emulator" : "Window");

        var connected = await TryConnectAsync(token);

        if (!connected && isAdb)
        {
            connected = await HandleAdbConnectionAsync(token);
        }

        if (!connected)
        {
            await HandleConnectionFailureAsync(isAdb, token);
            throw new Exception("Connection failed after all retries");
        }

        Instances.ConnectingViewModel.SetConnected(true);
    }

    async private Task<bool> HandleAdbConnectionAsync(CancellationToken token)
    {
        bool connected = false;
        var retrySteps = new List<Func<CancellationToken, Task<bool>>>
        {
            async t => await RetryConnectionAsync(t, StartSoftware, "TryToStartEmulator", Instances.ConnectSettingsUserControlModel.RetryOnDisconnected),
            async t => await RetryConnectionAsync(t, ReconnectByAdb, "TryToReconnectByAdb"),
            async t => await RetryConnectionAsync(t, RestartAdb, "RestartAdb", Instances.ConnectSettingsUserControlModel.AllowAdbRestart),
            async t => await RetryConnectionAsync(t, HardRestartAdb, "HardRestartAdb", Instances.ConnectSettingsUserControlModel.AllowAdbHardRestart)
        };

        foreach (var step in retrySteps)
        {
            if (token.IsCancellationRequested) break;
            connected = await step(token);
            if (connected) break;
        }

        return connected;
    }

    async private Task<bool> RetryConnectionAsync(CancellationToken token, Func<Task> action, string logKey, bool enable = true)
    {
        if (!enable) return false;
        token.ThrowIfCancellationRequested();
        RootView.AddLog("ConnectFailed".ToLocalization() + "\n" + logKey.ToLocalization());
        await action();
        if (token.IsCancellationRequested)
        {
            Stop();
            return false;
        }

        return await TryConnectAsync(token);
    }

    async private Task HandleConnectionFailureAsync(bool isAdb, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        LoggerService.LogWarning("ConnectFailed".ToLocalization());
        RootView.AddLogByKey("ConnectFailed");
        Instances.ConnectingViewModel.SetConnected(false);
        GrowlHelper.Warning("Warning_CannotConnect".ToLocalizationFormatted(true, isAdb ? "Emulator" : "Window"));
        Stop();
    }

    async private Task AddCoreTasksAsync(List<TaskAndParam> taskAndParams, CancellationToken token)
    {
        foreach (var task in taskAndParams)
        {
            TaskQueue.Push(CreateMaaFWTask(task.Name,
                async () =>
                {
                    token.ThrowIfCancellationRequested();
                    if (task.Tasks != null)
                        Instances.TaskQueueView.TaskDictionary = task.Tasks;
                    await TryRunTasksAsync(_currentTasker, task.Entry, task.Param, token);
                }, task.Count ?? 1
            ));
        }
    }

    async private Task AddPostTasksAsync(bool checkUpdate, CancellationToken token)
    {
        TaskQueue.Push(CreateMFATask("结束脚本", async () =>
        {
            await TaskManager.RunTaskAsync(() => Instances.RootView.RunScript("Post-script"), token);
        }));

        if (checkUpdate)
        {
            TaskQueue.Push(CreateMFATask("检查更新", async () =>
            {
                VersionChecker.Check();
            }));
        }
    }
    private MFATask CreateMaaFWTask(string? name, Func<Task> action, int count = 1)
    {
        return new MFATask
        {
            Name = name,
            Count = count,
            Type = MFATask.MFATaskType.MAAFW,
            Action = action
        };
    }

    private MFATask CreateMFATask(string? name, Func<Task> action)
    {
        return new MFATask
        {
            Name = name,
            Type = MFATask.MFATaskType.MFA,
            Action = action
        };
    }

    async public Task MeasureScreencapPerformanceAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await MeasureExecutionTimeAsync(async () =>
            _currentTasker?.Controller.Screencap().Wait());
    }

    async private Task<bool> TryConnectAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var instance = await GetCurrentTaskerAsync(token);
        return instance is { Initialized: true };
    }

    async private Task TryRunTasksAsync(MaaTasker? maa, string? task, string? param, CancellationToken token)
    {
        if (maa == null || task == null) return;

        var job = maa.AppendTask(task, param ?? "{}");
        await TaskManager.RunTaskAsync(() => job.Wait().ThrowIfNot(MaaJobStatus.Succeeded), token, catchException: true, shouldLog: false);
    }

    public void Stop(bool finished = false, bool onlyStart = false)
    {
        try
        {
            if (!ShouldProcessStop(finished))
            {
                GrowlHelper.Warning("NoTaskToStop".ToLocalization());

                ClearTaskQueue();
                return;
            }

            CancelOperations();

            ClearTaskQueue();

            ExecuteStopCore(finished, () =>
            {
                var stopResult = AbortCurrentTasker();
                HandleStopResult(finished, stopResult, onlyStart);
            });

        }
        catch (Exception ex)
        {
            HandleStopException(ex);
        }
    }

    #region Stop Helpers

    private void CancelOperations()
    {
        _emulatorCancellationTokenSource?.SafeCancel();
        CancellationTokenSource.SafeCancel();
    }

    private bool ShouldProcessStop(bool finished)
    {
        return (CancellationTokenSource?.IsCancellationRequested).IsFalse()
            || finished;
    }

    private void ExecuteStopCore(bool finished, Action stopAction)
    {
        TaskManager.RunTaskAsync(() =>
        {
            if (!finished)
                RootView.AddLogByKey("Stopping");

            stopAction.Invoke();

            Instances.RootViewModel.SetIdle(true);
        }, null, "停止任务");
    }

    private bool AbortCurrentTasker()
    {
        return _currentTasker == null || _currentTasker.Abort().Wait() == MaaJobStatus.Succeeded;
    }

    private void HandleStopResult(bool finished, bool success, bool onlyStart)
    {
        if (success)
        {
            DisplayTaskCompletionMessage(finished, onlyStart);
        }
        else
        {
            GrowlHelper.Error("StoppingFailed".ToLocalization());
        }
    }

    private void ClearTaskQueue()
    {
        TaskQueue.Clear();
        OnTaskQueueChanged();
    }

    private void HandleStopException(Exception ex)
    {
        LoggerService.LogError($"Stop operation failed: {ex.Message}");
        GrowlHelper.Error("StopOperationFailed".ToLocalization());
    }

    #endregion


    public void HandleAfterTaskOperation()
    {
        var afterTask = ConfigurationHelper.GetValue(ConfigurationKeys.AfterTask, "None");
        switch (afterTask)
        {
            case "CloseMFA":
                CloseMFA();
                break;
            case "CloseEmulator":
                CloseSoftware();
                break;
            case "CloseEmulatorAndMFA":
                CloseSoftwareAndMFA();
                break;
            case "ShutDown":
                ShutDown();
                break;
            case "CloseEmulatorAndRestartMFA":
                CloseSoftwareAndRestartMFA();
                break;
            case "RestartPC":
                Restart();
                break;
        }
    }


    private CancellationTokenSource? _emulatorCancellationTokenSource;

    private static Process? _softwareProcess;

    public async Task StartSoftware()
    {
        _emulatorCancellationTokenSource = new CancellationTokenSource();
        await StartRunnableFile(ConfigurationHelper.GetValue(ConfigurationKeys.SoftwarePath, string.Empty),
            ConfigurationHelper.GetValue(ConfigurationKeys.WaitSoftwareTime, 60.0), _emulatorCancellationTokenSource.Token);
    }

    async private Task StartRunnableFile(string exePath, double waitTimeInSeconds, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return;
        var processName = Path.GetFileNameWithoutExtension(exePath);
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            CreateNoWindow = false
        };
        if (Process.GetProcessesByName(processName).Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty)))
            {
                startInfo.Arguments = ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty);
                _softwareProcess =
                    Process.Start(startInfo);
            }
            else
                _softwareProcess = Process.Start(startInfo);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty)))
            {
                startInfo.Arguments = ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty);
                _softwareProcess = Process.Start(startInfo);
            }
            else
                _softwareProcess = Process.Start(startInfo);
        }

        for (double remainingTime = waitTimeInSeconds + 1; remainingTime > 0; remainingTime -= 1)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (remainingTime % 10 == 0)
            {
                RootView.AddLogByKey("WaitSoftwareTime", null, true,
                    Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb
                        ? "Emulator"
                        : "Window",
                    remainingTime.ToString()
                );
            }

            await Task.Delay(1000, token);
        }

    }

    private static string GetCommandLine(Process process)
    {
        return GetCommandLine(process.Id);
    }

    private static string GetCommandLine(int processId)
    {
        var commandLine = string.Empty;

        // 使用 WMI 查询命令行参数
        var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}";
        using var searcher = new ManagementObjectSearcher(query);

        foreach (var obj in searcher.Get())
        {
            commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
        }

        return commandLine;
    }

    public static void CloseSoftware(Action? action = null)
    {
        if (Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb)
        {
            EmulatorHelper.KillEmulatorModeSwitcher();
        }
        else
        {
            if (_softwareProcess != null && !_softwareProcess.HasExited)
            {
                _softwareProcess.Kill();
            }
            else
            {
                CloseProcessesByName(MaaFwConfiguration.DesktopWindow.Name, ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty));
                _softwareProcess = null;
            }

        }
        action?.Invoke();
    }

    private static void CloseProcessesByName(string processName, string emulatorConfig)
    {
        var processes = Process.GetProcesses().Where(p => p.ProcessName.StartsWith(processName));
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
                LoggerService.LogInfo($"Error closing process: {ex.Message}");
            }
        }
    }

    public static void CloseMFA()
    {
        DispatcherHelper.RunOnMainThread(Application.Current.Shutdown);
    }


    public static void CloseSoftwareAndMFA()
    {
        CloseSoftware(CloseMFA);
    }

    public static void ShutDown()
    {
        CloseSoftware();
        Process.Start("shutdown", "/s /t 0");
    }


    public static void RestartMFA(bool noAutoStart = false)
    {
        if (noAutoStart)
            GlobalConfiguration.SetValue(ConfigurationKeys.NoAutoStart, bool.TrueString);
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
        DispatcherHelper.RunOnMainThread(Application.Current.Shutdown);
    }

    public static void Restart()
    {
        CloseSoftware();
        Process.Start("shutdown", "/r /t 0");
    }

    public static void CloseSoftwareAndRestartMFA()
    {
        CloseSoftware();
        RestartMFA();
    }

    private TaskAndParam CreateTaskAndParam(DragItemViewModel task)
    {
        var taskModels = task.InterfaceItem?.PipelineOverride ?? new Dictionary<string, TaskModel>();

        UpdateTaskDictionary(ref taskModels, task.InterfaceItem?.Option);

        var taskParams = SerializeTaskParams(taskModels);
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        var json = JsonConvert.SerializeObject(Instances.TaskQueueView.BaseTasks, settings);

        var tasks = JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(json, settings);
        tasks = tasks.MergeTaskModels(taskModels);
        return new TaskAndParam
        {
            Name = task.InterfaceItem?.Name,
            Entry = task.InterfaceItem?.Entry,
            Count = task.InterfaceItem?.Repeatable == true ? (task.InterfaceItem?.RepeatCount ?? 1) : 1,
            Tasks = tasks,
            Param = taskParams
        };
    }

    private void UpdateTaskDictionary(ref Dictionary<string, TaskModel> taskModels,
        List<MaaInterface.MaaInterfaceSelectOption>? options)
    {
        Instances.TaskQueueView.TaskDictionary = Instances.TaskQueueView.TaskDictionary.MergeTaskModels(taskModels);

        if (options == null) return;

        foreach (var selectOption in options)
        {
            if (MaaInterface.Instance?.Option?.TryGetValue(selectOption.Name ?? string.Empty,
                    out var interfaceOption)
                == true
                && selectOption.Index is int index
                && interfaceOption.Cases is { } cases
                && cases[index]?.PipelineOverride != null)
            {
                var param = interfaceOption.Cases[selectOption.Index.Value].PipelineOverride;
                Instances.TaskQueueView.TaskDictionary = Instances.TaskQueueView.TaskDictionary.MergeTaskModels(param);
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
        catch (Exception)
        {
            return "{}";
        }
    }

    static void MeasureExecutionTime(Action methodToMeasure)
    {
        var stopwatch = Stopwatch.StartNew();

        methodToMeasure();

        stopwatch.Stop();
        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        switch (elapsedMilliseconds)
        {
            case >= 800:
                RootView.AddLogByKey("ScreencapErrorTip", BrushConverterHelper.ConvertToBrush("DarkGoldenrod"), false, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            case >= 400:
                RootView.AddLogByKey("ScreencapWarningTip", BrushConverterHelper.ConvertToBrush("DarkGoldenrod"), false, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            default:
                RootView.AddLogByKey("ScreencapCost", null, false, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;
        }
    }
    async static Task MeasureExecutionTimeAsync(Func<Task> methodToMeasure)
    {
        const int sampleCount = 4;
        long totalElapsed = 0;

        long min = 10000;
        long max = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            await methodToMeasure();
            sw.Stop();
            min = Math.Min(min, sw.ElapsedMilliseconds);
            max = Math.Max(max, sw.ElapsedMilliseconds);
            totalElapsed += sw.ElapsedMilliseconds;
        }

        var avgElapsed = totalElapsed / sampleCount;

        switch (avgElapsed)
        {
            case >= 800:
                RootView.AddLogByKey("ScreencapErrorTip", BrushConverterHelper.ConvertToBrush("DarkGoldenrod"), false, avgElapsed.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            case >= 400:
                RootView.AddLogByKey("ScreencapWarningTip", BrushConverterHelper.ConvertToBrush("DarkGoldenrod"), false, avgElapsed.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            default:
                RootView.AddLogByKey("ScreencapCost", null, false, avgElapsed.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;
        }
    }


    async private Task<bool> ExecuteTasks(CancellationToken token)
    {
        while (TaskQueue.Count > 0 && !token.IsCancellationRequested)
        {
            var task = TaskQueue.Dequeue();
            if (!await task.Run(token))
            {
                return false;
            }
            OnTaskQueueChanged();
        }
        return !token.IsCancellationRequested; // 根据取消状态返回正确结果
    }

    private void DisplayTaskCompletionMessage(bool finished, bool onlyStart = false)
    {
        if (!finished)
        {
            Growl.Info("TaskStopped".ToLocalization());
            RootView.AddLogByKey("TaskAbandoned");
        }
        else
        {
            ToastNotification.ShowDirect("TaskCompleted".ToLocalization());
            if (_startTime != null)
            {
                var elapsedTime = DateTime.Now - (DateTime)_startTime;
                RootView.AddLogByKey("TaskAllCompletedWithTime", null, true, ((int)elapsedTime.TotalHours).ToString(),
                    ((int)elapsedTime.TotalMinutes % 60).ToString(), ((int)elapsedTime.TotalSeconds % 60).ToString());
            }
            else
            {
                RootView.AddLogByKey("TaskAllCompleted");
            }
            if (!onlyStart)
            {
                ExternalNotificationHelper.ExternalNotificationAsync();
                HandleAfterTaskOperation();
            }
        }

        _startTime = null;
    }

    public void OnTaskQueueChanged()
    {
        TaskStackChanged?.Invoke(this, EventArgs.Empty);
    }

    public MaaTasker? GetCurrentTasker(CancellationToken token = default)
    {
        var task = GetCurrentTaskerAsync(token);
        task.Wait(token);
        return task.Result;
    }

    public async Task<MaaTasker?> GetCurrentTaskerAsync(CancellationToken token = default)
    {
        _currentTasker ??= await InitializeMaaTasker(token);
        return _currentTasker;
    }

    public bool HasTasker()
    {
        return _currentTasker != null;
    }

    public void SetCurrentTasker(MaaTasker? tasker = null)
    {
        if (tasker == null)
        {
            _agentClient?.LinkStop();
            _agentClient?.Dispose();
            _agentClient = null;
        }
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

    async private Task<MaaTasker?> InitializeMaaTasker(CancellationToken token) // 添加 async 和 token
    {

        AutoInitDictionary.Clear();
        LoggerService.LogInfo("LoadingResources".ToLocalization());

        MaaResource maaResource = null;
        try
        {
            var resources = Instances.GameSettingsUserControlModel.CurrentResources
                    .FirstOrDefault(c => c.Name == Instances.GameSettingsUserControlModel.CurrentResource)?.Path
                ?? [];
            LoggerService.LogInfo($"Resource: {string.Join(",", resources)}");


            maaResource = await TaskManager.RunTaskAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                return new MaaResource(resources);
            }, token, catchException: true, shouldLog: false, handleError: exception => HandleInitializationError(exception, "LoadResourcesFailed".ToLocalization()));

            maaResource.SetOptionInferenceDevice(Instances.PerformanceUserControlModel.GpuOption);
            LoggerService.LogInfo($"GPU acceleration: {Instances.PerformanceUserControlModel.GpuOption}");
        }
        catch (OperationCanceledException)
        {
            LoggerService.LogWarning("Resource loading was canceled");
            return null;
        }
        catch (Exception)
        {
            return null;
        }

        // 初始化控制器部分同理
        MaaController controller = null;
        try
        {
            controller = await TaskManager.RunTaskAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                return InitializeController(Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb);
            }, token, catchException: true, shouldLog: false, handleError: exception => HandleInitializationError(exception,
                "ConnectingEmulatorOrWindow".ToLocalization()
                    .FormatWith(Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb
                        ? "Emulator".ToLocalization()
                        : "Window".ToLocalization()), true,
                "InitControllerFailed".ToLocalization()));
        }
        catch (OperationCanceledException)
        {
            LoggerService.LogWarning("Controller initialization was canceled");
            return null;
        }
        catch (Exception)
        {
            return null;
        }
        try
        {
            token.ThrowIfCancellationRequested();


            var tasker = new MaaTasker
            {
                Controller = controller,
                Resource = maaResource,
                Utility = MaaUtility,
                Toolkit = MaaToolkit,
                DisposeOptions = DisposeOptions.All,
            };

            // 获取代理配置（假设MaaInterface.Instance在UI线程中访问）
            // 使用WPF日志框架记录（需实现ILogger接口）

            var agentConfig = MaaInterface.Instance?.Agent;
            if (agentConfig != null)
            {
                RootView.AddLogByKey("StartingAgent");
                _agentClient = new MaaAgentClient
                {
                    Resource = maaResource,
                    DisposeOptions = DisposeOptions.All
                };
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var identifier = string.IsNullOrWhiteSpace(MaaInterface.Instance?.Agent?.Identifier) ? new string(Enumerable.Repeat(chars, 8).Select(c => c[Random.Next(c.Length)]).ToArray()) : MaaInterface.Instance.Agent.Identifier;
                var socket = _agentClient.CreateSocket(identifier);
                if (string.IsNullOrWhiteSpace(socket))
                {
                    throw new Exception("Socket creation failed");
                }
                if (agentConfig?.ChildExec != null)
                {
                    try
                    {
                        if (!Directory.Exists($"{AppContext.BaseDirectory}"))
                            Directory.CreateDirectory($"{AppContext.BaseDirectory}");
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = MaaInterface.ReplacePlaceholder(agentConfig.ChildExec, AppContext.BaseDirectory),
                            WorkingDirectory = $"{AppContext.BaseDirectory}",
                            Arguments = $"{string.Join(" ", MaaInterface.ReplacePlaceholder(agentConfig.ChildArgs ?? Enumerable.Empty<string>(), AppContext.BaseDirectory))} {socket}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        TaskManager.RunTaskAsync(() => Process.Start(startInfo), token);

                        // 使用WPF日志框架记录（需实现ILogger接口）
                        LoggerService.LogInfo($"Agent启动: {agentConfig.ChildExec} {string.Join(" ", MaaInterface.ReplacePlaceholder(agentConfig.ChildArgs ?? Enumerable.Empty<string>(), AppContext.BaseDirectory))} {socket} "
                            + $"socket_id: {socket}");
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError($"Agent启动失败: {ex.Message}");
                    }
                }
                _agentClient?.LinkStart();
            }
            RegisterCustomRecognitionsAndActions(tasker);
            Instances.ConnectingViewModel.SetConnected(true);
            tasker.Utility.SetOptionRecording(ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.Recording, false));
            tasker.Utility.SetOptionSaveDraw(ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.SaveDraw, false));
            tasker.Utility.SetOptionShowHitDraw(ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.ShowHitDraw, false));
            return tasker;
        }
        catch (OperationCanceledException)
        {
            LoggerService.LogWarning("Tasker initialization was canceled");
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // private async Task<MaaTasker?> InitializeMaaTasker(CancellationToken token)
    // {
    //     token.ThrowIfCancellationRequested();
    //     AutoInitDictionary.Clear();
    //
    //     LoggerService.LogInfo("LoadingResources".ToLocalization());
    //     MaaResource maaResource = null;
    //     try
    //     {
    //         var resources = Instances.GameSettingsUserControlModel.CurrentResources.FirstOrDefault(c => c.Name == Instances.GameSettingsUserControlModel.CurrentResource)?.Path ?? [];
    //         LoggerService.LogInfo($"Resource: {string.Join(",", resources)}");
    //         maaResource = new MaaResource(resources);
    //
    //         maaResource.SetOptionInferenceDevice(Instances.PerformanceUserControlModel.GpuOption);
    //         LoggerService.LogInfo($"GPU acceleration: {Instances.PerformanceUserControlModel.GpuOption}");
    //     }
    //     catch (Exception e)
    //     {
    //         HandleInitializationError(e, "LoadResourcesFailed".ToLocalization());
    //         return null;
    //     }
    //
    //     LoggerService.LogInfo("InitResourcesSuccess".ToLocalization());
    //     LoggerService.LogInfo("LoadingController".ToLocalization());
    //     MaaController controller = null;
    //     try
    //     {
    //         controller = InitializeController();
    //     }
    //     catch (Exception e)
    //     {
    //         HandleInitializationError(e,
    //             "ConnectingEmulatorOrWindow".ToLocalization()
    //                 .FormatWith(Instances.ConnectingViewModel.CurrentController == MaaControllerTypes.Adb
    //                     ? "Emulator".ToLocalization()
    //                     : "Window".ToLocalization()), true,
    //             "InitControllerFailed".ToLocalization());
    //         return null;
    //     }
    //
    //     LoggerService.LogInfo("InitControllerSuccess".ToLocalization());
    //
    //
    // }

    private MaaController InitializeController(bool isAdb)
    {
        if (isAdb)
        {
            LoggerService.LogInfo($"AdbPath: {MaaFwConfiguration.AdbDevice.AdbPath}");
            LoggerService.LogInfo($"AdbSerial: {MaaFwConfiguration.AdbDevice.AdbSerial}");
            LoggerService.LogInfo($"ScreenCap: {MaaFwConfiguration.AdbDevice.ScreenCap}");
            LoggerService.LogInfo($"Input: {MaaFwConfiguration.AdbDevice.Input}");
            LoggerService.LogInfo($"Config: {MaaFwConfiguration.AdbDevice.Config}");
        }
        else
        {
            LoggerService.LogInfo($"HWnd: {MaaFwConfiguration.DesktopWindow.HWnd}");
            LoggerService.LogInfo($"ScreenCap: {MaaFwConfiguration.DesktopWindow.ScreenCap}");
            LoggerService.LogInfo($"Input: {MaaFwConfiguration.DesktopWindow.Input}");
            LoggerService.LogInfo($"Link: {MaaFwConfiguration.DesktopWindow.Link}");
            LoggerService.LogInfo($"Check: {MaaFwConfiguration.DesktopWindow.Check}");
        }
        return isAdb
            ? new MaaAdbController(
                MaaFwConfiguration.AdbDevice.AdbPath,
                MaaFwConfiguration.AdbDevice.AdbSerial,
                MaaFwConfiguration.AdbDevice.ScreenCap, MaaFwConfiguration.AdbDevice.Input,
                !string.IsNullOrWhiteSpace(MaaFwConfiguration.AdbDevice.Config) ? MaaFwConfiguration.AdbDevice.Config : "{}")
            : new MaaWin32Controller(
                MaaFwConfiguration.DesktopWindow.HWnd,
                MaaFwConfiguration.DesktopWindow.ScreenCap, MaaFwConfiguration.DesktopWindow.Input,
                MaaFwConfiguration.DesktopWindow.Link,
                MaaFwConfiguration.DesktopWindow.Check);
    }

    private static List<MetadataReference>? _metadataReferences;

    private static List<MetadataReference> GetMetadataReferences()
    {
        if (_metadataReferences == null)
        {
            var domainAssemblys = AppDomain.CurrentDomain.GetAssemblies();
            _metadataReferences = new List<MetadataReference>();

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
                        _metadataReferences.Add(metadataReference);
                    }
                }
            }

            unsafe
            {
                typeof(System.Linq.Expressions.Expression).Assembly.TryGetRawMetadata(out byte* blob, out int length);
                _metadataReferences.Add(AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length)).GetReference());
            }
        }
        return _metadataReferences;
    }


    private static bool _shouldLoadCustomClasses = true;
    private static FileSystemWatcher? _watcher;
    private static void onFileChanged(object sender, FileSystemEventArgs e)
    {
        _shouldLoadCustomClasses = true;
    }
    private static IEnumerable<CustomValue<object>> LoadAndInstantiateCustomClasses(string directory, string[] interfacesToImplement)
    {
        var customClasses = new List<CustomValue<object>>();
        if (Path.Exists(directory))
        {
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher(directory);
                _watcher.Filter = "*.cs";
                _watcher.Changed += onFileChanged;
                _watcher.Created += onFileChanged;
                _watcher.Deleted += onFileChanged;
                _watcher.Renamed += onFileChanged;
                _watcher.EnableRaisingEvents = true;
            }

            var csFiles = Directory.GetFiles(directory, "*.cs");

            var references = GetMetadataReferences();

            foreach (var filePath in csFiles)
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                LoggerService.LogInfo("Trying to parse " + name);
                var code = File.ReadAllText(filePath);

                var codeLines = code.Split([
                    '\n'
                ], StringSplitOptions.RemoveEmptyEntries).ToList();

                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("DynamicAssembly")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddSyntaxTrees(syntaxTree)
                    .AddReferences(references);

                using var ms = new MemoryStream();

                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                    foreach (var diagnostic in failures)
                    {
                        // 尝试从错误诊断信息中提取行号相关内容，这里假设格式类似 "(行号, 列号)"，不同环境格式可能不同，需按需调整
                        var lineInfo = diagnostic.Location.GetLineSpan().StartLinePosition;
                        var lineNumber = lineInfo.Line + 1; // 通常行号从1开始计数，所以加1
                        // 根据行号获取对应的代码行内容
                        var errorLine = lineNumber <= codeLines.Count ? codeLines[lineNumber - 1].Trim() : "无法获取对应代码行（行号超出范围）";
                        LoggerService.LogError($"{diagnostic.Id}: {diagnostic.GetMessage()}  [错误行号: {lineNumber}]  [错误代码行: {errorLine}]");
                    }
                    continue;
                }

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                var instances =
                    from type in assembly.GetTypes()
                    from iface in interfacesToImplement
                    where type.GetInterfaces().Any(i => i.Name == iface)
                    let instance = Activator.CreateInstance(type)
                    where instance != null
                    select new CustomValue<object>(name, instance);

                customClasses.AddRange(instances);

            }
        }
        _shouldLoadCustomClasses = false;
        return customClasses;
    }

    private static IEnumerable<CustomValue<object>>? _customClasses;
    private static IEnumerable<CustomValue<object>> GetCustomClasses(string directory, string[] interfacesToImplement)
    {
        if (_customClasses == null || _shouldLoadCustomClasses)
            _customClasses = LoadAndInstantiateCustomClasses(directory, interfacesToImplement);
        else
            _customClasses.ForEach(value => LoggerService.LogInfo($"Trying to loading {value.Name}"));
        return _customClasses;
    }

    private void RegisterCustomRecognitionsAndActions(MaaTasker instance)
    {
        if (MaaInterface.Instance == null) return;
        LoggerService.LogInfo("RegisteringCustomRecognizer".ToLocalization());
        LoggerService.LogInfo("RegisteringCustomAction".ToLocalization());
        // instance.Resource.Register(new MoneyDetectRecognition());
        // instance.Resource.Register(new MoneyRecognition());
        var customClasses = GetCustomClasses($"{Resource}/custom", [
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
            if (args.Message.StartsWith(MaaMsg.Node.Action.Prefix))
            {
                if (Instances.TaskQueueView.TaskDictionary.TryGetValue(name, out var taskModel))
                {
                    DisplayFocus(taskModel, args.Message);
                }
            }
        };
    }

    private void DisplayFocus(TaskModel taskModel, string message)
    {
        switch (message)
        {
            case MaaMsg.Node.Action.Succeeded:
                if (taskModel.FocusSucceeded != null)
                {
                    for (int i = 0; i < taskModel.FocusSucceeded.Count; i++)
                    {
                        Brush brush = null;
                        var tip = taskModel.FocusSucceeded[i];
                        try
                        {
                            if (taskModel.FocusSucceededColor != null && taskModel.FocusSucceededColor.Count > i)
                                brush = BrushConverterHelper.ConvertToBrush(taskModel.FocusSucceededColor[i]);
                        }
                        catch (Exception e)
                        {
                            LoggerService.LogError(e);
                        }

                        RootView.AddLog(HandleStringsWithVariables(tip), brush);
                    }
                }
                break;
            case MaaMsg.Node.Action.Failed:
                if (taskModel.FocusFailed != null)
                {
                    for (int i = 0; i < taskModel.FocusFailed.Count; i++)
                    {
                        Brush brush = null;
                        var tip = taskModel.FocusFailed[i];
                        try
                        {
                            if (taskModel.FocusFailedColor != null && taskModel.FocusFailedColor.Count > i)
                                brush = BrushConverterHelper.ConvertToBrush(taskModel.FocusFailedColor[i]);
                        }
                        catch (Exception e)
                        {
                            LoggerService.LogError(e);
                        }

                        RootView.AddLog(HandleStringsWithVariables(tip), brush);
                    }
                }
                break;
            case MaaMsg.Node.Action.Starting:
                if (!string.IsNullOrWhiteSpace(taskModel.FocusToast))
                {
                    ToastNotification.ShowDirect(taskModel.FocusToast);
                }
                if (taskModel.FocusTip != null)
                {
                    for (int i = 0; i < taskModel.FocusTip.Count; i++)
                    {
                        Brush? brush = null;
                        var tip = taskModel.FocusTip[i];
                        try
                        {
                            if (taskModel.FocusTipColor != null && taskModel.FocusTipColor.Count > i)
                            {
                                brush = BrushConverterHelper.ConvertToBrush(taskModel.FocusTipColor[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            LoggerService.LogError(e);
                        }

                        RootView.AddLog(HandleStringsWithVariables(tip), brush);
                    }
                }
                break;
        }

    }

    private void HandleInitializationError(Exception e,
        string message,
        bool hasWarning = false,
        string waringMessage = "")
    {
        Console.WriteLine(e);
        GrowlHelper.Error(message);
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
            GrowlHelper.Error("Handle为空！");
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

    private static MaaImageBuffer? GetImage(IMaaController? maaController)
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

    public async Task RestartAdb()
    {
        var adbPath = MaaFwConfiguration.AdbDevice.AdbPath;

        if (string.IsNullOrEmpty(adbPath))
        {
            return;
        }

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false,
        };

        Process process = new Process
        {
            StartInfo = processStartInfo,
        };

        process.Start();
        await process.StandardInput.WriteLineAsync($"{adbPath} kill-server");
        await process.StandardInput.WriteLineAsync($"{adbPath} start-server");
        await process.StandardInput.WriteLineAsync("exit");
        await process.WaitForExitAsync();
    }

    public async Task ReconnectByAdb()
    {
        var adbPath = MaaFwConfiguration.AdbDevice.AdbPath;
        var address = MaaFwConfiguration.AdbDevice.AdbSerial;

        if (string.IsNullOrEmpty(adbPath))
        {
            return;
        }

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false,
        };

        var process = new Process
        {
            StartInfo = processStartInfo,
        };

        process.Start();
        await process.StandardInput.WriteLineAsync($"{adbPath} disconnect {address}");
        await process.StandardInput.WriteLineAsync("exit");
        await process.WaitForExitAsync();
    }

    public async Task HardRestartAdb()
    {
        var adbPath = MaaFwConfiguration.AdbDevice.AdbPath;
        if (string.IsNullOrEmpty(adbPath))
        {
            return;
        }

        // This allows for SQL injection, but since it is not on a real database nothing horrible would happen.
        // The following query string does what I want, but WMI does not accept it.
        // var wmiQueryString = string.Format("SELECT ProcessId, CommandLine FROM Win32_Process WHERE ExecutablePath='{0}'", adbPath);
        const string WmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
        using var searcher = new ManagementObjectSearcher(WmiQueryString);
        using var results = searcher.Get();
        var query = from p in Process.GetProcesses()
                    join mo in results.Cast<ManagementObject>()
                        on p.Id equals (int)(uint)mo["ProcessId"]
                    select new
                    {
                        Process = p,
                        Path = (string)mo["ExecutablePath"],
                    };
        foreach (var item in query)
        {
            if (item.Path != adbPath)
            {
                continue;
            }

            // Some emulators start their ADB with administrator privilege.
            // Not sure if this is necessary
            try
            {
                item.Process.Kill();
                await item.Process.WaitForExitAsync();
            }
            catch
            {
                // ignored
            }
        }

    }

    public async Task TestConnecting()
    {
        var task = GetCurrentTasker().Controller.LinkStart();
        task.Wait();
        Instances.ConnectingViewModel.SetConnected(task.Status == MaaJobStatus.Succeeded);
    }
}
