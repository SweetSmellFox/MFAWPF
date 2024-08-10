using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MaaFramework.Binding.Messages;
using MFAWPF.Actions;
using MFAWPF.Data;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MFAWPF.Utils
{
    public class MaaProcessor
    {
        public static string Resource = AppDomain.CurrentDomain.BaseDirectory + "Resource";
        public static string ModelResource = $"{Resource}/model/";
        public Stack<ICommand> TaskStack { get; } = new Stack<ICommand>();
        public event EventHandler TaskStackChanged;
        private static MaaProcessor _instance;
        public static int Money { get; set; }
        public static int AllMoney { get; set; }

        public static MaaProcessor Instance
        {
            get => _instance ??= new MaaProcessor();
            set => _instance = value;
        }

        public static Config Config { get; } = new Config();
        public static string AdbConfigFile => $"{Resource}/controller_config.json";
        public static string AdbConfigFileFullPath => Path.GetFullPath(AdbConfigFile);
        public static string ResourceBase => $"{Resource}/base";
        public static string ResourcePipelineFilePath => $"{ResourceBase}/pipeline/";
        public static string AdbConfig { get; set; }

        public static List<string>? CurrentResources { get; set; }
        public static AutoInitDictionary AutoInitDictionary { get; } = new();
        private Action _action;

        public MaaProcessor()
        {
            AdbConfig = File.ReadAllText(AdbConfigFileFullPath);
        }

        private class TaskAndParam
        {
            public string Name { get; set; }
            public string Entry { get; set; }
            public int? Count { get; set; }
            public string Param { get; set; }
        }


        public void Start(List<DragItemViewModel> tasks)
        {
            if (!Config.IsConnected)
            {
                Growls.Warning($"无法连接至{(MainWindow.Instance.IsADB ? "模拟器" : "窗口")}");
                return;
            }

            var taskAndParams = tasks.Select(task =>
            {
                var taskModels = task.InterfaceItem.param ?? new Dictionary<string, TaskModel>();
                if (MainWindow.Instance.TaskDictionary != null)
                    MainWindow.Instance.TaskDictionary = MainWindow.Instance.TaskDictionary.MergeTaskModels(taskModels);

                if (task.InterfaceItem.option != null)
                {
                    foreach (var selectOption in task.InterfaceItem.option)
                    {
                        if (MaaInterface.Instance.option.TryGetValue(selectOption.name, out var interfaceOption) &&
                            selectOption.index != null &&
                            interfaceOption.cases[selectOption.index.Value] != null &&
                            interfaceOption.cases[selectOption.index.Value].param != null)
                        {
                            // taskModels = taskModels.Concat(interfaceOption.cases[selectOption.index.Value].param)
                            //     .ToDictionary(pair => pair.Key, pair => pair.Value);
                            if (MainWindow.Instance.TaskDictionary != null)
                                MainWindow.Instance.TaskDictionary =
                                    MainWindow.Instance.TaskDictionary.MergeTaskModels(interfaceOption
                                        .cases[selectOption.index.Value].param);
                            taskModels =
                                taskModels.MergeTaskModels(interfaceOption.cases[selectOption.index.Value].param);
                        }
                    }
                }

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                string taskParms;
                try
                {
                    taskParms = JsonConvert.SerializeObject(taskModels, settings);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    taskParms = "{}";
                }

                if (string.IsNullOrWhiteSpace(taskParms))
                    taskParms = "{}";

                return new TaskAndParam
                {
                    Name = task.InterfaceItem?.name,
                    Entry = task.InterfaceItem?.entry,
                    Count = task.InterfaceItem?.repeatable == true ? (task.InterfaceItem?.repeat_count ?? 1) : 1,
                    Param = taskParms
                };
            }).ToList();

            CurrentInstance = null;

            TaskManager.RunTaskAsync(() =>
            {
                MainWindow.Data.Idle = false;
                MainWindow.Data.AddLog($"正在连接至{(MainWindow.Instance.IsADB ? "模拟器" : "窗口")}......");

                if (!CurrentInstance.Initialized)
                {
                    Growls.ErrorGlobal("初始化MAA实例失败，发生连接错误或资源文件损坏，请参阅日志.");
                    LoggerService.Logger.LogWarning("初始化MAA实例失败!");
                    MainWindow.Data.AddLog("MAA实例初始化失败");
                    Stop(false);
                }
                else
                {
                    foreach (var task in taskAndParams)
                    {
                        for (int i = 0; i < task.Count; i++)
                        {
                            MainWindow.Data.AddLog($"开始任务: {task.Name}");
                            if (!TryRunTasks(CurrentInstance, task.Entry, task.Param))
                            {
                                if (!isStopped)
                                {
                                    MainWindow.Data.AddLog($"任务出错: {task.Name}", Brushes.Red);
                                    Growls.ErrorGlobal("任务失败!");
                                    isStopped = true;
                                }

                                break;
                            }
                        }
                    }

                    Stop(false);
                }
            }, () => Stop(false), "开始任务");

            TaskStack.Push(new RelayCommand(_ =>
            {
                TaskManager.RunTaskAsync(() =>
                {
                    MainWindow.Data.AddLog("正在停止......");
                    if (CurrentInstance.Abort())
                    {
                        Growl.Info("任务已中止");
                        MainWindow.Data.AddLog("已放弃本次任务");
                        MainWindow.Data.Idle = true;
                    }
                    else
                    {
                        Growls.ErrorGlobal("停止任务失败");
                    }
                }, null, "结束任务");
            }));

            OnTaskStackChanged();
        }

        private bool isStopped = false;

        public void Stop(bool setIsStopped = true)
        {
            if (TaskStack.Count > 0)
            {
                isStopped = setIsStopped;

                var undoCommand = TaskStack.Pop();
                undoCommand.Execute(null);
                OnTaskStackChanged();
            }
            else
            {
                if (setIsStopped)
                {
                    Growls.Warning("当前没有可结束的任务!");
                    OnTaskStackChanged();
                }
            }
        }

        protected virtual void OnTaskStackChanged()
        {
            TaskStackChanged?.Invoke(this, EventArgs.Empty);
        }

        private MaaInstance currentInstance;

        public MaaInstance CurrentInstance
        {
            get => currentInstance ??= GetMaaInstance();
            set => currentInstance = value;
        }

        public static string HandlingStringsWithVariables(string content)
        {
            try
            {
                string updatedContent = Regex.Replace(content, @"\{(\+\+|\-\-)?(\w+)(\+\+|\-\-)?([\+\-\*/]\w+)?\}",
                    match =>
                    {
                        string prefix = match.Groups[1].Value;
                        string counterKey = match.Groups[2].Value;
                        string suffix = match.Groups[3].Value;
                        string operation = match.Groups[4].Value;

                        int value = 0;
                        if (AutoInitDictionary.ContainsKey(counterKey))
                        {
                            value = AutoInitDictionary[counterKey];
                        }

                        // 处理前置操作符
                        if (prefix == "++")
                        {
                            value = ++AutoInitDictionary[counterKey];
                        }
                        else if (prefix == "--")
                        {
                            value = --AutoInitDictionary[counterKey];
                        }

                        // 处理后置操作符
                        if (suffix == "++")
                        {
                            value = AutoInitDictionary[counterKey]++;
                        }
                        else if (suffix == "--")
                        {
                            value = AutoInitDictionary[counterKey]--;
                        }

                        // 处理算术操作
                        if (!string.IsNullOrEmpty(operation))
                        {
                            string operationType = operation[0].ToString();
                            string operandKey = operation.Substring(1);

                            if (AutoInitDictionary.ContainsKey(operandKey))
                            {
                                int operandValue = AutoInitDictionary[operandKey];
                                switch (operationType)
                                {
                                    case "+":
                                        value += operandValue;
                                        break;
                                    case "-":
                                        value -= operandValue;
                                        break;
                                    case "*":
                                        value *= operandValue;
                                        break;
                                    case "/":
                                        value /= operandValue;
                                        break;
                                }
                            }
                        }

                        return value.ToString();
                    });
                return updatedContent;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return content;
            }
        }

        private MaaInstance GetMaaInstance()
        {
            AutoInitDictionary.Clear();
            MaaResource maaResource;
            LoggerService.Logger.LogInfo("正在载入资源");
            try
            {
                maaResource = new MaaResource(CurrentResources);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TaskStack.Clear();
                OnTaskStackChanged();
                MainWindow.Data.Idle = true;
                Growls.ErrorGlobal("资源加载失败！");
                LoggerService.Logger.LogError(e.ToString());
                return null;
            }

            LoggerService.Logger.LogInfo("载入资源成功");
            LoggerService.Logger.LogInfo("正在初始化控制器");
            IMaaController<nint> controller;
            try
            {
                if (MainWindow.Instance.IsADB)
                {
                    controller = new MaaAdbController(
                        Config.Adb.Adb,
                        Config.Adb.AdbAddress,
                        Config.Adb.ControlType,
                        !string.IsNullOrWhiteSpace(Config.Adb.AdbConfig) ? Config.Adb.AdbConfig : AdbConfig,
                        $"{Resource}/MaaAgentBinary",
                        LinkOption.Start,
                        CheckStatusOption.None);
                }
                else
                {
                    controller = new MaaWin32Controller(
                        Config.Win32.HWnd,
                        Config.Win32.ControlType,
                        Config.Win32.Link,
                        Config.Win32.Check);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TaskStack.Clear();
                OnTaskStackChanged();
                MainWindow.Data.Idle = true;
                Growls.ErrorGlobal($"连接{(MainWindow.Instance.IsADB ? "模拟器" : "窗口")}时发生错误！");
                LoggerService.Logger.LogWarning("初始化控制器时发生错误！");
                LoggerService.Logger.LogError(e.ToString());
                return null;
            }

            LoggerService.Logger.LogInfo("初始化控制器成功");
            CurrentInstance = new MaaInstance
            {
                Controller = controller,
                Resource = maaResource,
                DisposeOptions = DisposeOptions.All,
            };

            CurrentInstance.Register(new MoneyRecognizer());
            CurrentInstance.Register(new MoneyDetectRecognizer());
            LoggerService.Logger.LogInfo("正在注册自定义识别器");
            foreach (var VARIABLE in MaaInterface.Instance.CustomRecognizerExecutors)
            {
                Console.WriteLine($"注册自定义Recognizer:{VARIABLE.Name}");
                CurrentInstance.Toolkit.ExecAgent.Register(CurrentInstance, VARIABLE);
            }

            LoggerService.Logger.LogInfo("正在注册自定义行动");
            foreach (var VARIABLE in MaaInterface.Instance.CustomActionExecutors)
            {
                Console.WriteLine($"注册自定义Action:{VARIABLE.Name}");
                CurrentInstance.Toolkit.ExecAgent.Register(CurrentInstance, VARIABLE);
            }

            CurrentInstance.Callback += (sender, args) =>
            {
                JObject jObject = JObject.Parse(args.Details);
                string name = jObject["name"]?.ToString();
                if (args.Message.Equals(MaaMsg.Task.Focus.Completed))
                {
                    if (MainWindow.Instance.TaskDictionary.ContainsKey(name))
                    {
                        var taskModel = MainWindow.Instance.TaskDictionary[name];
                        var converter = new BrushConverter();

                        if (taskModel.focus_tip != null)
                        {
                            for (int i = 0; i < taskModel.focus_tip.Count; i++)
                            {
                                Brush brush = null;
                                var tip = taskModel.focus_tip[i];
                                try
                                {
                                    if (taskModel.focus_tip_color != null && taskModel.focus_tip_color.Count > i)
                                        brush = converter.ConvertFromString(taskModel.focus_tip_color[i]) as Brush;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    LoggerService.Logger.LogError(e);
                                }

                                MainWindow.Data.AddLog(HandlingStringsWithVariables(tip), brush);
                            }
                        }
                    }
                }
            };

            return CurrentInstance;
        }

        private static MaaImageBuffer GetImage(IMaaController maaController)
        {
            var status = maaController.Screencap().Wait();
            if (status != MaaJobStatus.Success)
                return null;
            var buffer = new MaaImageBuffer();
            return maaController.GetImage(buffer) ? buffer : null;
        }

        public BitmapImage GetBitmapImage()
        {
            using var buffer = GetImage(CurrentInstance.Controller);
            if (buffer != null)
            {
                var encodedDataHandle = buffer.GetEncodedData(out var size);
                if (encodedDataHandle == null)
                {
                    Growls.ErrorGlobal("Handle为空！");
                    return null;
                }

                var imageData = new byte[size];
                Marshal.Copy(encodedDataHandle, imageData, 0, (int)size);

                if (imageData.Length == 0)
                    return null;

                BitmapImage bitmapImage = new BitmapImage();
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                }

                bitmapImage.Freeze();
                return bitmapImage;
            }

            return null;
        }

        private bool TryRunTasks(MaaInstance maa, string task, string taskParams)
        {
            var status = maa.AppendTask(task, taskParams).Wait();
            if (status != MaaJobStatus.Success)
            {
                Stop(false);
                return false;
            }

            return true;
        }
    }
}