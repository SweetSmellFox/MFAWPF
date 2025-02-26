using MaaFramework.Binding;
using MFAWPF.Extensions.Maa;
using Newtonsoft.Json;

namespace MFAWPF.Helper.Converters;

public class MaaControllerTypesConverter : JsonConverter<MaaControllerTypes>
{
    public override MaaControllerTypes ReadJson(
        JsonReader reader,
        Type objectType,
        MaaControllerTypes existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            var value = Convert.ToInt32(reader.Value);
            if (Enum.IsDefined(typeof(MaaControllerTypes), value))
            {
                return (MaaControllerTypes)value;
            }
            return (MaaControllerTypes)value;
        }
        if (reader.TokenType == JsonToken.String)
        {
            var enumString = Convert.ToString(reader.Value) ?? string.Empty;
            return (MaaControllerTypes)Enum.Parse(typeof(MaaControllerTypes), enumString);
        }

        throw new JsonException($"无法将 {reader.Value} 转换为 {typeof(MaaControllerTypes)}");
    }


    public override void WriteJson(JsonWriter writer, MaaControllerTypes value, JsonSerializer serializer)
    {
        writer.WriteValue((int)value);
    }
}
