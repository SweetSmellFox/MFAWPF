using System.Reflection;
using MFAWPF.Utils.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class TaskModel
{
    [JsonIgnore] public string name { get; set; } = "未命名";

    // 任务属性
    public string? recognition { get; set; }
    public string? action { get; set; }

    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? next { get; set; }

    public bool? is_sub { get; set; }
    public bool? inverse { get; set; }
    public bool? enabled { get; set; }
    public uint? timeout { get; set; }

    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? timeout_next { get; set; }

    public uint? times_limit { get; set; }

    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? runout_next { get; set; }

    public uint? pre_delay { get; set; }
    public uint? post_delay { get; set; }
    public object? pre_wait_freezes { get; set; }
    public object? post_wait_freezes { get; set; }
    public bool? focus { get; set; }

    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? focus_tip { get; set; }

    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? focus_tip_color { get; set; }

    // Action-specific properties
    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? expected { get; set; }

    public bool? only_rec { get; set; }

    public List<string>? labels { get; set; }
    public string? model { get; set; }

    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    public object? target { get; set; }

    public List<int>? target_offset { get; set; }

    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    public object? begin { get; set; }

    public List<int>? begin_offset { get; set; }

    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    public object? end { get; set; }

    public List<int>? end_offset { get; set; }

    public uint? duration { get; set; }

    [JsonConverter(typeof(SingleOrIntListConverter))]
    public List<int>? key { get; set; }

    public string? input_text { get; set; }
    public string? package { get; set; }

    public string? custom_recognition { get; set; }

    public string? custom_recognition_param { get; set; }
    public string? custom_action { get; set; }
    public string? custom_action_param { get; set; }

    public string? order_by { get; set; }

    public int? index { get; set; }

    public int? method { get; set; }

    public int? count { get; set; }

    public bool? green_mask { get; set; }

    public string? detector { get; set; }
    public double? ratio { get; set; }

    [JsonConverter(typeof(SingleOrListConverter))]
    public List<string>? template { get; set; }

    [JsonConverter(typeof(SingleOrNestedListConverter))]
    public object? roi { get; set; }

    public double? threshold { get; set; }

    [JsonConverter(typeof(SingleOrNestedListConverter))]
    public object? lower { get; set; }

    [JsonConverter(typeof(SingleOrNestedListConverter))]
    public object? upper { get; set; }

    public bool? connected { get; set; }

    public override string ToString()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        string json = JsonConvert.SerializeObject(this, settings);
        return json;
    }

    public TaskModel Set(object properties)
    {
        foreach (PropertyInfo prop in properties.GetType().GetProperties())
        {
            PropertyInfo? propInfo = this.GetType().GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo != null && propInfo.CanWrite)
            {
                propInfo.SetValue(this, prop.GetValue(properties));
            }
        }

        return this;
    }

    public TaskModel Set(Dictionary<string, object?> properties)
    {
        foreach (var property in properties)
        {
            PropertyInfo? propInfo =
                this.GetType().GetProperty(property.Key, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo != null && propInfo.CanWrite)
            {
                propInfo.SetValue(this, property.Value);
            }
        }

        return this;
    }

    public TaskModel Set(params Attribute[] attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.Key != null)
            {
                PropertyInfo? propInfo =
                    GetType().GetProperty(attribute.Key, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo != null && propInfo.CanWrite)
                    propInfo.SetValue(this, attribute.Value);
            }
        }

        return this;
    }

    public void Merge(TaskModel other)
    {
        foreach (var property in typeof(TaskModel).GetProperties())
        {
            var otherValue = property.GetValue(other);
            if (otherValue != null)
            {
                property.SetValue(this, otherValue);
            }
        }
    }

    public TaskModel Reset()
    {
        name = "未命名";
        recognition = null;
        action = null;
        next = null;
        is_sub = null;
        inverse = null;
        enabled = null;
        timeout = null;
        timeout_next = null;
        times_limit = null;
        runout_next = null;
        pre_delay = null;
        post_delay = null;
        pre_wait_freezes = null;
        post_wait_freezes = null;
        focus = null;
        focus_tip = null;
        focus_tip_color = null;
        expected = null;
        only_rec = null;
        labels = null;
        model = null;
        target = null;
        target_offset = null;
        begin = null;
        begin_offset = null;
        end = null;
        end_offset = null;
        duration = null;
        key = null;
        input_text = null;
        package = null;
        custom_recognition = null;
        custom_recognition_param = null;
        custom_action = null;
        custom_action_param = null;
        order_by = null;
        index = null;
        method = null;
        count = null;
        green_mask = null;
        detector = null;
        ratio = null;
        template = null;
        roi = null;
        threshold = null;
        lower = null;
        upper = null;
        connected = null;
        return this;
    }


    public List<Attribute> ToAttributeList()
    {
        var attributes = new List<Attribute>();
        foreach (PropertyInfo prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(this);
            if (value != null && !prop.Name.Equals("name"))
            {
                attributes.Add(new Attribute(prop.Name, value));
            }
        }

        return attributes;
    }
}