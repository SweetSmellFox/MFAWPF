using MaaFramework.Binding.Custom;
using MFAWPF.Utils.Converters;
using MFAWPF.Views;
using Newtonsoft.Json;

namespace MFAWPF.Utils
{
    public class MaaInterface
    {
        public class MaaInterfaceOptionCase
        {
            public string? name { get; set; }
            public Dictionary<string, TaskModel>? param { get; set; }

            public override string ToString()
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };
                return JsonConvert.SerializeObject(param, settings);
            }
        }

        public class MaaInterfaceOption
        {
            [JsonIgnore] public string name { get; set; } = string.Empty;
            public List<MaaInterfaceOptionCase>? cases { get; set; }
        }

        public class MaaInterfaceSelectOption
        {
            public string? name { get; set; }
            public int? index { get; set; }
        }

        public class CustomExecutor
        {
            [JsonIgnore] public string? name { get; set; }
            public string? exec_path { get; set; }

            [JsonConverter(typeof(SingleOrListConverter))]
            public List<string>? exec_param { get; set; }
        }

        public class MaaCustomResource
        {
            public string? name { get; set; }

            [JsonConverter(typeof(SingleOrListConverter))]
            public List<string>? path { get; set; }
        }

        public class MaaResourceVersion
        {
            public string? name { get; set; }
            public string? version { get; set; }
            public string? url { get; set; }
        }

        public MaaResourceVersion? version { get; set; }

        public List<MaaCustomResource>? resource { get; set; }
        public List<TaskInterfaceItem>? task { get; set; }
        public Dictionary<string, CustomExecutor>? recognizer { get; set; }
        public Dictionary<string, CustomExecutor>? action { get; set; }
        public Dictionary<string, MaaInterfaceOption>? option { get; set; }

        private static MaaInterface? _instance;

        [JsonIgnore] public List<MaaCustomRecognizerExecutor> CustomRecognizerExecutors { get; } = new();

        [JsonIgnore] public List<MaaCustomActionExecutor> CustomActionExecutors { get; } = new();

        [JsonIgnore] public Dictionary<string, List<string>> Resources { get; } = new();

        // 替换单个字符串中的 "{PROJECT_DIR}" 为指定的替换值
        public static string ReplacePlaceholder(string? input, string replacement)
        {
            return string.IsNullOrEmpty(input) ? string.Empty : input.Replace("{PROJECT_DIR}", replacement);
        }

        // 替换字符串列表中的每个字符串中的 "{PROJECT_DIR}"
        public static List<string> ReplacePlaceholder(List<string>? inputs, string replacement)
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

                _instance?.CustomRecognizerExecutors.Clear();
                _instance?.CustomActionExecutors.Clear();
                _instance?.Resources.Clear();

                if (value.recognizer != null)
                {
                    foreach (var customExecutor in value.recognizer)
                    {
                        _instance?.CustomRecognizerExecutors.Add(new MaaCustomRecognizerExecutor
                        {
                            Name = customExecutor.Key,
                            Path = ReplacePlaceholder(customExecutor.Value.exec_path, MaaProcessor.Resource),
                            Parameter = ReplacePlaceholder(customExecutor.Value.exec_param, MaaProcessor.Resource)
                        });
                    }
                }

                if (value.action != null)
                {
                    foreach (var customExecutor in value.action)
                    {
                        _instance?.CustomActionExecutors.Add(new MaaCustomActionExecutor
                        {
                            Name = customExecutor.Key,
                            Path = ReplacePlaceholder(customExecutor.Value.exec_path, MaaProcessor.Resource),
                            Parameter = ReplacePlaceholder(customExecutor.Value.exec_param, MaaProcessor.Resource)
                        });
                    }
                }

                if (value.resource != null)
                {
                    foreach (var customResource in value.resource)
                    {
                        var paths = ReplacePlaceholder(customResource.path ?? new List<string>(),
                            AppDomain.CurrentDomain.BaseDirectory);
                        if (_instance != null)
                            _instance.Resources[customResource.name ?? string.Empty] = paths;
                    }
                }

                if (value.version != null)
                {
                    if (value.version.name != null)
                        MainWindow.Instance?.ShowResourceName(value.version.name);
                    if (value.version.version != null)
                        MainWindow.Instance?.ShowResourceVersion(value.version.version);
                }
            }
        }
    }
}