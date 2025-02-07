using System.Globalization;
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class SubtractConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double originalWidth && parameter is string parameterString && double.TryParse(parameterString, out double subtractValue))
        {
            return originalWidth - subtractValue;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new Exception("Type not exists!");
    }
}