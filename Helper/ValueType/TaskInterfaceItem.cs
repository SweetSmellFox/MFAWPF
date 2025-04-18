﻿using MFAWPF.Extensions.Maa;
using MFAWPF.Helper.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Helper.ValueType;

public class TaskInterfaceItem
{
    [JsonProperty("name")] public string? Name;
    [JsonProperty("entry")] public string? Entry;
    [JsonConverter(typeof(SingleOrListConverter))] [JsonProperty("doc")]
    public List<string>? Document;
    [JsonProperty("check",
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Include)]
    public bool? Check = false;
    [JsonProperty("repeatable")] public bool? Repeatable;
    [JsonProperty("repeat_count")] public int? RepeatCount;

    [JsonProperty("advanced")] public List<MaaInterface.MaaInterfaceSelectAdvanced>? Advanced;

    [JsonProperty("option")] public List<MaaInterface.MaaInterfaceSelectOption>? Option;

    [JsonProperty("pipeline_override")] public Dictionary<string, TaskModel>? PipelineOverride;

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
