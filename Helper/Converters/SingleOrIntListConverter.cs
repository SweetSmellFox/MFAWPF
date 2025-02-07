using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class SingleOrIntListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Integer)
        {
            return new List<int> { token.ToObject<int>() };
        }

        if (token.Type == JTokenType.Array)
        {
            return token.ToObject<List<int>>();
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is List<int> list)
        {
            if (list.Count == 1)
            {
                writer.WriteValue(list[0]);
            }
            else
            {
                serializer.Serialize(writer, list);
            }
        }
    }
}