using System.ComponentModel;
using System.Reflection;
using HandyControl.Controls;
using HandyControl.Data;
using MFAWPF.Utils.Converters;
using MFAWPF.Utils.Editor;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class TaskModel
{
    [Browsable(false)] [JsonIgnore] public string name { get; set; } = "未命名";

    // 任务属性
    [Category("基本属性")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? recognition { get; set; }

    [Category("基本属性")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? action { get; set; }

    [Category("任务流程")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? next { get; set; }

    [Category("任务流程")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? is_sub { get; set; }

    [Category("任务识别")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? inverse { get; set; }

    [Category("基本属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? enabled { get; set; }

    [Category("超时设置")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? timeout { get; set; }

    [Category("超时设置")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? timeout_next { get; set; }

    [Category("次数限制")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? times_limit { get; set; }

    [Category("次数限制")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? runout_next { get; set; }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? pre_delay { get; set; }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? post_delay { get; set; }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntStringEditor), typeof(NullableUIntStringEditor))]
    public object? pre_wait_freezes { get; set; }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntStringEditor), typeof(NullableUIntStringEditor))]
    public object? post_wait_freezes { get; set; }

    [Category("任务回调")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? focus { get; set; }

    [Category("任务回调")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? focus_tip { get; set; }

    [Category("任务回调")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? focus_tip_color { get; set; }


    // Action-specific properties
    [Category("文字匹配")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? expected { get; set; }

    [Category("文字匹配")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? only_rec { get; set; }

    [Category("文字匹配")]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? labels { get; set; }

    [Category("文字匹配")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? model { get; set; }

    [Category("动作匹配")]
    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    [Editor(typeof(ListTrueIntStringEditor), typeof(ListTrueIntStringEditor))]
    public object? target { get; set; }

    [Category("动作匹配")]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? target_offset { get; set; }

    [Category("动作匹配")]
    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    [Editor(typeof(ListTrueIntStringEditor), typeof(ListTrueIntStringEditor))]
    public object? begin { get; set; }

    [Category("动作匹配")]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? begin_offset { get; set; }

    [Category("动作匹配")]
    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    [Editor(typeof(ListTrueIntStringEditor), typeof(ListTrueIntStringEditor))]
    public object? end { get; set; }

    [Category("动作匹配")]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? end_offset { get; set; }

    [Category("动作匹配")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? duration { get; set; }

    [Category("动作匹配")]
    [JsonConverter(typeof(SingleOrIntListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? key { get; set; }

    [Category("文本输入")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? input_text { get; set; }

    [Category("App控制")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? package { get; set; }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_recognition { get; set; }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_recognition_param { get; set; }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_action { get; set; }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_action_param { get; set; }

    [Category("排序")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? order_by { get; set; }

    [Category("索引")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? index { get; set; }

    [Category("算法选择")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? method { get; set; }

    [Category("特征匹配")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? count { get; set; }

    [Category("基本属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? green_mask { get; set; }

    [Category("检测器")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? detector { get; set; }

    [Category("特征匹配")]
    [Editor(typeof(NullableDoubleEditor), typeof(NullableDoubleEditor))]
    public double? ratio { get; set; }

    [Category("模板匹配")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? template { get; set; }

    [Category("基本属性")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? roi { get; set; }

    [Category("基本属性")]
    [Editor(typeof(NullableDoubleEditor), typeof(NullableDoubleEditor))]
    public double? threshold { get; set; }

    [Category("颜色匹配")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? lower { get; set; }

    [Category("颜色匹配")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? upper { get; set; }

    [Category("颜色匹配")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? connected { get; set; }

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
                GetType().GetProperty(property.Key, BindingFlags.Public | BindingFlags.Instance);
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