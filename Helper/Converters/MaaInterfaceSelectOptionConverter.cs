using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class MaaInterfaceSelectOptionConverter : JsonConverter
{
    private readonly bool _serializeAsStringArray;

    public MaaInterfaceSelectOptionConverter(bool serializeAsStringArray)
    {
        _serializeAsStringArray = serializeAsStringArray;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<MaaInterface.MaaInterfaceSelectOption>);
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
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
                            Name = item.ToString(),
                            Index = 0
                        });
                    }

                    return list;
                }

                if (firstElement?.Type == JTokenType.Object)
                {
                    return token.ToObject<List<MaaInterface.MaaInterfaceSelectOption>>(serializer);
                }

                break;
            case JTokenType.String:
               var oName = token.ToObject<string>(serializer);
                return new List<MaaInterface.MaaInterfaceSelectOption>
                {
                    new()
                    {
                        Name = oName ?? "", Index = 0
                    }
                };
            case JTokenType.None:
                return null;
        }

        Console.WriteLine($"Invalid JSON format for MaaInterfaceSelectOptionConverter. Unexpected type {objectType}.");
        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var array = new JArray();

        if (value is List<MaaInterface.MaaInterfaceSelectOption> selectOptions)
        {
            if (_serializeAsStringArray)
            {
                foreach (var option in selectOptions)
                {
                    array.Add(option.Name);
                }
            }
            else
            {
                foreach (var option in selectOptions)
                {
                    JObject obj = new JObject
                    {
                        ["name"] = option.Name,
                        ["index"] = option.Index
                    };
                    array.Add(obj);
                }
            }

            array.WriteTo(writer);
        }
    }
}