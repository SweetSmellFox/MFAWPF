using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class StringOrObjectConverter : JsonConverter
{
    // 允许转换的类型：字符串或动态对象
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string) || objectType == typeof(object);
    }

    // 反序列化逻辑
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
            
        // 情况 1：JSON 值为字符串
        if (token.Type == JTokenType.String)
        {
            return token.ToObject<string>();
        }
            
        // 情况 2：JSON 值为对象（动态解析为 JObject）
        if (token.Type == JTokenType.Object)
        {
            // 使用 JObject 动态处理任意结构
            return token.ToObject<JObject>();
        }
            
        throw new JsonSerializationException($"不支持的 JSON 类型：{token.Type}");
    }

    // 序列化逻辑
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // 情况 1：输入为字符串类型
        if (value is string str)
        {
            writer.WriteValue(str);
            return;
        }
            
        // 情况 2：输入为动态对象（如 JObject 或自定义类型）
        if (value is JObject jObj)
        {
            jObj.WriteTo(writer);
        }
        else
        {
            // 处理其他动态对象（如字典）
            JToken.FromObject(value, serializer).WriteTo(writer);
        }
    }
}