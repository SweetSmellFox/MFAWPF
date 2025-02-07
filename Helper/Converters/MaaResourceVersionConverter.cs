using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class MaaResourceVersionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string) || objectType == typeof(MaaInterface.MaaResourceVersion);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        var res = token.ToString();
        if (res.Contains('{') || res.Contains('}'))
        {
            var version = token.ToObject<MaaInterface.MaaResourceVersion>(serializer);
            return version?.Version;
        }

        return res;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is string strValue)
        {
            writer.WriteValue(strValue);
        }
   
        if (value is MaaInterface.MaaResourceVersion mrv)
            writer.WriteValue(mrv.Version);
    }
}