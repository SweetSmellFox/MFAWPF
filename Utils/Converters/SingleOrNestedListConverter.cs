using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Utils.Converters;

public class SingleOrNestedListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<int>) || objectType == typeof(List<List<int>>) ||
               objectType == typeof(string) || objectType == typeof(bool);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Array)
        {
            if (token.First is { Type: JTokenType.Array })
                return token.ToObject<List<List<int>>>();

            return token.ToObject<List<int>>();
        }

        if (token.Type == JTokenType.String)
        {
            return token.ToString();
        }

        if (token.Type == JTokenType.Boolean)
        {
            return token.ToString();
        }

        throw new JsonSerializationException($"Invalid JSON format for SingleOrNestedListConverter. Unexpected Type \"{objectType}\"");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is List<List<int>> nestedList)
        {
            if (nestedList.Count == 1)
                serializer.Serialize(writer, nestedList[0]);
            else
                serializer.Serialize(writer, nestedList);
            serializer.Serialize(writer, nestedList);
        }
        else if (value is List<int> singleList)
        {
            serializer.Serialize(writer, singleList);
        }
        else if (value is string s)
        {
            writer.WriteValue(s);
        }
        else if (value is bool b)
        {
            writer.WriteValue(b);
        }
        else
        {
            throw new JsonSerializationException($"Unexpected value type \"{value?.GetType()}\" for SingleOrNestedListConverter.");
        }
    }
}