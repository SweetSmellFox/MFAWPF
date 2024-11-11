using MFAWPF.Utils.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class TaskInterfaceItem
{
    [JsonProperty("name")] public string? Name;
    [JsonProperty("entry")] public string? Entry;
    [JsonProperty("doc")] public string? Document;
    [JsonProperty("check")] public bool? Check;
    [JsonProperty("repeatable")] public bool? Repeatable;
    [JsonProperty("repeat_count")] public int? RepeatCount;

    [JsonProperty("option")] 
    public List<MaaInterface.MaaInterfaceSelectOption>? Option;

    [JsonProperty("pipeline_override")] public Dictionary<string, TaskModel>? PipelineOverride;

    [JsonProperty("task")] public TaskModel? Task { get; set; }

    public override string ToString()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        return JsonConvert.SerializeObject(this, settings);
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="TaskInterfaceItem"/> instance.
    /// </summary>
    /// <returns>A new <see cref="TaskInterfaceItem"/> instance that is a deep copy of the current instance.</returns>
    public TaskInterfaceItem Clone()
    {
        return JsonConvert.DeserializeObject<TaskInterfaceItem>(ToString()) ?? new TaskInterfaceItem();
    }
}