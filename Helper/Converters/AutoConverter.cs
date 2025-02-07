using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class AutoConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object); // We are converting to object as it can be any type.
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        switch (token.Type)
        {
            case JTokenType.Boolean:
                return token.ToObject<bool>();

            case JTokenType.Integer:
                long longValue = token.ToObject<long>();
                if (longValue is >= uint.MinValue and <= uint.MaxValue)
                    return (uint)longValue;

                return longValue;

            case JTokenType.Float:
                return token.ToObject<double>();

            case JTokenType.String:
                return token.ToString();

            case JTokenType.Array:
                var firstElement = token.First;
                if (firstElement?.Type == JTokenType.Integer)
                    return token.ToObject<List<int>>();
                if (firstElement?.Type == JTokenType.String)
                    return token.ToObject<List<string>>();
                if (firstElement?.Type == JTokenType.Array)
                    return token.ToObject<List<List<int>>>();

                break;
        }

        throw new JsonSerializationException($"Invalid JSON format for AutoConverter. Unexpected Type \"{objectType}\"");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        switch (value)
        {
            case bool booleanValue:
                writer.WriteValue(booleanValue);
                break;

            case int intValue:
                writer.WriteValue(intValue);
                break;

            case uint uintValue:
                writer.WriteValue(uintValue);
                break;

            case double doubleValue:
                writer.WriteValue(doubleValue);
                break;

            case string stringValue:
                writer.WriteValue(stringValue);
                break;

            case List<int> intList:
                serializer.Serialize(writer, intList);
                break;

            case List<string> stringList:
                serializer.Serialize(writer, stringList);
                break;

            case List<List<int>> nestedIntList:
                serializer.Serialize(writer, nestedIntList);
                break;

            default:
                throw new JsonSerializationException($"Unexpected value type \"{value?.GetType()}\" for AutoConverter.");
        }
    }
}