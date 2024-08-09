using MaaFramework.Binding;
using MaaFramework.Binding.Custom;
using MFAWPF.Utils.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class MaaInterface
{
    public class MaaInterfaceOptionCase
    {
        public string? name { get; set; }
        public Dictionary<string, TaskModel>? param;

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
        public List<MaaInterfaceOptionCase>? cases;
        public string name;
    }

    public class MaaInterfaceSelectOption
    {
        public string? name;
        public int? index;
    }

    public class CustomExecutor
    {
        [JsonIgnore] public string? name;
        public string? exec_path;

        [JsonConverter(typeof(SingleOrListConverter))]
        public List<string>? exec_param;
    }

    public List<TaskInterfaceItem>? task;
    public Dictionary<string, CustomExecutor>? recognizer;
    public Dictionary<string, CustomExecutor>? action;
    public Dictionary<string, MaaInterfaceOption>? option;

    private static MaaInterface _instance;
    [JsonIgnore] public List<MaaCustomRecognizerExecutor> CustomRecognizerExecutors = new();

    [JsonIgnore] public List<MaaCustomActionExecutor> CustomActionExecutors = new();

    // 替换单个字符串中的 "{PROJECT_DIR}" 为指定的替换值
    public static string ReplacePlaceholder(string input, string replacement)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace("{PROJECT_DIR}", replacement);
    }

    // 替换字符串列表中的每个字符串中的 "{PROJECT_DIR}"
    public static List<string> ReplacePlaceholder(List<string> inputs, string replacement)
    {
        var replacedList = new List<string>();
        if (inputs == null)
            return replacedList;

        foreach (var input in inputs)
            replacedList.Add(ReplacePlaceholder(input, replacement));

        return replacedList;
    }

    public static MaaInterface Instance
    {
        get => _instance;
        set
        {
            _instance = value;
            if (value != null)
            {
                _instance.CustomRecognizerExecutors.Clear();
                _instance.CustomActionExecutors.Clear();
                if (value.recognizer != null)
                {
                    foreach (var customExecutor in value.recognizer)
                    {
                        value.CustomRecognizerExecutors.Add(new MaaCustomRecognizerExecutor()
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
                        value.CustomActionExecutors.Add(new MaaCustomActionExecutor()
                        {
                            Name = customExecutor.Key,
                            Path = ReplacePlaceholder(customExecutor.Value.exec_path, MaaProcessor.Resource),
                            Parameter = ReplacePlaceholder(customExecutor.Value.exec_param, MaaProcessor.Resource)
                        });
                    }
                }
            }
        }
    }
}