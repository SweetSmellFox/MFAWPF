using MFAWPF.Utils.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class Attribute
{
    public string? Key { get; set; }
    [JsonConverter(typeof(AutoConverter))] public object? Value { get; set; }

    public Attribute(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public Attribute()
    {
    }
}