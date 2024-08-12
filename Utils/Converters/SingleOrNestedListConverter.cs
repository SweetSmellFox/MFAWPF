using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Utils.Converters;

public class SingleOrNestedListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<int>) || objectType == typeof(List<List<int>>);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Array)
        {
            var firstElement = token.First;
            if (firstElement != null && firstElement.Type == JTokenType.Array)
            {
                // Deserialize as List<List<int>>
                return token.ToObject<List<List<int>>>();
            }
            else
            {
                // Deserialize as List<int>
                return token.ToObject<List<int>>();
            }
        }

        throw new JsonSerializationException("Invalid JSON format for SingleOrNestedListConverter.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is List<List<int>> nestedList)
        {
            serializer.Serialize(writer, nestedList);
        }
        else if (value is List<int> singleList)
        {
            serializer.Serialize(writer, singleList);
        }
        else
        {
            throw new JsonSerializationException("Unexpected value type for SingleOrNestedListConverter.");
        }
    }
}
