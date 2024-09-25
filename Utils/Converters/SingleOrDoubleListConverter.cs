using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Utils.Converters;

public class SingleOrDoubleListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Float)
        {
            return new List<double> { token.ToObject<double>() };
        }

        if (token.Type == JTokenType.Array)
        {
            return token.ToObject<List<double>>();
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is List<double> list)
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