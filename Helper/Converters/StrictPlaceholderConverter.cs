using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class StrictPlaceholderConverter: JsonConverter
{
    public override bool CanConvert(Type objectType) => true;

    public override object ReadJson(JsonReader reader, Type objectType, 
        object existingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        
        if (token.Type == JTokenType.String && 
            token.Value<string>() == "[placeholder]")
        {
            return GetDefaultValue(objectType); // 根据目标类型返回安全值
        }

        return token.ToObject(objectType);
    }

    private object GetDefaultValue(Type targetType) => 
        Type.GetTypeCode(targetType) switch {
            TypeCode.Int32 => 0,
            TypeCode.Boolean => false,
            TypeCode.String => "[placeholder]", // 保持字符串形式
            _ => Activator.CreateInstance(targetType)
        };

    public override void WriteJson(JsonWriter writer, object value, 
        JsonSerializer serializer) => serializer.Serialize(writer, value);
}