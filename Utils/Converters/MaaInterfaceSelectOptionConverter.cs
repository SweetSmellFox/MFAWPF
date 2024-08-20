using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Utils.Converters;

public class MaaInterfaceSelectOptionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        switch (token.Type)
        {
            case JTokenType.Array:
                var firstElement = token.First;

                if (firstElement?.Type == JTokenType.String)
                {
                    var list = new List<MaaInterface.MaaInterfaceSelectOption>();
                    foreach (var item in token)
                    {
                        list.Add(new MaaInterface.MaaInterfaceSelectOption
                        {
                            name = item.ToString(),
                            index = 0
                        });
                    }

                    return list;
                }

                return token.ToObject<List<MaaInterface.MaaInterfaceSelectOption>>(serializer);
            case JTokenType.String:
                string? oName = token.ToObject<string>(serializer);
                return new List<MaaInterface.MaaInterfaceSelectOption>
                {
                    new()
                    {
                        name = oName ?? ""
                    }
                };
        }

        throw new JsonSerializationException("Invalid JSON format for MaaInterfaceSelectOptionConverter.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        JArray array = new JArray();

        if (value != null && value is List<MaaInterface.MaaInterfaceSelectOption> selectOptions)
        {
            foreach (var option in selectOptions)
            {
                JObject obj = new JObject
                {
                    ["name"] = option.name,
                    ["index"] = option.index ?? 1
                };
                array.Add(obj);
            }
        }

        array.WriteTo(writer);
    }
}