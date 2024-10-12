using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

namespace MFAWPF.Utils;

public class MaaProcessor
{
    private static MaaProcessor? _instance;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isStopped;
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

        tasks ??= new List<DragItemViewModel>();
        var taskAndParams = tasks.Select(CreateTaskAndParam).ToList();

        foreach (var task in taskAndParams)
            TaskQueue.Enqueue(task);
        OnTaskQueueChanged();

        SetCurrentTasker();
        if (MainWindow.Data != null)
            MainWindow.Data.Idle = false;

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
                Stop(false);
                return;
            }

            bool run = await ExecuteTasks(token);
            if (run)
                Stop(_isStopped);
        }, null, "启动任务");
    }

    public void Stop(bool setIsStopped = true)
    {
        if (_cancellationTokenSource != null)
        {
            _isStopped = setIsStopped;
            _cancellationTokenSource?.Cancel();
            TaskManager.RunTaskAsync(() =>
            {
                if (_isStopped)
                    MainWindow.Data?.AddLogByKey("Stopping");
                if (_currentTasker == null || (_currentTasker?.Abort()).IsTrue())
                {
                    DisplayTaskCompletionMessage();
                    if (MainWindow.Data != null)
                        MainWindow.Data.Idle = true;
                    HandleAfterTaskOperation();
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
        if (_isStopped) return;
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
        }
    }

    private void CloseMFA()
    {
        Growls.Process(Application.Current.Shutdown);
    }

    private void CloseEmulator()
    {
        var emulatorPath = DataSet.GetData("EmulatorPath", string.Empty);

        if (!string.IsNullOrEmpty(emulatorPath))
        {
            string processName = Path.GetFileNameWithoutExtension(emulatorPath);

            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }

    private void CloseEmulatorAndMFA()
    {
        CloseEmulator();
        CloseMFA();
    }

    private void ShutDown()
    {
        Process.Start("shutdown", "/s /t 0");
    }
    
    private void CloseEmulatorAndRestartMFA()
    {
        CloseEmulator();
        Process.Start(Process.GetCurrentProcess().MainModule.FileName);
        Growls.Process(Application.Current.Shutdown);
    }

    private void Restart()
    {
        Process.Start("shutdown", "/r /t 0");
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
                        if (_isStopped) return false;
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
        if (_isStopped)
        {
            Growl.Info("TaskStopped".GetLocalizationString());
            MainWindow.Data?.AddLogByKey("TaskAbandoned");
        }
        else
        {
            Growl.Info("TaskCompleted".GetLocalizationString());
            MainWindow.Data?.AddLogByKey("TaskAllCompleted");
        }
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
                !string.IsNullOrWhiteSpace(Config.AdbDevice.Config) && Config.AdbDevice.Config != "{}" &&
                (DataSet.GetData("AdbConfig", "{\"extras\":{}}") == "{\"extras\":{}}" ||
                 string.IsNullOrWhiteSpace(DataSet.GetData("AdbConfig", "{\"extras\":{}}")))
                    ? Config.AdbDevice.Config
                    : DataSet.GetData("AdbConfig", "{\"extras\":{}}"))
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