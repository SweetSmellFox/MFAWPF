using MFAWPF.Helper.Converters;
using MFAWPF.Views;
using Newtonsoft.Json;
using System.IO;

namespace MFAWPF.Helper;

public class MaaInterface
{
    public class MaaInterfaceOptionCase
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("pipeline_override")]
        public Dictionary<string, TaskModel>? PipelineOverride { get; set; }

        public override string? ToString()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(PipelineOverride, settings);
        }
    }

    public class MaaInterfaceOption
    {
        [JsonIgnore]
        public string? Name { get; set; } = string.Empty;
        [JsonProperty("cases")]
        public List<MaaInterfaceOptionCase>? Cases { get; set; }
        [JsonProperty("default_case")]
        public string? DefaultCase { get; set; }
    }

    public class MaaInterfaceSelectOption
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("index")]
        public int? Index { get; set; }

        public override string? ToString()
        {
            return Name ?? string.Empty;
        }
    }

    public class CustomExecutor
    {
        [JsonIgnore]
        public string? Name { get; set; }
        [JsonProperty("exec_path")]
        public string? ExecPath { get; set; }

        [JsonConverter(typeof(SingleOrListConverter))]
        [JsonProperty("exec_param")]
        public List<string> ExecParam { get; set; }
    }

    public class MaaCustomResource
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonConverter(typeof(SingleOrListConverter))]
        [JsonProperty("path")]
        public List<string>? Path { get; set; }
    }

    public class MaaResourceVersion
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("version")]
        public string? Version { get; set; }
        [JsonProperty("url")]
        public string? Url { get; set; }


        public override string? ToString()
        {
            return Version ?? string.Empty;
        }
    }

    public class MaaResourceControllerAdb
    {
        [JsonProperty("input")]
        public long? Input { get; set; }
        [JsonProperty("screencap")]
        public long? ScreenCap { get; set; }
        [JsonProperty("config")]
        public object? Adb { get; set; }
    }

    public class MaaResourceControllerWin32
    {
        [JsonProperty("class_regex")]
        public string? ClassRegex { get; set; }
        [JsonProperty("window_regex")]
        public string? WindowRegex { get; set; }
        [JsonProperty("input")]
        public long? Input { get; set; }
        [JsonProperty("screencap")]
        public long? ScreenCap { get; set; }
    }

    public class MaaResourceController
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("type")]
        public string? Type { get; set; }
        [JsonProperty("adb")]
        public MaaResourceControllerAdb? Adb { get; set; }
        [JsonProperty("win32")]
        public MaaResourceControllerWin32? Win32 { get; set; }
    }

    [JsonProperty("mirrorchyan_rid")]
    public string? RID { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("version")]
    [JsonConverter(typeof(MaaResourceVersionConverter))]
    public string? Version { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("custom_title")]
    public string? CustomTitle { get; set; }

    [JsonProperty("default_controller")]
    public string? DefaultController { get; set; }

    [JsonProperty("lock_controller")]
    public bool LockController { get; set; }

    [JsonProperty("controller")]
    public List<MaaResourceController>? Controller { get; set; }
    [JsonProperty("resource")]
    public List<MaaCustomResource>? Resource { get; set; }
    [JsonProperty("task")]
    public List<TaskInterfaceItem>? Task { get; set; }
    [JsonProperty("recognition")]
    public Dictionary<string, CustomExecutor>? Recognition { get; set; }
    [JsonProperty("action")]
    public Dictionary<string, CustomExecutor>? Action { get; set; }
    [JsonProperty("option")]
    public Dictionary<string, MaaInterfaceOption>? Option { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    private static MaaInterface? _instance;

    // [JsonIgnore] public List<MaaCustomRecognizerExecutor> CustomRecognizerExecutors { get; } = new();
    //
    // [JsonIgnore] public List<MaaCustomActionExecutor> CustomActionExecutors { get; } = new();

    [JsonIgnore]
    public Dictionary<string, List<string>> Resources { get; } = new();

    // 替换单个字符串中的 "{PROJECT_DIR}" 为指定的替换值
    public static string? ReplacePlaceholder(string? input, string? replacement)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : Path.GetFullPath(input.Replace("{PROJECT_DIR}", replacement));
    }

    // 替换字符串列表中的每个字符串中的 "{PROJECT_DIR}"
    public static List<string> ReplacePlaceholder(List<string>? inputs, string? replacement)
    {
        if (inputs == null) return new List<string>();

        return inputs.ConvertAll(input => ReplacePlaceholder(input, replacement));
    }

    public static MaaInterface? Instance
    {
        get => _instance;
        set
        {
            _instance = value;
            if (value == null) return;

            // _instance?.CustomRecognizerExecutors.Clear();
            // _instance?.CustomActionExecutors.Clear();
            _instance?.Resources.Clear();

            // if (value.recognizer != null)
            // {
            //     foreach (var customExecutor in value.recognizer)
            //     {
            //         _instance?.CustomRecognizerExecutors.Add(new Maacustom
            //         {
            //             Name = customExecutor.Key,
            //             Path = ReplacePlaceholder(customExecutor.Value.exec_path, MaaProcessor.Resource),
            //             Parameter = ReplacePlaceholder(customExecutor.Value.exec_param, MaaProcessor.Resource)
            //         });
            //     }
            // }
            //
            // if (value.action != null)
            // {
            //     foreach (var customExecutor in value.action)
            //     {
            //         _instance?.CustomActionExecutors.Add(new MaaCustomActionExecutor
            //         {
            //             Name = customExecutor.Key,
            //             Path = ReplacePlaceholder(customExecutor.Value.exec_path, MaaProcessor.Resource),
            //             Parameter = ReplacePlaceholder(customExecutor.Value.exec_param, MaaProcessor.Resource)
            //         });
            //     }
            // }

            if (value.Resource != null)
            {
                foreach (var customResource in value.Resource)
                {
                    var paths = ReplacePlaceholder(customResource.Path ?? new List<string>(),
                        AppContext.BaseDirectory);
                    if (_instance != null)
                        _instance.Resources[customResource.Name ?? string.Empty] = paths;
                }
            }


            if (value.Name != null)
                MainWindow.Instance?.ShowResourceName(value.Name);
            if (value.Version != null)
                MainWindow.Instance?.ShowResourceVersion(value.Version);
            if (value.CustomTitle != null)
                MainWindow.Instance?.ShowCustomTitle(value.CustomTitle);
        }
    }

    public override string? ToString()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        return JsonConvert.SerializeObject(this, settings);
    }

    public static void Check()
    {
        if (!File.Exists($"{AppContext.BaseDirectory}/interface.json"))
        {
            LoggerService.LogInfo("未找到interface文件，生成interface.json...");
            Instance = new MaaInterface
                {
                    Version = "1.0",
                    Name = "Debug",
                    Task = [],
                    Resource =
                    [
                        new MaaInterface.MaaCustomResource
                        {
                            Name = "默认",
                            Path =
                            [
                                "{PROJECT_DIR}/resource/base",
                            ],
                        },
                    ],
                    Controller =
                    [
                        new MaaInterface.MaaResourceController()
                        {
                            Name = "adb 默认方式",
                            Type = "adb"
                        },
                    ],
                    Option = new Dictionary<string, MaaInterface.MaaInterfaceOption>
                    {
                        {
                            "测试", new MaaInterface.MaaInterfaceOption()
                            {
                                Cases =
                                [

                                    new MaaInterface.MaaInterfaceOptionCase
                                    {
                                        Name = "测试1",
                                        PipelineOverride = new Dictionary<string, TaskModel>()
                                    },
                                    new MaaInterface.MaaInterfaceOptionCase
                                    {
                                        Name = "测试2",
                                        PipelineOverride = new Dictionary<string, TaskModel>()
                                    }
                                ]
                            }
                        }
                    }
                }
                ;
            JsonHelper.WriteToJsonFilePath(AppContext.BaseDirectory, "interface",
                Instance, new MaaInterfaceSelectOptionConverter(true));
        }
        else
        {
            Instance =
                JsonHelper.ReadFromJsonFilePath(AppContext.BaseDirectory, "interface",
                    new MaaInterface(),
                    () => { }, new MaaInterfaceSelectOptionConverter(false));
        }
    }
}
