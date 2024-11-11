using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Controls;
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
    // [DisplayName("识别算法类型")]
    [Description(
        "识别算法类型。可选，默认 DirectHit 。\n可选的值：DirectHit | TemplateMatch | FeatureMatch | ColorMatch | OCR | NeuralNetworkClassify | NeuralNetworkDetect | Custom")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? Recognition
    {
        get => _recognition;
        set => SetNewProperty(ref _recognition, value);
    }

    [JsonProperty("action")]
    [Category("基础属性")]
    [Description(
        "执行的动作。可选，默认 DoNothing 。\n可选的值：DoNothing | Click | Swipe | Key | Text | StartApp | StopApp | StopTask | Custom")]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? Action
    {
        get => _action;
        set => SetNewProperty(ref _action, value);
    }

    [JsonProperty("next")]
    [Category("基础属性")]
    [Description("接下来要执行的任务列表。可选，默认空。\n按顺序识别 next 中的每个任务，只执行第一个识别到的。")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? Next
    {
        get => _next;
        set => SetNewProperty(ref _next, value);
    }

    [JsonProperty("interrupt")]
    [Category("基础属性")]
    [Description(
        "next 中全部未识别到时的候补任务列表，会执行类似中断操作。可选，默认空。\n若 next 中的任务全部未识别到，则会按序识别该中断列表中的每个任务，并执行第一个识别到的。在后续任务全部执行完成后，重新跳转到该任务来再次尝试识别。\n例如: A: { next: [B, C], interrupt: [D, E] }\n当 B, C 未识别到而识别到 D 时，会去完整的执行 D 及 D.next。但当 D 的流水线完全执行完毕后。会再次回到任务 A，继续尝试识别 B, C, D, E 。\n该字段多用于异常处理，例如 D 是识别 “网络断开提示框”，在点击确认并等待网络连接成功后，继续之前的任务流程。")]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? Interrupt
    {
        get => _interrupt;
        set => SetNewProperty(ref _interrupt, value);
    }

    [JsonProperty("is_sub")]
    [Category("基础属性")]
    [Description(
        "（已在 2.x 版本中废弃，但保留兼容性，推荐使用 interrupt 替代）\n是否是子任务。可选，默认 false 。\n如果是子任务，执行完本任务（及后续 next 等）后，会返回来再次识别本任务 所在的 next 列表。\n例如：A.next = [B, Sub_C, D]，这里的 Sub_C.is_sub = true，\n若匹配上了 Sub_C，在完整执行完 Sub_C 及后续任务后，会返回来再次识别 [B, Sub_C, D] 并执行命中项及后续任务。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? IsSub
    {
        get => _is_sub;
        set => SetNewProperty(ref _is_sub, value);
    }

    [JsonProperty("timeout")]
    [Category("基础属性")]
    [Description(
        "next + interrupt 识别超时时间，毫秒。默认 20 * 1000 。\n具体逻辑为 while(!timeout) { foreach(next + interrupt); sleep_until(rate_limit); } 。"
    )]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? Timeout
    {
        get => _timeout;
        set => SetNewProperty(ref _timeout, value);
    }

    [JsonProperty("on_error")]
    [Category("基础属性")]
    [Description(
        "当识别超时，或动作执行失败后，接下来会执行该列表中的任务。可选，默认空。"
    )]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? OnError
    {
        get => _on_error;
        set => SetNewProperty(ref _on_error, value);
    }

    [JsonProperty("inverse")]
    [Category("基础属性")]
    [Description(
        "反转识别结果，识别到了当做没识别到，没识别到的当做识别到了。可选，默认 false 。\n请注意由此识别出的任务，Click 等动作的点击自身将失效（因为实际并没有识别到东西），若有需求可单独设置 target 。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Inverse
    {
        get => _inverse;
        set => SetNewProperty(ref _inverse, value);
    }

    [JsonProperty("enabled")]
    [Category("基础属性")]
    [Description(
        "是否启用该 task。可选，默认 true 。\n若为 false，其他 task 的 next 列表中的该 task 会被跳过，既不会被识别也不会被执行。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Enabled
    {
        get => _enabled;
        set => SetNewProperty(ref _enabled, value);
    }

    [JsonProperty("pre_delay")]
    [Category("基础属性")]
    [Description(
        "识别到 到 执行动作前 的延迟，毫秒。可选，默认 200 。\n推荐尽可能增加中间过程任务，少用延迟，不然既慢还不稳定。"
    )]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? PreDelay
    {
        get => _pre_delay;
        set => SetNewProperty(ref _pre_delay, value);
    }

    [JsonProperty("post_delay")]
    [Category("基础属性")]
    [Description(
        "执行动作后 到 识别 next 的延迟，毫秒。可选，默认 200 。\n推荐尽可能增加中间过程任务，少用延迟，不然既慢还不稳定。"
    )]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? PostDelay
    {
        get => _post_delay;
        set => SetNewProperty(ref _post_delay, value);
    }

    [JsonProperty("pre_wait_freezes")]
    [Category("延时设置")]
    [Description(
        "识别到 到 执行动作前，等待画面不动了的时间，毫秒。可选，默认 0 ，即不等待。\n连续 pre_wait_freezes 毫秒 画面 没有较大变化 才会退出动作。"
    )]
    [JsonConverter(typeof(UIntOrObjectConverter))]
    [Editor(typeof(NullableUIntOrObjectEditor), typeof(NullableUIntOrObjectEditor))]
    public object? PreWaitFreezes
    {
        get => _pre_wait_freezes;
        set => SetNewProperty(ref _pre_wait_freezes, value);
    }

    [JsonProperty("post_wait_freezes")]
    [Category("延时设置")]
    [Description(
        "行动动作后 到 识别 next，等待画面不动了的时间，毫秒。可选，默认 0 ，即不等待。\n其余逻辑同 pre_wait_freezes。"
    )]
    [JsonConverter(typeof(UIntOrObjectConverter))]
    [Editor(typeof(NullableUIntOrObjectEditor), typeof(NullableUIntOrObjectEditor))]
    public object? PostWaitFreezes
    {
        get => _post_wait_freezes;
        set => SetNewProperty(ref _post_wait_freezes, value);
    }

    [JsonProperty("focus")]
    [Category("任务回调")]
    [Description(
        "是否关注任务，会额外产生部分回调消息。可选，默认 false ，即不产生。\n开启后，focus_tip 和 focus_tip_color 才会生效。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Focus
    {
        get => _focus;
        set => SetNewProperty(ref _focus, value);
    }

    [JsonProperty("focus_tip")]
    [Category("任务回调")]
    [Description(
        "当执行某任务时，在MFA右侧日志输出的内容。可选，默认空。\n需要 focus 开启才会生效。"
    )]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? FocusTip
    {
        get => _focus_tip;
        set => SetNewProperty(ref _focus_tip, value);
    }

    [JsonProperty("focus_tip_color")]
    [Category("任务回调")]
    [Description(
        "当执行某任务时，在MFA右侧日志输出的内容的颜色。可选，默认为Gray。\n需要 focus 开启才会生效。"
    )]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListAutoStringEditor), typeof(ListAutoStringEditor))]
    public List<string>? FocusTipColor
    {
        get => _focus_tip_color;
        set => SetNewProperty(ref _focus_tip_color, value);
    }

    [JsonProperty("roi")]
    [Category("识别器")]
    [Description(
        "识别区域坐标。可选，默认 [0, 0, 0, 0] ，即全屏。\n  array<int, 4>: 识别区域坐标，[x, y, w, h]，若希望全屏可设为 [0, 0, 0, 0] 。\n  string: 填写任务名，在之前执行过的某任务识别到的目标范围内识别。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? Roi
    {
        get => _roi;
        set => SetNewProperty(ref _roi, value);
    }

    [JsonProperty("roi_offset")]
    [Category("识别器")]
    [Description(
        "在 roi 的基础上额外移动再作为范围，四个值分别相加。可选，默认 [0, 0, 0, 0] 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public object? RoiOffset
    {
        get => _roi_offset;
        set => SetNewProperty(ref _roi_offset, value);
    }

    [JsonProperty("template")]
    [Category("识别器")]
    [Description(
        "模板图片路径，需要 image 文件夹的相对路径。必选。\n 所使用的图片需要是无损原图缩放到 720p 后的裁剪。"
    )]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? Template
    {
        get => _template;
        set => SetNewProperty(ref _template, value);
    }

    [JsonProperty("threshold")]
    [Category("识别器")]
    [Description(
        "模板匹配阈值。可选，默认 0.7 。\n 若为数组，长度需和 template 数组长度相同。"
    )]
    [JsonConverter(typeof(SingleOrDoubleListConverter))]
    [Editor(typeof(ListDoubleStringEditor), typeof(ListDoubleStringEditor))]
    public object? Threshold
    {
        get => _threshold;
        set => SetNewProperty(ref _threshold, value);
    }

    [JsonProperty("order_by")]
    [Category("识别器")]
    [Description(
        "结果排序方式。可选，默认 Horizontal。\n 可选的值：Horizontal | Vertical | Score | Area | Random 。 \n 可结合 index 字段使用。"
    )]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? OrderBy
    {
        get => _order_by;
        set => SetNewProperty(ref _order_by, value);
    }

    [JsonProperty("index")]
    [Category("识别器")]
    [Description(
        "命中第几个结果。可选，默认 0 。\n 假设共有 N 个结果，则 index 的取值范围为 [-N, N - 1] ，其中负数使用类 Python 的规则转换为 N - index 。若超出范围，则视为当前识别无结果。"
    )]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? Index
    {
        get => _index;
        set => SetNewProperty(ref _index, value);
    }

    [JsonProperty("method")]
    [Category("识别器")]
    [Description(
        "匹配算法。"
    )]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? Method
    {
        get => _method;
        set => SetNewProperty(ref _method, value);
    }

    [JsonProperty("green_mask")]
    [Category("识别器")]
    [Description(
        "是否进行绿色掩码。可选，默认 false 。\n 若为 true，可以将图片中不希望匹配的部分涂绿 RGB: (0, 255, 0)，则不对绿色部分进行匹配。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? GreenMask
    {
        get => _green_mask;
        set => SetNewProperty(ref _green_mask, value);
    }

    [JsonProperty("count")]
    [Category("识别器")]
    [Description(
        "匹配的点的数量要求（阈值）。"
    )]
    [Editor(typeof(NullableIntEditor), typeof(NullableIntEditor))]
    public int? Count
    {
        get => _count;
        set => SetNewProperty(ref _count, value);
    }

    [JsonProperty("detector")]
    [Category("识别器")]
    [Description(
        "特征检测器。可选，默认 SIFT 。\n目前支持以下算法：\nSIFT\n计算复杂度高，具有尺度不变性、旋转不变性。效果最好。\nKAZE\n适用于2D和3D图像，具有尺度不变性、旋转不变性。\nAKAZE\n计算速度较快，具有尺度不变性、旋转不变性。\nBRISK\n计算速度非常快，具有尺度不变性、旋转不变性。\nORB\n计算速度非常快，具有旋转不变性。但不具有尺度不变性。"
    )]
    [Editor(typeof(StringComboBoxEditor), typeof(StringComboBoxEditor))]
    public string? Detector
    {
        get => _detector;
        set => SetNewProperty(ref _detector, value);
    }

    [JsonProperty("ratio")]
    [Category("识别器")]
    [Description(
        "KNN 匹配算法的距离比值，[0 - 1.0] , 越大则匹配越宽松（更容易连线）。可选，默认 0.6 。"
    )]
    [Editor(typeof(NullableDoubleEditor), typeof(NullableDoubleEditor))]
    public double? Ratio
    {
        get => _ratio;
        set => SetNewProperty(ref _ratio, value);
    }

    [JsonProperty("lower")]
    [Category("识别器")]
    [Description(
        "颜色下限值。必选。最内层 list 长度需和 method 的通道数一致。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? Lower
    {
        get => _lower;
        set => SetNewProperty(ref _lower, value);
    }

    [JsonProperty("upper")]
    [Category("识别器")]
    [Description(
        "颜色上限值。必选。最内层 list 长度需和 method 的通道数一致。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public object? Upper
    {
        get => _upper;
        set => SetNewProperty(ref _upper, value);
    }

    [JsonProperty("connected")]
    [Category("识别器")]
    [Description(
        "是否是相连的点才会被计数。可选，默认 false 。\n若为是，在完成颜色过滤后，则只会计数像素点 全部相连 的最大块。\n若为否，则不考虑这些像素点是否相连。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? Connected
    {
        get => _connected;
        set => SetNewProperty(ref _connected, value);
    }

    [JsonProperty("expected")]
    [Category("识别器")]
    [Description(
        "期望的结果，支持正则。必选。"
    )]
    [JsonConverter(typeof(SingleOrListConverter))]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? Expected
    {
        get => _expected;
        set => SetNewProperty(ref _expected, value);
    }

    [JsonProperty("replace")]
    [Category("识别器")]
    [Description(
        "部分文字识别结果不准确，进行替换。可选。"
    )]
    [JsonConverter(typeof(ReplaceConverter))]
    [Editor(typeof(ListStringArrayEditor), typeof(ListStringArrayEditor))]
    public List<string[]>? Replace
    {
        get => _replace;
        set => SetNewProperty(ref _replace, value);
    }

    [JsonProperty("only_rec")]
    [Category("识别器")]
    [Description(
        "是否仅识别（不进行检测，需要精确设置 roi）。可选，默认 false 。"
    )]
    [Editor(typeof(SwitchPropertyEditor), typeof(SwitchPropertyEditor))]
    public bool? OnlyRec
    {
        get => _only_rec;
        set => SetNewProperty(ref _only_rec, value);
    }

    [JsonProperty("model")]
    [Category("识别器")]
    [Description(
        "模型 文件夹 路径。使用 model/ocr 文件夹的相对路径。可选，默认为空。\n若为空，则为 model/ocr 根目录下的模型文件。\n文件夹中需要包含 rec.onnx, det.onnx, keys.txt 三个文件。"
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? Model
    {
        get => _model;
        set => SetNewProperty(ref _model, value);
    }

    [JsonProperty("labels")]
    [Category("识别器")]
    [Description(
        "标注，即每个分类的名字。可选。\n仅影响调试图片及日志等，若未填写则会填充 \"Unknown\" 。"
    )]
    [Editor(typeof(ListStringEditor), typeof(ListStringEditor))]
    public List<string>? Labels
    {
        get => _labels;
        set => SetNewProperty(ref _labels, value);
    }

    [JsonProperty("custom_recognition")]
    [Category("自定义")]
    [Description(
        "识别名，同注册接口传入的识别名。同时会通过 MaaCustomRecognitionCallback.custom_recognition_name 传出。必选。"
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomRecognition
    {
        get => _custom_recognition;
        set => SetNewProperty(ref _custom_recognition, value);
    }

    [JsonProperty("custom_recognition_param")]
    [Category("自定义")]
    [Description(
        "识别参数，任意类型，会通过 MaaCustomRecognitionCallback.custom_recognition_param 传出。可选，默认空 json，即 {} 。"
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomRecognitionParam
    {
        get => _custom_recognition_param;
        set => SetNewProperty(ref _custom_recognition_param, value);
    }

    [JsonProperty("custom_action")]
    [Category("自定义")]
    [Description(
        "动作名，同注册接口传入的识别器名。同时会通过 MaaCustomActionCallback.custom_action_name 传出。必选。"
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomAction
    {
        get => _custom_action;
        set => SetNewProperty(ref _custom_action, value);
    }

    [JsonProperty("custom_action_param")]
    [Category("自定义")]
    [Description(
        "动作参数，任意类型，会通过 MaaCustomActionCallback.custom_action_param 传出。可选，默认空 json，即 {} 。"
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? CustomActionParam
    {
        get => _custom_action_param;
        set => SetNewProperty(ref _custom_action_param, value);
    }

    [JsonProperty("target")]
    [Category("动作")]
    [Description(
        "点击的位置。可选，默认 true 。\n\ntrue: 点击本任务中刚刚识别到的目标（即点击自身）。\nstring: 填写任务名，点击之前执行过的某任务识别到的目标。\narray<int, 4>: 点击固定坐标区域内随机一点，[x, y, w, h]，若希望全屏可设为 [0, 0, 0, 0] 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? Target
    {
        get => _target;
        set => SetNewProperty(ref _target, value);
    }

    [JsonProperty("target_offset")]
    [Category("动作")]
    [Description(
        "在 target 的基础上额外移动再点击，四个值分别相加。可选，默认 [0, 0, 0, 0] 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public List<int>? TargetOffset
    {
        get => _target_offset;
        set => SetNewProperty(ref _target_offset, value);
    }

    [JsonProperty("begin")]
    [Category("动作")]
    [Description(
        "滑动起点。可选，默认 true 。值同上述 Click.target 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? Begin
    {
        get => _begin;
        set => SetNewProperty(ref _begin, value);
    }

    [JsonProperty("begin_offset")]
    [Category("动作")]
    [Description(
        "在 begin 的基础上额外移动再作为起点，四个值分别相加。可选，默认 [0, 0, 0, 0] 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public List<int>? BeginOffset
    {
        get => _begin_offset;
        set => SetNewProperty(ref _begin_offset, value);
    }

    [JsonProperty("end")]
    [Category("动作")]
    [Description(
        "滑动终点。必选。值同上述 Click.target 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListOrAutoEditor), typeof(SingleIntListOrAutoEditor))]
    public object? End
    {
        get => _end;
        set => SetNewProperty(ref _end, value);
    }

    [JsonProperty("end_offset")]
    [Category("动作")]
    [Description(
        "在 end 的基础上额外移动再作为终点，四个值分别相加。可选，默认 [0, 0, 0, 0] 。"
    )]
    [JsonConverter(typeof(SingleOrNestedListConverter))]
    [Editor(typeof(SingleIntListEditor), typeof(SingleIntListEditor))]
    public List<int>? EndOffset
    {
        get => _end_offset;
        set => SetNewProperty(ref _end_offset, value);
    }

    [JsonProperty("duration")]
    [Category("动作")]
    [Description(
        "滑动持续时间，单位毫秒。可选，默认 200 。"
    )]
    [Editor(typeof(NullableUIntEditor), typeof(NullableUIntEditor))]
    public uint? Duration
    {
        get => _duration;
        set => SetNewProperty(ref _duration, value);
    }

    [JsonProperty("key")]
    [Category("动作")]
    [Description(
        "要按的键，仅支持对应控制器的虚拟按键码。"
    )]
    [JsonConverter(typeof(SingleOrIntListConverter))]
    [Editor(typeof(ListIntStringEditor), typeof(ListIntStringEditor))]
    public List<int>? Key
    {
        get => _key;
        set => SetNewProperty(ref _key, value);
    }

    [JsonProperty("input_text")]
    [Category("动作")]
    [Description(
        "要输入的文本，部分控制器仅支持 ascii 。"
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? InputText
    {
        get => _input_text;
        set => SetNewProperty(ref _input_text, value);
    }

    [JsonProperty("package")]
    [Category("动作")]
    [Description(
        "启动入口 或 要关闭的程序。必选。\n 需要填入 package name ，例如 com.hypergryph.arknights "
    )]
    [Editor(typeof(NullableStringEditor), typeof(NullableStringEditor))]
    public string? Package
    {
        get => _package;
        set => SetNewProperty(ref _package, value);
    }

    [Browsable(false)] [JsonExtensionData] public Dictionary<string, object> AdditionalData { get; set; } = new();

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

    public TaskModel Clone()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        
        string json = JsonConvert.SerializeObject(this, settings);
        return JsonConvert.DeserializeObject<TaskModel>(json) ?? new TaskModel();
    }
}