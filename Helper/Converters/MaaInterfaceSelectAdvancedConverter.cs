using MFAWPF.Extensions.Maa;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;


public class MaaInterfaceSelectAdvancedConverter(bool serializeAsStringArray) : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<MaaInterface.MaaInterfaceSelectAdvanced>);
    }


    public override object ReadJson(JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        switch (token.Type)
        {
            case JTokenType.Array:
                var firstElement = token.First;

                if (firstElement?.Type == JTokenType.String)
                {
                    var list = new List<MaaInterface.MaaInterfaceSelectAdvanced>();
                    foreach (var item in token)
                    {
                        list.Add(new MaaInterface.MaaInterfaceSelectAdvanced
                        {
                            Name = item.ToString(),
                        });
                    }

                    return list;
                }

                if (firstElement?.Type == JTokenType.Object)
                {
                    return token.ToObject<List<MaaInterface.MaaInterfaceSelectAdvanced>>(serializer);
                }

                break;
            case JTokenType.String:
                var oName = token.ToObject<string>(serializer);
                return new List<MaaInterface.MaaInterfaceSelectAdvanced>
                {
                    new()
                    {
                        Name = oName ?? ""
                    }
                };
            case JTokenType.None:
                return null;
        }

        Console.WriteLine($"Invalid JSON format for MaaInterfaceSelectAdvancedConverter. Unexpected type {objectType}.");
        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var array = new JArray();

        if (value is List<MaaInterface.MaaInterfaceSelectAdvanced> selectOptions)
        {
            if (serializeAsStringArray)
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
                    var obj = JObject.FromObject(option);
                    array.Add(obj);
                }
            }

            array.WriteTo(writer);
        }
    }
}
