using MFAWPF.Helper.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace MFAWPF.Extensions.Maa;

public class MaaInterfaceAdvancedOption
{
    [JsonConverter(typeof(SingleOrListConverter))] [JsonProperty("field")]
    public List<string>? Field;
    [JsonConverter(typeof(SingleOrListConverter))] [JsonProperty("type")]
    public List<string>? Type;
    [JsonConverter(typeof(SingleOrListConverter))] [JsonProperty("default")]
    public List<string>? Default;
    [JsonProperty("pipeline_override")] public Dictionary<string, Dictionary<string, JToken>>? PipelineOverride;

    private Dictionary<string, Type> GetTypeMap()
    {
        var typeMap = new Dictionary<string, Type>();
        if (Field == null || Type == null) return typeMap;

        for (int i = 0; i < Field.Count; i++)
        {
            var type = Type.Count > 0 ? (i >= Type.Count ? Type[0] : Type[i]) : "string";
            var typeName = type.ToLower();
            typeMap[Field[i]] = typeName switch
            {
                "int" => typeof(int),
                "float" => typeof(float),
                "bool" => typeof(bool),
                _ => typeof(string)
            };
        }
        return typeMap;
    }


    // 内置的占位符替换方法
    public string GenerateProcessedPipeline(Dictionary<string, string> inputValues)
    {
        if (PipelineOverride == null) return "{}";

        // 深拷贝原始数据（关键步骤）
        var cloned = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, JToken>>>(
            JsonConvert.SerializeObject(PipelineOverride)
        );

        var typeMap = GetTypeMap(); // 获取类型映射
        var regex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        foreach (var preset in cloned.Values)
        {
            foreach (var key in preset.Keys.ToList())
            {
                var jToken = preset[key];
                if (jToken.Type == JTokenType.Array)
                {
                    for (int i = 0; i < jToken.Count(); i++)
                    {
                        ReplaceToken(jToken[i], regex, inputValues, typeMap);
                    }
                }
                else
                {
                    var newToken = ProcessTokenValue(jToken, regex, inputValues, typeMap);
                    preset[key] = newToken;
                }
            }
        }
        return JsonConvert.SerializeObject(cloned, Formatting.Indented);
    }

    private JToken ProcessTokenValue(JToken token,
        Regex regex,
        Dictionary<string, string> inputValues,
        Dictionary<string, Type> typeMap)
    {
        if (token.Type == JTokenType.String)
        {
            string currentPlaceholder = null;
            var option = this;
            var strVal = token.Value<string>();
            var newVal = regex.Replace(strVal, match =>
            {
                currentPlaceholder = match.Groups[1].Value;
                if (!inputValues.TryGetValue(currentPlaceholder, out var inputStr))
                {
                    // 从 Default 列表获取默认值
                    var fieldIndex = option.Field?.IndexOf(currentPlaceholder) ?? -1;
                    if (fieldIndex >= 0 && option.Default != null && fieldIndex < option.Default.Count)
                        inputStr = option.Default[fieldIndex];
                    else
                        return $"{{{currentPlaceholder}}}"; // 无默认值保持占位符
                }

                // 类型转换逻辑（参考网页4的 Embedding 技术中的动态类型映射）
                if (typeMap.TryGetValue(currentPlaceholder, out var targetType))
                {
                    try { return Convert.ChangeType(inputStr, targetType).ToString(); }
                    catch { return inputStr; }
                }
                return inputStr;
            });

            // 构造新 Token（保持类型一致性）
            if (typeMap.TryGetValue(currentPlaceholder, out var targetType) && targetType != typeof(string))
                return JToken.FromObject(Convert.ChangeType(newVal, targetType));
            else
                return new JValue(newVal);
        }
        return token.DeepClone();
    }

    private void ReplaceToken(JToken token,
        Regex regex,
        Dictionary<string, string> inputValues,
        Dictionary<string, Type> typeMap)
    {
        if (token is JValue { Type: JTokenType.String } val)
        {
            // 字符串处理逻辑
            HandleStringValue(val, regex, inputValues, typeMap);
        }
        else if (token is JArray arr)
        {
            // 创建临时副本避免修改遍历中的集合
            var tempArr = new JArray(arr);
            for (int i = 0; i < tempArr.Count; i++)
            {
                ReplaceToken(tempArr[i], regex, inputValues, typeMap);
            }
            arr.Replace(tempArr); // 批量替换保持父节点
        }
    }

    private void HandleStringValue(JToken token,
        Regex regex,
        Dictionary<string, string> inputValues,
        Dictionary<string, Type> typeMap)
    {
        if (token.Parent == null) // 防御性校验
        {
            Console.WriteLine($"警告：跳过无父节点的Token路径 {token.Path}");
            return;
        }

        var strVal = token.Value<string>();
        string currentPlaceholder = null;

        var newVal = regex.Replace(strVal, match =>
        {
            currentPlaceholder = match.Groups[1].Value;
            if (!inputValues.TryGetValue(currentPlaceholder, out var inputStr))
                return $"{{{currentPlaceholder}}}"; // 未匹配占位符保持原样

            // 类型转换逻辑
            if (typeMap.TryGetValue(currentPlaceholder, out var targetType))
            {
                try
                {
                    return Convert.ChangeType(inputStr, targetType).ToString();
                }
                catch
                {
                    return inputStr; // 转换失败保持原始输入[3](@ref)
                }
            }
            return inputStr;
        });

        if (newVal != strVal && currentPlaceholder != null)
        {
            try
            {
                // 根据类型重建 JToken[1,6](@ref)
                object convertedValue = newVal;
                if (typeMap.TryGetValue(currentPlaceholder, out var targetType)
                    && targetType != typeof(string))
                {
                    convertedValue = Convert.ChangeType(newVal, targetType);
                }

                // 通过父节点安全替换[6,7](@ref)
                token.Replace(JToken.FromObject(convertedValue));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"替换失败：{ex.Message}");
            }
        }
    }
}
