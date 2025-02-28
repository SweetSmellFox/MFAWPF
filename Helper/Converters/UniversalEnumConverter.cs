using Newtonsoft.Json;
using System.Numerics;

namespace MFAWPF.Helper.Converters;

public class UniversalEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value is BigInteger bigInt)
            return ConvertFromBigInteger(bigInt);
        if (reader.Value != null && reader.TokenType == JsonToken.Integer)
            return (T)Enum.ToObject(typeof(T), reader.Value);
        if (reader.TokenType == JsonToken.String)
            return Enum.Parse<T>(reader.Value?.ToString() ?? string.Empty, true);
        throw new JsonException($"无法转换 {reader.Value} 到 {typeof(T)}");
    }
    private T ConvertFromBigInteger(BigInteger bigInt)
    {
        Type underlyingType = Enum.GetUnderlyingType(typeof(T));
        
        try
        {
            checked 
            {
                return underlyingType switch
                {
                    _ when underlyingType == typeof(ulong) => (T)Enum.ToObject(typeof(T), (ulong)bigInt),
                    _ when underlyingType == typeof(long) => (T)Enum.ToObject(typeof(T), (long)bigInt),
                    _ when underlyingType == typeof(uint) => (T)Enum.ToObject(typeof(T), (uint)bigInt),
                    _ when underlyingType == typeof(int) => (T)Enum.ToObject(typeof(T), (int)bigInt),
                    _ => throw new NotSupportedException($"不支持的底层类型: {underlyingType.Name}")
                };
            }
        }
        catch (OverflowException)
        {
            throw new JsonException($"数值 {bigInt} 超过 {underlyingType.Name} 的范围");
        }
    }
    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}
