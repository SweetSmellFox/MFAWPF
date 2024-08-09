using System.Globalization;
using System.Windows.Data;

namespace MFAWPF.Utils.Converters;

public class CustomIsEnabledConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is bool? && values[1] is bool idle)
        {
            bool isChecked = (values[0] as bool?).GetValueOrDefault(false);
            return (isChecked && idle) || !isChecked;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null;
    }
}