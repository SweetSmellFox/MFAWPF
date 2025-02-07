using MaaFramework.Binding;
using Newtonsoft.Json;

namespace MFAWPF.Helper.Converters;

public class AdbInputMethodsConverter : JsonConverter<AdbInputMethods>
{
    public override AdbInputMethods ReadJson(JsonReader reader, Type objectType, AdbInputMethods existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        ulong value = serializer.Deserialize<ulong>(reader);
        if (Enum.IsDefined(typeof(AdbInputMethods), value))
        {
            return (AdbInputMethods)value;
        }

        return AdbInputMethods.MinitouchAndAdbKey;
    }

    public override void WriteJson(JsonWriter writer, AdbInputMethods value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, (ulong)value);
    }
}