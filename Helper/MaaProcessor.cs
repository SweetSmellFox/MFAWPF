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
using HandyControl.Tools.Extension;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Custom;
using MaaFramework.Binding.Notification;
using MailKit.Net.Smtp;
using MailKit.Security;
using MFAWPF.Data;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using MFAWPF.Views.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using DragItemViewModel = MFAWPF.ViewModels.Tool.DragItemViewModel;


namespace MFAWPF.Helper;

public class MaaProcessor
{
    private static MaaProcessor? _instance;
    private CancellationTokenSource? _cancellationTokenSource;

    public static MaaUtility MaaUtility { get; } = new();
    public static MaaToolkit MaaToolkit { get; } = new(init: true);
    public bool IsStopped
    {
        get;
        set;
    }

    public CancellationTokenSource? CancellationTokenSource => _cancellationTokenSource;
    private MaaTasker? _currentTasker;

    public static string Resource => AppContext.BaseDirectory + "Resource";
    public static string ModelResource => $"{Resource}/model/";
    public static string ResourceBase => $"{Resource}/base";
    public static string ResourcePipelineFilePath => $"{ResourceBase}/pipeline/";

    public Queue<MFATask> TaskQueue { get; } = new();
    public static int Money { get; set; } = 0;
    public static int AllMoney { get; set; }
    public static MaaFWConfig MaaFwConfig { get; } = new();

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


