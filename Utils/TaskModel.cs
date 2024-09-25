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
    private List<string>? _interrupt;
    private List<string>? _on_error;
    private bool? _is_sub;
    private bool? _inverse;
    private bool? _enabled;
    private uint? _timeout;
    private List<string>? _timeout_next;
    private uint? _times_limit;
    private List<string>? _runout_next;
    private uint? _pre_delay;
    private uint? _post_delay;
    private uint? _rate_limit;
    private object? _pre_wait_freezes;
    private object? _post_wait_freezes;
    private bool? _focus;
    private List<string>? _focus_tip;
    private List<string>? _focus_tip_color;
    private List<string>? _expected;
    private List<string[]>? _replace;
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
    private object? _roi_offset;
    private object? _threshold;
    private object? _lower;
    private object? _upper;
    private bool? _connected;

    [Browsable(false)]
    [JsonIgnore]
    [JsonProperty("name")]
    public string Name
    {
        get => _name;
        set => SetNewProperty(ref _name, value);
    }

    [JsonProperty("recognition")]
    [Category("基础属性")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? Recognition
    {
        get => _recognition;
        set => SetNewProperty(ref _recognition, value);
    }

    [JsonProperty("action")]
    [Category("基础属性")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? Action
    {
        get => _action;
        set => SetNewProperty(ref _action, value);
    }

    [JsonProperty("next")]
    [Category("基础属性")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? Next
    {
        get => _next;
        set => SetNewProperty(ref _next, value);
    }

    [JsonProperty("interrupt")]
    [Category("基础属性")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? Interrupt
    {
        get => _interrupt;
        set => SetNewProperty(ref _interrupt, value);
    }

    [JsonProperty("timeout")]
    [Category("基础属性")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? Timeout
    {
        get => _timeout;
        set => SetNewProperty(ref _timeout, value);
    }

    [JsonProperty("on_error")]
    [Category("基础属性")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? OnError
    {
        get => _on_error;
        set => SetNewProperty(ref _on_error, value);
    }

    [JsonProperty("inverse")]
    [Category("基础属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Inverse
    {
        get => _inverse;
        set => SetNewProperty(ref _inverse, value);
    }

    [JsonProperty("enabled")]
    [Category("基础属性")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Enabled
    {
        get => _enabled;
        set => SetNewProperty(ref _enabled, value);
    }

    [JsonProperty("pre_delay")]
    [Category("基础属性")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? PreDelay
    {
        get => _pre_delay;
        set => SetNewProperty(ref _pre_delay, value);
    }

    [JsonProperty("post_delay")]
    [Category("基础属性")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? PostDelay
    {
        get => _post_delay;
        set => SetNewProperty(ref _post_delay, value);
    }

    [JsonProperty("pre_wait_freezes")]
    [Category("延时设置")]
    [JsonConverter(typeof(UIntOrObjectConverter))]
    [Editor(typeof(NullableUIntOrObjectEditor), typeof(NullableUIntOrObjectEditor))]
    public object? PreWaitFreezes
    {
        get => _pre_wait_freezes;
        set => SetNewProperty(ref _pre_wait_freezes, value);
    }

    [JsonProperty("post_wait_freezes")]
    [Category("延时设置")]
    [JsonConverter(typeof(UIntOrObjectConverter))]
    [Editor(typeof(NullableUIntOrObjectEditor), typeof(NullableUIntOrObjectEditor))]
    public object? PostWaitFreezes
    {
        get => _post_wait_freezes;
        set => SetNewProperty(ref _post_wait_freezes, value);
    }

    [JsonProperty("focus")]
    [Category("任务回调")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Focus
    {
        get => _focus;
        set => SetNewProperty(ref _focus, value);
    }

    [JsonProperty("focus_tip")]
    [Category("任务回调")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? FocusTip
    {
        get => _focus_tip;
        set => SetNewProperty(ref _focus_tip, value);
    }

    [JsonProperty("focus_tip_color")]
    [Category("任务回调")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? FocusTipColor
    {
        get => _focus_tip_color;
        set => SetNewProperty(ref _focus_tip_color, value);
    }

    [JsonProperty("roi")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? Roi
    {
        get => _roi;
        set => SetNewProperty(ref _roi, value);
    }

    [JsonProperty("roi_offset")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public object? RoiOffset
    {
        get => _roi_offset;
        set => SetNewProperty(ref _roi_offset, value);
    }

    [JsonProperty("template")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? Template
    {
        get => _template;
        set => SetNewProperty(ref _template, value);
    }

    [JsonProperty("threshold")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrDoubleListConverter))]
    [Editor(typeof(ListDoubleStringEditor), typeof(ListDoubleStringEditor))]
    public object? Threshold
    {
        get => _threshold;
        set => SetNewProperty(ref _threshold, value);
    }

    [JsonProperty("order_by")]
    [Category("识别器")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? OrderBy
    {
        get => _order_by;
        set => SetNewProperty(ref _order_by, value);
    }

    [JsonProperty("index")]
    [Category("识别器")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? Index
    {
        get => _index;
        set => SetNewProperty(ref _index, value);
    }

    [JsonProperty("method")]
    [Category("识别器")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? Method
    {
        get => _method;
        set => SetNewProperty(ref _method, value);
    }

    [JsonProperty("green_mask")]
    [Category("识别器")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? GreenMask
    {
        get => _green_mask;
        set => SetNewProperty(ref _green_mask, value);
    }

    [JsonProperty("count")]
    [Category("识别器")]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? Count
    {
        get => _count;
        set => SetNewProperty(ref _count, value);
    }

    [JsonProperty("detector")]
    [Category("识别器")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? Detector
    {
        get => _detector;
        set => SetNewProperty(ref _detector, value);
    }

    [JsonProperty("ratio")]
    [Category("识别器")]
    [Editor(typeof(NullableDoubleEditor), typeof(NullableDoubleEditor))]
    public double? Ratio
    {
        get => _ratio;
        set => SetNewProperty(ref _ratio, value);
    }

    [JsonProperty("lower")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? Lower
    {
        get => _lower;
        set => SetNewProperty(ref _lower, value);
    }

    [JsonProperty("upper")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? Upper
    {
        get => _upper;
        set => SetNewProperty(ref _upper, value);
    }

    [JsonProperty("connected")]
    [Category("识别器")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Connected
    {
        get => _connected;
        set => SetNewProperty(ref _connected, value);
    }

    [JsonProperty("expected")]
    [Category("识别器")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? Expected
    {
        get => _expected;
        set => SetNewProperty(ref _expected, value);
    }

    [JsonProperty("replace")]
    [Category("识别器")]
    [JsonConverter(typeof(ReplaceConverter))]
    [Editor(typeof(ListStringArrayEditor), typeof(ListStringArrayEditor))]
    public List<string[]>? Replace
    {
        get => _replace;
        set => SetNewProperty(ref _replace, value);
    }

    [JsonProperty("only_rec")]
    [Category("识别器")]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? OnlyRec
    {
        get => _only_rec;
        set => SetNewProperty(ref _only_rec, value);
    }

    [JsonProperty("model")]
    [Category("识别器")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? Model
    {
        get => _model;
        set => SetNewProperty(ref _model, value);
    }

    [JsonProperty("labels")]
    [Category("识别器")]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? Labels
    {
        get => _labels;
        set => SetNewProperty(ref _labels, value);
    }

    [JsonProperty("custom_recognition")]
    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomRecognition
    {
        get => _custom_recognition;
        set => SetNewProperty(ref _custom_recognition, value);
    }

    [JsonProperty("custom_recognition_param")]
    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomRecognitionParam
    {
        get => _custom_recognition_param;
        set => SetNewProperty(ref _custom_recognition_param, value);
    }

    [JsonProperty("custom_action")]
    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomAction
    {
        get => _custom_action;
        set => SetNewProperty(ref _custom_action, value);
    }

    [JsonProperty("custom_action_param")]
    [Category("自定义")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomActionParam
    {
        get => _custom_action_param;
        set => SetNewProperty(ref _custom_action_param, value);
    }

    [JsonProperty("target")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? Target
    {
        get => _target;
        set => SetNewProperty(ref _target, value);
    }

    [JsonProperty("target_offset")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public List<int>? TargetOffset
    {
        get => _target_offset;
        set => SetNewProperty(ref _target_offset, value);
    }

    [JsonProperty("begin")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? Begin
    {
        get => _begin;
        set => SetNewProperty(ref _begin, value);
    }

    [JsonProperty("begin_offset")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public List<int>? BeginOffset
    {
        get => _begin_offset;
        set => SetNewProperty(ref _begin_offset, value);
    }

    [JsonProperty("end")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? End
    {
        get => _end;
        set => SetNewProperty(ref _end, value);
    }

    [JsonProperty("end_offset")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public List<int>? EndOffset
    {
        get => _end_offset;
        set => SetNewProperty(ref _end_offset, value);
    }

    [JsonProperty("duration")]
    [Category("动作")]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? Duration
    {
        get => _duration;
        set => SetNewProperty(ref _duration, value);
    }

    [JsonProperty("key")]
    [Category("动作")]
    [JsonConverter(typeof(SingleOrIntListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? Key
    {
        get => _key;
        set => SetNewProperty(ref _key, value);
    }

    [JsonProperty("input_text")]
    [Category("动作")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? InputText
    {
        get => _input_text;
        set => SetNewProperty(ref _input_text, value);
    }

    [JsonProperty("package")]
    [Category("动作")]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? Package
    {
        get => _package;
        set => SetNewProperty(ref _package, value);
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
        var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.CanWrite);

        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;

            if (propType.IsValueType)
            {
                if (Nullable.GetUnderlyingType(propType) != null)
                {
                    prop.SetValue(this, null);
                }
                else
                {
                    object defaultValue = Activator.CreateInstance(propType)!;
                    prop.SetValue(this, defaultValue);
                }
            }
            else
            {
                if (prop.Name.Equals(nameof(Name), StringComparison.OrdinalIgnoreCase))
                {
                    prop.SetValue(this, "未命名");
                }
                else
                {
                    prop.SetValue(this, null);
                }
            }
        }

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