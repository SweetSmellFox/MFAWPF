using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class ReplaceConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<string[]>);
    }

    public override object ReadJson(JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.Array)
        {
            if (token.First?.Type == JTokenType.Array)
            {
                return token.ToObject<List<string[]>>();
            }

            var list = new List<string[]>
            {
                token.ToObject<string[]>() ?? []
            };
            return list;
        }

        throw new JsonSerializationException("Unexpected token type: " + token.Type);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is List<string[]> list)
        {
            if (list.Count == 1)
            {
                serializer.Serialize(writer, list[0]);
            }
            else
            {
                serializer.Serialize(writer, list);
            }
        }
        else
        {
            writer.WriteValue(value?.ToString() ?? string.Empty);
        }

    }
}