    public void Start(List<DragItemViewModel>? tasks, bool onlyStart = false, bool checkUpdate = false)
    {
        SetCurrentTasker();
        Instances.RootViewModel.SetIdle(false);
        if (!onlyStart)
            TaskQueue.Push(new MFATask
            {
                Name = "启动脚本",
                Type = MFATask.MFATaskType.MFA,
                Action = () =>
                {
                    Instances.RootView.RunScript();
                }
            });

        _startTime = DateTime.Now;
        IsStopped = false;
        tasks ??= new();
        var taskAndParams = tasks.Select(CreateTaskAndParam).ToList();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        if (!onlyStart)
            TaskQueue.Push(new MFATask
            {
                Name = "计时",
                Type = MFATask.MFATaskType.MFA,
                Action = () =>
                {
                    RootView.AddLogByKey("ConnectingTo", null, Instances.RootViewModel.IsAdb
                        ? "Emulator"
                        : "Window");
                    var instance = Task.Run(GetCurrentTasker, token);
                    instance.Wait(token);
                    bool connected = instance.Result is { Initialized: true };
                    if (!connected && Instances.RootViewModel.IsAdb && MFAConfiguration.GetConfiguration("RetryOnDisconnected", false))
                    {
                        RootView.AddLog("ConnectFailed".ToLocalization() + "\n" + "TryToStartEmulator".ToLocalization());

                        StartSoftware();

                        if (token.IsCancellationRequested)
                        {
                            Stop();
                            return;
                        }
                        Instances.ConnectingView.AutoDetectDevice();

                        instance = Task.Run(GetCurrentTasker, token);
                        instance.Wait(token);
                        connected = instance.Result is { Initialized: true };

                        Instances.ConnectingView.AutoDetectDevice();
                    }


                    if (!connected && Instances.RootViewModel.IsAdb)
                    {
                        RootView.AddLog("ConnectFailed".ToLocalization() + "\n" + "TryToReconnectByAdb".ToLocalization());
                        ReconnectByAdb();

                        if (token.IsCancellationRequested)
                        {
                            Stop();
                            return;
                        }

                        Instances.RootViewModel.SetConnected(false);
                        instance = Task.Run(GetCurrentTasker, token);
                        instance.Wait(token);
                        connected = instance.Result is { Initialized: true };
                    }
                    if (!connected && Instances.RootViewModel.IsAdb && MFAConfiguration.GetConfiguration("AllowAdbRestart", true))
                    {
                        RootView.AddLog("ConnectFailed".ToLocalization() + "\n" + "RestartAdb".ToLocalization());

                        RestartAdb();

                        if (token.IsCancellationRequested)
                        {
                            Stop();
                            return;
                        }

                        instance = Task.Run(GetCurrentTasker, token);
                        instance.Wait(token);
                        connected = instance.Result is { Initialized: true };
                    }

                    // 尝试杀掉 ADB 进程
                    if (!connected && Instances.RootViewModel.IsAdb && MFAConfiguration.GetConfiguration("AllowAdbHardRestart", true))
                    {
                        RootView.AddLog("ConnectFailed".ToLocalization() + "\n" + "HardRestartAdb".ToLocalization());

                        HardRestartAdb();

                        if (token.IsCancellationRequested)
                        {
                            Stop();
                            return;
                        }

                        instance = Task.Run(GetCurrentTasker, token);
                        instance.Wait(token);
                        connected = instance.Result is { Initialized: true };
                    }

                    if (!connected)
                    {
                        LoggerService.LogWarning("ConnectFailed".ToLocalization());
                        RootView.AddLogByKey("ConnectFailed");
                        Instances.RootViewModel.SetConnected(false);
                        Stop();
                    }

                    if (connected) Instances.RootViewModel.SetConnected(true);

                    if (!Instances.RootView.IsConnected())
                    {
                        GrowlHelper.Warning("Warning_CannotConnect".ToLocalization()
                            .FormatWith(Instances.RootViewModel.IsAdb
                                ? "Emulator".ToLocalization()
                                : "Window".ToLocalization()));
                        throw new Exception();
                    }
                }
            });
        if (!onlyStart)
            TaskQueue.Push(new MFATask
            {
                Name = "计时",
                Type = MFATask.MFATaskType.MFA,
                Action = () =>
                {
                    MeasureExecutionTime(() => _currentTasker?.Controller.Screencap().Wait());
                }
            });

        if (!onlyStart)
        {
            foreach (var task in taskAndParams)
                TaskQueue.Push(new MFATask
                {
                    Name = task.Name,
                    Type = MFATask.MFATaskType.MAAFW,
                    Count = task.Count ?? 1,
                    Action = () =>
                    {
                        if (task.Tasks != null) Instances.TaskQueueView.TaskDictionary = task.Tasks;
                        TryRunTasks(_currentTasker, task.Entry, task.Param);
                    },
                });
        }
        if (!onlyStart)
            TaskQueue.Push(new MFATask
            {
                Name = "结束",
                Type = MFATask.MFATaskType.MFA,
                Action = () => { Instances.RootView.RunScript("Post-script"); }
            });
        if (checkUpdate)
            TaskQueue.Push(new MFATask
            {
                Name = "检查更新",
                Type = MFATask.MFATaskType.MFA,
                Action = () => { VersionChecker.Check(); }
            });


        TaskManager.RunTaskAsync(async () =>
        {
            var run = await ExecuteTasks(token);
            if (run)
            {
                Stop(IsStopped, onlyStart);
            }
        }, null, "启动任务");
    }

