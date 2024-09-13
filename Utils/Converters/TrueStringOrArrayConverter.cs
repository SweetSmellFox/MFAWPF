namespace MFAWPF.Utils.Converters;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

public class TrueStringOrArrayConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Boolean)
        {
            return token.ToObject<bool>();
        }

        if (token.Type == JTokenType.String)
        {
            return token.ToString();
        }

        if (token.Type == JTokenType.Array)
        {
            List<int>? list = token.ToObject<List<int>>();
            return list;
        }

        Console.WriteLine(objectType);
        Console.WriteLine(token.ToString());
        return token.ToString();
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is bool booleanValue)
        {
            writer.WriteValue(booleanValue);
        }
        else if (value is string stringValue)
        {
            writer.WriteValue(stringValue);
        }
        else if (value is IEnumerable<int> intList)
        {
            serializer.Serialize(writer, intList);
        }
        else
        {
            Console.WriteLine(value?.GetType());
            Console.WriteLine(value?.ToString());
            writer.WriteValue(value?.ToString());
        }
    }
}