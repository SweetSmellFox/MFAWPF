using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Controls;
using HandyControl.Data;
using MFAWPF.Utils.Converters;
using MFAWPF.Utils.Editor;
using Newtonsoft.Json;

namespace MFAWPF.Utils;

public class TaskModel : ObservableObject
{
    private string _name = "未命名";
    private string? _recognition;
    private string? _action;
    private List<string>? _next;
    private bool? _is_sub;
    private bool? _inverse;
    private bool? _enabled;
    private uint? _timeout;
    private List<string>? _timeout_next;
    private uint? _times_limit;
    private List<string>? _runout_next;
    private uint? _pre_delay;
    private uint? _post_delay;
    private object? _pre_wait_freezes;
    private object? _post_wait_freezes;
    private bool? _focus;
    private List<string>? _focus_tip;
    private List<string>? _focus_tip_color;
    private List<string>? _expected;
    private bool? _only_rec;
    private List<string>? _labels;
    private string? _model;
    private object? _target;
    private List<int>? _target_offset;
    private object? _begin;
    private List<int>? _begin_offset;
    private object? _end;
    private List<int>? _end_offset;
    private uint? _duration;
    private List<int>? _key;
    private string? _input_text;
    private string? _package;
    private string? _custom_recognition;
    private string? _custom_recognition_param;
    private string? _custom_action;
    private string? _custom_action_param;
    private string? _order_by;
    private int? _index;
    private int? _method;
    private int? _count;
    private bool? _green_mask;
    private string? _detector;
    private double? _ratio;
    private List<string>? _template;
    private object? _roi;
    private double? _threshold;
    private object? _lower;
    private object? _upper;
    private bool? _connected;

    [Browsable(false)]
    [JsonIgnore]
    public string name
    {
        get => _name;
        set => SetNewProperty(ref _name, value);
    }

