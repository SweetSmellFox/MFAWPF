using System.Windows;
using System.Windows.Data;
using Newtonsoft.Json;

namespace MFAWPF.Helper.Converters;

public class NullableUIntOrObjectConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null)
            return string.Empty;
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
    {
        var strValue = value as string;
        if (string.IsNullOrWhiteSpace(strValue))
            return null;
        if (uint.TryParse(strValue, out var result))
            return result;
        if (strValue.Contains('{') && strValue.Contains('}'))
        {
            return JsonConvert.DeserializeObject<UIntOrObjectConverter.WaitFreezes>(strValue);
        }

        return DependencyProperty.UnsetValue;
    }
}