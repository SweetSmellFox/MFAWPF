using Newtonsoft.Json;

namespace MFAWPF.Helper.Converters;

public class UniversalEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
            return (T)Enum.ToObject(typeof(T), Convert.ToInt32(reader.Value)); 
        if (reader.TokenType == JsonToken.String)
            return Enum.Parse<T>(reader.Value?.ToString() ?? string.Empty, true); 
        throw new JsonException($"无法转换 {reader.Value} 到 {typeof(T)}");
    }

    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString()); 
    }
}