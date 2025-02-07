using System.Globalization;
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class NullStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value?.ToString()) ? null : value;
    }
}