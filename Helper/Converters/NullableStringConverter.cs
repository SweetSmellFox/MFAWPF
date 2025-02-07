
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class NullableStringConverter : IValueConverter
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
        return strValue;
    }
}