using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Helper.Converters;

public class UIntOrObjectConverter : JsonConverter
{
    public class WaitFreezes
    {
        [JsonProperty("time")] public uint? Time;

        [JsonProperty("target")] [JsonConverter(typeof(SingleOrNestedListConverter))]
        public object? Target;

        [JsonProperty("target_offset")] [JsonConverter(typeof(SingleOrNestedListConverter))]
        public object? TargetOffset;

        [JsonProperty("threshold")] public double? Threshold;
        [JsonProperty("method")] public int? Method;
        [JsonProperty("rate_limit")] public uint? RateLimit;
        [JsonProperty("timeout")] public uint? Timeout;
        
        public override string ToString()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(this, settings);
            return json;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(uint) || objectType == typeof(object);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Integer)
        {
            return token.ToObject<uint>();
        }

        if (token.Type == JTokenType.Object)
        {
            return token.ToObject<WaitFreezes>();
        }

        throw new JsonSerializationException("Invalid JSON format for SingleOrNestedListConverter.");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is uint ui)
        {
            writer.WriteValue(ui);
            return;
        }

        if (value is WaitFreezes waitFreezes)
        {
            JObject jsonObject = new JObject();

            if (waitFreezes.Time.HasValue)
                jsonObject["time"] = JToken.FromObject(waitFreezes.Time.Value, serializer);

            if (waitFreezes.Target != null)
                jsonObject["target"] = JToken.FromObject(waitFreezes.Target, serializer);

            if (waitFreezes.TargetOffset != null)
                jsonObject["target_offset"] = JToken.FromObject(waitFreezes.TargetOffset, serializer);

            if (waitFreezes.Threshold.HasValue)
                jsonObject["threshold"] = JToken.FromObject(waitFreezes.Threshold.Value, serializer);

            if (waitFreezes.Method.HasValue)
                jsonObject["method"] = JToken.FromObject(waitFreezes.Method.Value, serializer);

            if (waitFreezes.RateLimit.HasValue)
                jsonObject["rate_limit"] = JToken.FromObject(waitFreezes.RateLimit.Value, serializer);

            if (waitFreezes.Timeout.HasValue)
                jsonObject["timeout"] = JToken.FromObject(waitFreezes.Timeout.Value, serializer);

            jsonObject.WriteTo(writer);
        }
        else
        {
            throw new JsonSerializationException("Expected WaitFreezes object value.");
        }
    }
}