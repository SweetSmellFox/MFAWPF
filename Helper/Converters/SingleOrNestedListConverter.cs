using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Text;
using System.Windows.Documents;

namespace MFAWPF.Helper.Converters;

public class SingleOrNestedListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<int>) || objectType == typeof(List<List<int>>) || objectType == typeof(string) || objectType == typeof(bool);
    }

    public override object ReadJson(JsonReader reader,
        Type objectType,
        object existingValue,
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

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is IEnumerable<IEnumerable<int>> nestedList)
        {
            serializer.Serialize(writer, nestedList);
        }
        else if (value is IEnumerable<int> singleList)
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
            throw new JsonSerializationException($"Unexpected value type \"{value?.GetType()}\" for CustomListConverter.");
        }
    }

    private List<int> FlattenMultiDimArray(Array multiDimArray)
    {
        List<int> result = new List<int>();
        if (multiDimArray.Rank == 1)
        {
            foreach (int element in multiDimArray)
            {
                result.Add(element);
            }
        }
        else
        {
            foreach (var element in multiDimArray)
            {
                if (element is Array subArray)
                {
                    result.AddRange(FlattenMultiDimArray(subArray));
                }
            }
        }
        return result;
    }
}
