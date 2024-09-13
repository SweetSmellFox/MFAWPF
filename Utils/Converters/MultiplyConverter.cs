using System.Globalization;
using System.Windows.Data;

namespace MFAWPF.Utils.Converters;

public class MultiplyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double originalWidth && parameter is string parameterString &&
            double.TryParse(parameterString, out double mValue))
        {
            return originalWidth * mValue;
        }

        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new Exception("Type not exists!");
    }
}