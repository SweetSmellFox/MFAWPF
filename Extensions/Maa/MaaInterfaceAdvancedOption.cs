using MFAWPF.Helper;
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
// 深拷贝原始数据
        var cloned = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, JToken>>>(
            JsonConvert.SerializeObject(PipelineOverride)
        );
        var typeMap = GetTypeMap();
        var regex = new Regex(@"{(\w+)}", RegexOptions.Compiled);
        foreach (var preset in cloned.Values)
        {
            foreach (var key in preset.Keys.ToList())
            {
                var jToken = preset[key];
                var newToken = ProcessToken(jToken, regex, inputValues, typeMap);
                if (newToken != null)
                {
                    preset[key] = newToken;
                }
            }
        }
        var result = JsonConvert.SerializeObject(cloned, Formatting.Indented);
        Console.WriteLine(result);
        return result;
    }
// 统一处理各种类型的 Token，返回处理后的新 Token
    private JToken? ProcessToken(JToken? token, Regex regex, Dictionary<string, string> inputValues, Dictionary<string, Type> typeMap)
    {
        if (token == null) return null;
        switch (token.Type)
        {
            case JTokenType.String:
                return ProcessStringToken(token, regex, inputValues, typeMap);
            case JTokenType.Array:
                return ProcessArrayToken(token, regex, inputValues, typeMap);
            case JTokenType.Object:
                return ProcessObjectToken(token, regex, inputValues, typeMap);
            default:
                return token; // 其他类型直接返回原值
        }
    }
// 处理字符串类型的 Token
    private JToken ProcessStringToken(JToken token, Regex regex, Dictionary<string, string> inputValues, Dictionary<string, Type> typeMap)
    {
        var strVal = token.Value<string>();
        string currentPlaceholder = null;
        var newVal = regex.Replace(strVal, match =>
        {
            currentPlaceholder = match.Groups[1].Value;
// 首先尝试从输入值获取
            if (inputValues.TryGetValue(currentPlaceholder, out var inputStr))
            {
                return ApplyTypeConversion(inputStr, currentPlaceholder, typeMap);
            }
// 输入值不存在，尝试从默认值获取
            return GetDefaultValue(currentPlaceholder, typeMap);
        });
        if (newVal != strVal && currentPlaceholder != null)
        {
            try
            {
// 根据类型重建 JToken
                object convertedValue = newVal;
                if (typeMap.TryGetValue(currentPlaceholder, out var targetType) && targetType != typeof(string))
                {
                    convertedValue = Convert.ChangeType(newVal, targetType);
                }
                return JToken.FromObject(convertedValue);
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"创建 JToken 失败：{ex.Message}");
                return token; // 发生异常时返回原值
            }
        }
        return token; // 未发生变化时返回原值
    }
// 应用类型转换
    private string ApplyTypeConversion(string inputStr, string placeholder, Dictionary<string, Type> typeMap)
    {
        if (typeMap.TryGetValue(placeholder, out var targetType))
        {
            try
            {
                return Convert.ChangeType(inputStr, targetType).ToString();
            }
            catch
            {
// 类型转换失败，返回默认值
                return GetDefaultValue(placeholder, typeMap);
            }
        }
        return inputStr;
    }
// 获取默认值
    private string GetDefaultValue(string placeholder, Dictionary<string, Type> typeMap)
    {
// 从 Default 列表获取默认值
        var fieldIndex = Field?.IndexOf(placeholder) ?? -1;
        if (fieldIndex >= 0 && Default != null && fieldIndex < Default.Count)
        {
            string defaultValue = Default[fieldIndex];
// 如果有类型映射，尝试将默认值转换为目标类型
            if (typeMap.TryGetValue(placeholder, out var targetType) && targetType != typeof(string))
            {
                try
                {
                    return Convert.ChangeType(defaultValue, targetType).ToString();
                }
                catch
                {
// 转换失败，返回原始默认值
                    return defaultValue;
                }
            }
            return defaultValue;
        }
// 无默认值，保持占位符
        return $"{{{placeholder}}}";
    }
// 处理数组类型的 Token
    private JToken ProcessArrayToken(JToken token, Regex regex, Dictionary<string, string> inputValues, Dictionary<string, Type> typeMap)
    {
        var arr = (JArray)token;
        var newArr = new JArray();
        foreach (var item in arr)
        {
            var processedItem = ProcessToken(item, regex, inputValues, typeMap);
            if (processedItem != null)
            {
                newArr.Add(processedItem);
            }
        }
        return newArr;
    }
// 处理对象类型的 Token
    private JToken ProcessObjectToken(JToken token, Regex regex, Dictionary<string, string> inputValues, Dictionary<string, Type> typeMap)
    {
        var obj = (JObject)token;
        var newObj = new JObject();
        foreach (var property in obj.Properties())
        {
            var processedValue = ProcessToken(property.Value, regex, inputValues, typeMap);
            if (processedValue != null)
            {
                newObj[property.Name] = processedValue;
            }
        }
        return newObj;
    }
}
