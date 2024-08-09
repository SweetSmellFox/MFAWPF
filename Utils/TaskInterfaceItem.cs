using MFAWPF.Utils.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class TaskInterfaceItem
{
    public string? name;
    public string? entry;
    public bool? check;
    
    [JsonConverter(typeof(MaaInterfaceSelectOptionConverter))]
    public List<MaaInterface.MaaInterfaceSelectOption>? option;

    public Dictionary<string, TaskModel>? param;

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
}