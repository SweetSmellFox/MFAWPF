using MaaFramework.Binding;
using Newtonsoft.Json;

namespace MFAWPF.Utils.Converters;

public class AdbScreencapMethodsConverter : JsonConverter<AdbScreencapMethods>
{
    public override AdbScreencapMethods ReadJson(JsonReader reader, Type objectType, AdbScreencapMethods existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        ulong value = serializer.Deserialize<ulong>(reader);
        if (Enum.IsDefined(typeof(AdbScreencapMethods), value))
        {
            return (AdbScreencapMethods)value;
        }

        throw new ArgumentException($"Invalid value for AdbScreenCapMethods: {value}");
    }

    public override void WriteJson(JsonWriter writer, AdbScreencapMethods value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, (ulong)value);
    }
}