using MaaFramework.Binding;
using Newtonsoft.Json;

namespace MFAWPF.Helper.Converters;

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

        return AdbScreencapMethods.Default;
    }

    public override void WriteJson(JsonWriter writer, AdbScreencapMethods value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, (ulong)value);
    }
}