using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class SingleOrListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object ReadJson(JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token is { Type: JTokenType.String })
        {
            return new List<string>
            {
                token.ToString()
            };
        }

        if (token is { Type: JTokenType.Array })
        {
            return token.ToObject<List<string>>();
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is List<string> list)
        {
            serializer.Serialize(writer, list);
        }
    }
}