    public void Stop(bool setIsStopped = true, bool onlyStart = false)
    {

        _emulatorCancellationTokenSource?.Cancel();

        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            IsStopped = setIsStopped;
            _cancellationTokenSource?.Cancel();
            TaskManager.RunTaskAsync(() =>
            {
                if (IsStopped)
                    RootView.AddLogByKey("Stopping");
                if (_currentTasker == null || _currentTasker?.Abort().Wait() == MaaJobStatus.Succeeded)
                {
                    DisplayTaskCompletionMessage(onlyStart);
                    Instances.RootViewModel.SetIdle(true);
                }
                else
                {
                    GrowlHelper.Error("StoppingFailed".ToLocalization());
                }
            }, null, "停止任务");
            TaskQueue.Clear();
            OnTaskQueueChanged();
        }
        else
        {
            if (setIsStopped)
            {
                GrowlHelper.Warning("NoTaskToStop".ToLocalization());
                TaskQueue.Clear();
                OnTaskQueueChanged();
            }
        }
    }
    async private static Task<bool> DingTalkMessageAsync(string accessToken, string secret)
    {
        var timestamp = GetTimestamp();
        var sign = CalculateSignature(timestamp, secret);
        var message = new
        {
            msgtype = "text",
            text = new
            {
                content = "TaskAllCompleted".ToLocalization()
            }
        };

        try
        {
            var apiUrl = $"https://oapi.dingtalk.com/robot/send?access_token={accessToken}&timestamp={timestamp}&sign={sign}";
            using var client = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                LoggerService.LogInfo("Message sent successfully");
                return true;
            }

            LoggerService.LogError($"Message sending failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            return false;
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"Error sending message: {ex.Message}");
            return false;
        }
    }

    public static void SendEmail(string email, string password)
    {
        try
        {
            var smtpConfig = GetSmtpConfigByEmail(email);

            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress("", email));
            mail.To.Add(new MailboxAddress("", email));
            mail.Subject = "TaskAllCompleted".ToLocalization();
            mail.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = "TaskAllCompleted".ToLocalization()
            };

            using var client = new SmtpClient();

            client.Connect(
                smtpConfig.Host,
                smtpConfig.Port,
                smtpConfig.UseSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto
            );

            client.Authenticate(email, password);

            client.Send(mail);

            client.Disconnect(true);
        }
        catch (AuthenticationException ex)
        {
            LoggerService.LogError($"认证失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"未知错误: {ex.Message}");
        }
    }

    private static (string Host, int Port, bool UseSSL, string Notes) GetSmtpConfigByEmail(string email)
    {
        if (!email.Contains('@') || email.Split('@').Length != 2)
            throw new ArgumentException("无效的邮箱地址格式");

        string domain = email.Split('@')[1].ToLower().Trim();


        var smtpConfigs = new Dictionary<string, (string Host, int Port, bool UseSSL, string Notes)>()
        {
            // 国内邮箱
            ["qq.com"] = ("smtp.qq.com", 465, true, "需使用授权码，非QQ密码"),
            ["163.com"] = ("smtp.163.com", 465, true, "25端口可能被运营商屏蔽"),
            ["126.com"] = ("smtp.126.com", 465, true, ""),
            ["sina.com"] = ("smtp.sina.com.cn", 465, true, ""),
            ["aliyun.com"] = ("smtp.aliyun.com", 465, true, ""),


            ["gmail.com"] = ("smtp.gmail.com", 587, true, "需开启两步验证并创建应用密码"),
            ["outlook.com"] = ("smtp.office365.com", 587, true, "支持Microsoft全家桶邮箱"),
            ["hotmail.com"] = ("smtp.office365.com", 587, true, ""),
            ["live.com"] = ("smtp.office365.com", 587, true, ""),
            ["yahoo.com"] = ("smtp.mail.yahoo.com", 465, true, "2024年后需付费使用SMTP"),
            ["icloud.com"] = ("smtp.mail.me.com", 587, true, "需生成应用专用密码"),

            ["edu.cn"] = ("smtptt.[DOMAIN]", 465, true, "自动替换域名，如：smtptt.tsinghua.edu.cn"),
            ["gov.cn"] = ("mail.[DOMAIN]", 25, false, "通常使用非加密端口")
        };

        if (smtpConfigs.TryGetValue(domain, out var config))
        {
            return HandleSpecialCases(domain, config);
        }

        foreach (var key in smtpConfigs.Keys.Where(k => k.Contains('.') && !k.StartsWith(".")))
        {
            if (domain.EndsWith($".{key}"))
            {
                var customConfig = smtpConfigs[key];
                customConfig.Host = customConfig.Host.Replace("[DOMAIN]", domain);
                return customConfig;
            }
        }

        throw new Exception("Email not supported");
    }


    private static (string Host, int Port, bool UseSSL, string Notes) HandleSpecialCases(
        string domain,
        (string Host, int Port, bool UseSSL, string Notes) config)
    {
        if (domain == "163.com" || domain == "126.com")
        {
            return config with
            {
                Port = 994
            };
        }
        return config;
    }

    public async static Task ExternalNotificationAsync()
    {
        var enabledProviders = Instances.SettingsViewModel.EnabledExternalNotificationProviderList;

        foreach (var enabledProvider in enabledProviders)
        {
            switch (enabledProvider)
            {
                case "DingTalk":
                    await DingTalkMessageAsync(Instances.SettingsViewModel.DingTalkToken, Instances.SettingsViewModel.DingTalkSecret);
                    break;
                case "Email":
                    SendEmail(Instances.SettingsViewModel.EmailAccount, Instances.SettingsViewModel.EmailSecret);
                    break;
            }
        }
    }

    public void HandleAfterTaskOperation()
    {
        if (IsStopped) return;
        var afterTask = MFAConfiguration.GetConfiguration("AfterTask", "None");
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

    public void StartSoftware()
    {
        _emulatorCancellationTokenSource = new CancellationTokenSource();
        StartRunnableFile(MFAConfiguration.GetConfiguration("SoftwarePath", string.Empty),
            MFAConfiguration.GetConfiguration("WaitSoftwareTime", 60.0), _emulatorCancellationTokenSource.Token);
    }

    private void StartRunnableFile(string exePath, double waitTimeInSeconds, CancellationToken token)
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
            if (!string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty)))
            {
                startInfo.Arguments = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);
                _softwareProcess =
                    Process.Start(startInfo);
            }
            else
                _softwareProcess = Process.Start(startInfo);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty)))
            {
                startInfo.Arguments = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);
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
                RootView.AddLogByKey("WaitSoftwareTime", null,
                    Instances.RootViewModel.IsAdb
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
        using var searcher = new ManagementObjectSearcher(query);

        foreach (var obj in searcher.Get())
        {
            commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
        }

        return commandLine;
    }

    public static void CloseSoftware(Action? action = null)
    {
        if (Instances.RootViewModel.IsAdb)
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
                CloseProcessesByName(MaaFwConfig.DesktopWindow.Name, MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty));
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
            GlobalConfiguration.SetConfiguration("NoAutoStart", bool.TrueString);
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
        string sign = WebUtility.UrlEncode(base64Encoded).Replace("+", "%20").Replace("/", "%2F").Replace("=", "%3D");
        return sign;
    }

    static byte[] ComputeHmacSha256(byte[] key, byte[] data)
    {
        using var hmacsha256 = new HMACSHA256(key);
        return hmacsha256.ComputeHash(data);

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

        switch (elapsedMilliseconds)
        {
            case >= 800:
                RootView.AddLogByKey("ToScreencapErrorTip", new BrushConverter().ConvertFromString("DarkGoldenrod") as Brush, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            case >= 400:
                RootView.AddLogByKey("ScreencapWarningTip", new BrushConverter().ConvertFromString("DarkGoldenrod") as Brush, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            default:
                RootView.AddLogByKey("ScreencapCost", null, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;
        }
    }

    static async Task MeasureExecutionTimeAsync(Func<Task> methodToMeasure)
    {
        var stopwatch = Stopwatch.StartNew();

        await methodToMeasure();

        stopwatch.Stop();
        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        switch (elapsedMilliseconds)
        {
            case >= 800:
                RootView.AddLogByKey("ToScreencapErrorTip", new BrushConverter().ConvertFromString("DarkGoldenrod") as Brush, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            case >= 400:
                RootView.AddLogByKey("ScreencapWarningTip", new BrushConverter().ConvertFromString("DarkGoldenrod") as Brush, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;

            default:
                RootView.AddLogByKey("ScreencapCost", null, elapsedMilliseconds.ToString(),
                    Instances.TaskQueueView.ScreenshotType());
                break;
        }

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
            }

            OnTaskQueueChanged();
        }

        return true;
    }

    private void DisplayTaskCompletionMessage(bool onlyStart = false)
    {
        if (IsStopped)
        {
            Growl.Info("TaskStopped".ToLocalization());
            RootView.AddLogByKey("TaskAbandoned");
            IsStopped = false;
        }
        else
        {
            ToastNotification.ShowDirect("TaskCompleted".ToLocalization());
            if (_startTime != null)
            {
                var elapsedTime = DateTime.Now - (DateTime)_startTime;
                RootView.AddLogByKey("TaskAllCompletedWithTime", null, ((int)elapsedTime.TotalHours).ToString(),
                    ((int)elapsedTime.TotalMinutes % 60).ToString(), ((int)elapsedTime.TotalSeconds % 60).ToString());
            }
            else
            {
                RootView.AddLogByKey("TaskAllCompleted");
            }
            if (!onlyStart)
            {
                ExternalNotificationAsync();
                HandleAfterTaskOperation();
            }
        }

        _startTime = null;
    }

    public void OnTaskQueueChanged()
    {
        TaskStackChanged?.Invoke(this, EventArgs.Empty);
    }

    public MaaTasker GetCurrentTasker()
    {
        return _currentTasker ??= InitializeMaaTasker();
    }
    public bool HasTasker()
    {
        return _currentTasker != null;
    }
    public void SetCurrentTasker(MaaTasker tasker = null)
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

        LoggerService.LogInfo("LoadingResources".ToLocalization());
        MaaResource maaResource;
        try
        {
            var resources = Instances.TaskQueueViewModel.CurrentResources.FirstOrDefault(c => c.Name == Instances.TaskQueueViewModel.CurrentResource)?.Path ?? [];
            LoggerService.LogInfo($"Resource: {string.Join(",", resources)}");
            maaResource = new MaaResource(resources);

            maaResource.SetOptionInferenceDevice(MFAConfiguration.GetConfiguration("EnableGPU", true) ? InferenceDevice.Auto : InferenceDevice.CPU);
            LoggerService.LogInfo($"GPU acceleration: {MFAConfiguration.GetConfiguration("EnableGPU", true)}");
        }
        catch (Exception e)
        {
            HandleInitializationError(e, "LoadResourcesFailed".ToLocalization());
            return null;
        }

        LoggerService.LogInfo("InitResourcesSuccess".ToLocalization());
        LoggerService.LogInfo("LoadingController".ToLocalization());
        MaaController controller;
        try
        {
            controller = InitializeController();
        }
        catch (Exception e)
        {
            HandleInitializationError(e,
                "ConnectingEmulatorOrWindow".ToLocalization()
                    .FormatWith(Instances.RootViewModel.IsAdb
                        ? "Emulator".ToLocalization()
                        : "Window".ToLocalization()), true,
                "InitControllerFailed".ToLocalization());
            return null;
        }

        LoggerService.LogInfo("InitControllerSuccess".ToLocalization());


        try
        {
            var tasker = new MaaTasker
            {
                Controller = controller,
                Resource = maaResource,
                Utility = MaaUtility,
                Toolkit = MaaToolkit,
                DisposeOptions = DisposeOptions.All,
            };
            RegisterCustomRecognitionsAndActions(tasker);
            tasker.Utility.SetOptionRecording(MFAConfiguration.MaaConfig.GetConfig("recording", false));
            tasker.Utility.SetOptionSaveDraw(MFAConfiguration.MaaConfig.GetConfig("save_draw", false));
            tasker.Utility.SetOptionShowHitDraw(MFAConfiguration.MaaConfig.GetConfig("show_hit_draw", false));
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
        if (Instances.RootViewModel.IsAdb)
        {
            LoggerService.LogInfo($"AdbPath: {MaaFwConfig.AdbDevice.AdbPath}");
            LoggerService.LogInfo($"AdbSerial: {MaaFwConfig.AdbDevice.AdbSerial}");
            LoggerService.LogInfo($"ScreenCap: {MaaFwConfig.AdbDevice.ScreenCap}");
            LoggerService.LogInfo($"Input: {MaaFwConfig.AdbDevice.Input}");
            LoggerService.LogInfo($"Config: {MaaFwConfig.AdbDevice.Config}");
        }
        else
        {
            LoggerService.LogInfo($"HWnd: {MaaFwConfig.DesktopWindow.HWnd}");
            LoggerService.LogInfo($"ScreenCap: {MaaFwConfig.DesktopWindow.ScreenCap}");
            LoggerService.LogInfo($"Input: {MaaFwConfig.DesktopWindow.Input}");
            LoggerService.LogInfo($"Link: {MaaFwConfig.DesktopWindow.Link}");
            LoggerService.LogInfo($"Check: {MaaFwConfig.DesktopWindow.Check}");
        }
        return Instances.RootViewModel.IsAdb
            ? new MaaAdbController(
                MaaFwConfig.AdbDevice.AdbPath,
                MaaFwConfig.AdbDevice.AdbSerial,
                MaaFwConfig.AdbDevice.ScreenCap, MaaFwConfig.AdbDevice.Input,
                !string.IsNullOrWhiteSpace(MaaFwConfig.AdbDevice.Config) ? MaaFwConfig.AdbDevice.Config : "{}")
            //!string.IsNullOrWhiteSpace(Config.AdbDevice.Config) && Config.AdbDevice.Config != "{}" &&
            //(MFAConfiguration.GetConfiguration("AdbConfig", "{\"extras\":{}}") == "{\"extras\":{}}" ||
            //string.IsNullOrWhiteSpace(MFAConfiguration.GetConfiguration("AdbConfig", "{\"extras\":{}}")))
            //   ? Config.AdbDevice.Config
            //   : MFAConfiguration.GetConfiguration("AdbConfig", "{\"extras\":{}}"))
            : new MaaWin32Controller(
                MaaFwConfig.DesktopWindow.HWnd,
                MaaFwConfig.DesktopWindow.ScreenCap, MaaFwConfig.DesktopWindow.Input,
                MaaFwConfig.DesktopWindow.Link,
                MaaFwConfig.DesktopWindow.Check);
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

    private static IEnumerable<CustomValue<object>> _customClasses;
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
        var converter = new BrushConverter();
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
                                brush = BrushConverterHelper.ConvertToBrush(taskModel.FocusSucceededColor[i]) as Brush;
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
                            Console.WriteLine(e);
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

    private void TryRunTasks(MaaTasker maa, string? task, string? taskParams)
    {
        if (maa == null || task == null) throw new NullReferenceException();
        if (string.IsNullOrWhiteSpace(taskParams)) taskParams = "{}";
        maa.AppendTask(task, taskParams).Wait().ThrowIfNot(MaaJobStatus.Succeeded);
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

    public void RestartAdb()
    {
        if (!MFAConfiguration.GetConfiguration("AllowAdbRestart", false))
        {
            return;
        }

        var adbPath = MaaFwConfig.AdbDevice.AdbPath;

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
        process.StandardInput.WriteLine($"{adbPath} kill-server");
        process.StandardInput.WriteLine($"{adbPath} start-server");
        process.StandardInput.WriteLine("exit");
        process.WaitForExit();
    }

    public void ReconnectByAdb()
    {
        var adbPath = MaaFwConfig.AdbDevice.AdbPath;
        var address = MaaFwConfig.AdbDevice.AdbSerial;

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
        process.StandardInput.WriteLine($"{adbPath} disconnect {address}");
        process.StandardInput.WriteLine("exit");
        process.WaitForExit();
    }

    public void HardRestartAdb()
    {
        if (!MFAConfiguration.GetConfiguration("AllowAdbHardRestart", false))
        {
            return;
        }

        var adbPath = MaaFwConfig.AdbDevice.AdbPath;
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
                item.Process.WaitForExit();
            }
            catch
            {
                // ignored
            }
        }
    }
}
