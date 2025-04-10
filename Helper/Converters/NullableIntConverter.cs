using System.Windows;
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class NullableIntConverter : IValueConverter
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
        if (int.TryParse(strValue, out var result))
            return result;
        return DependencyProperty.UnsetValue;
    }
}