    [Category("基本属性")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? recognition
    {
        get => _recognition;
        set => SetNewProperty(ref _recognition, value);
    }

    [Category("基本属性")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? action
    {
        get => _action;
        set => SetNewProperty(ref _action, value);
    }

    [Category("任务流程")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? next
    {
        get => _next;
        set => SetNewProperty(ref _next, value);
    }

    [Category("任务流程")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? is_sub
    {
        get => _is_sub;
        set => SetNewProperty(ref _is_sub, value);
    }

    [Category("基本属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? inverse
    {
        get => _inverse;
        set => SetNewProperty(ref _inverse, value);
    }

    [Category("基本属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? enabled
    {
        get => _enabled;
        set => SetNewProperty(ref _enabled, value);
    }

    [Category("超时设置")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? timeout
    {
        get => _timeout;
        set => SetNewProperty(ref _timeout, value);
    }

    [Category("超时设置")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? timeout_next
    {
        get => _timeout_next;
        set => SetNewProperty(ref _timeout_next, value);
    }

    [Category("次数限制")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? times_limit
    {
        get => _times_limit;
        set => SetNewProperty(ref _times_limit, value);
    }

    [Category("次数限制")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? runout_next
    {
        get => _runout_next;
        set => SetNewProperty(ref _runout_next, value);
    }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? pre_delay
    {
        get => _pre_delay;
        set => SetNewProperty(ref _pre_delay, value);
    }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? post_delay
    {
        get => _post_delay;
        set => SetNewProperty(ref _post_delay, value);
    }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntStringEditor), typeof(NullableUIntStringEditor))]
    public object? pre_wait_freezes
    {
        get => _pre_wait_freezes;
        set => SetNewProperty(ref _pre_wait_freezes, value);
    }

    [Category("延时设置")]
    [Editor(typeof(NullableUIntStringEditor), typeof(NullableUIntStringEditor))]
    public object? post_wait_freezes
    {
        get => _post_wait_freezes;
        set => SetNewProperty(ref _post_wait_freezes, value);
    }

    [Category("任务回调")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? focus
    {
        get => _focus;
        set => SetNewProperty(ref _focus, value);
    }

    [Category("任务回调")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? focus_tip
    {
        get => _focus_tip;
        set => SetNewProperty(ref _focus_tip, value);
    }

    [Category("任务回调")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? focus_tip_color
    {
        get => _focus_tip_color;
        set => SetNewProperty(ref _focus_tip_color, value);
    }

    [Category("文字匹配")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? expected
    {
        get => _expected;
        set => SetNewProperty(ref _expected, value);
    }

    [Category("文字匹配")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? only_rec
    {
        get => _only_rec;
        set => SetNewProperty(ref _only_rec, value);
    }

    [Category("文字匹配")]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? labels
    {
        get => _labels;
        set => SetNewProperty(ref _labels, value);
    }

    [Category("文字匹配")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? model
    {
        get => _model;
        set => SetNewProperty(ref _model, value);
    }

    [Category("动作匹配")]
    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    [Editor(typeof(ListTrueIntStringEditor), typeof(ListTrueIntStringEditor))]
    public object? target
    {
        get => _target;
        set => SetNewProperty(ref _target, value);
    }

    [Category("动作匹配")]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? target_offset
    {
        get => _target_offset;
        set => SetNewProperty(ref _target_offset, value);
    }

    [Category("动作匹配")]
    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    [Editor(typeof(ListTrueIntStringEditor), typeof(ListTrueIntStringEditor))]
    public object? begin
    {
        get => _begin;
        set => SetNewProperty(ref _begin, value);
    }

    [Category("动作匹配")]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? begin_offset
    {
        get => _begin_offset;
        set => SetNewProperty(ref _begin_offset, value);
    }

    [Category("动作匹配")]
    [JsonConverter(typeof(TrueStringOrArrayConverter))]
    [Editor(typeof(ListTrueIntStringEditor), typeof(ListTrueIntStringEditor))]
    public object? end
    {
        get => _end;
        set => SetNewProperty(ref _end, value);
    }

    [Category("动作匹配")]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? end_offset
    {
        get => _end_offset;
        set => SetNewProperty(ref _end_offset, value);
    }

    [Category("动作匹配")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? duration
    {
        get => _duration;
        set => SetNewProperty(ref _duration, value);
    }

    [Category("动作匹配")]
    [JsonConverter(typeof(SingleOrIntListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? key
    {
        get => _key;
        set => SetNewProperty(ref _key, value);
    }

    [Category("文本输入")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? input_text
    {
        get => _input_text;
        set => SetNewProperty(ref _input_text, value);
    }

    [Category("App控制")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? package
    {
        get => _package;
        set => SetNewProperty(ref _package, value);
    }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_recognition
    {
        get => _custom_recognition;
        set => SetNewProperty(ref _custom_recognition, value);
    }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_recognition_param
    {
        get => _custom_recognition_param;
        set => SetNewProperty(ref _custom_recognition_param, value);
    }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_action
    {
        get => _custom_action;
        set => SetNewProperty(ref _custom_action, value);
    }

    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? custom_action_param
    {
        get => _custom_action_param;
        set => SetNewProperty(ref _custom_action_param, value);
    }

    [Category("排序")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? order_by
    {
        get => _order_by;
        set => SetNewProperty(ref _order_by, value);
    }

    [Category("索引")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? index
    {
        get => _index;
        set => SetNewProperty(ref _index, value);
    }

    [Category("算法选择")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? method
    {
        get => _method;
        set => SetNewProperty(ref _method, value);
    }

    [Category("特征匹配")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? count
    {
        get => _count;
        set => SetNewProperty(ref _count, value);
    }

    [Category("基本属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? green_mask
    {
        get => _green_mask;
        set => SetNewProperty(ref _green_mask, value);
    }

    [Category("检测器")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? detector
    {
        get => _detector;
        set => SetNewProperty(ref _detector, value);
    }

    [Category("特征匹配")]
    [Editor(typeof(NullableDoubleEditor), typeof(NullableDoubleEditor))]
    public double? ratio
    {
        get => _ratio;
        set => SetNewProperty(ref _ratio, value);
    }

    [Category("模板匹配")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? template
    {
        get => _template;
        set => SetNewProperty(ref _template, value);
    }

    [Category("基本属性")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? roi
    {
        get => _roi;
        set => SetNewProperty(ref _roi, value);
    }

    [Category("基本属性")]
    [Editor(typeof(NullableDoubleEditor), typeof(NullableDoubleEditor))]
    public double? threshold
    {
        get => _threshold;
        set => SetNewProperty(ref _threshold, value);
    }

    [Category("颜色匹配")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? lower
    {
        get => _lower;
        set => SetNewProperty(ref _lower, value);
    }

    [Category("颜色匹配")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? upper
    {
        get => _upper;
        set => SetNewProperty(ref _upper, value);
    }

    [Category("颜色匹配")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? connected
    {
        get => _connected;
        set => SetNewProperty(ref _connected, value);
    }

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

    protected bool SetNewProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue,
        [CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanging(propertyName);

        field = newValue;

        OnPropertyChanged(propertyName);

        return true;
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