using System.Globalization;
using System.Windows.Data;

namespace MFAWPF.Utils.Converters;

public class MultiBoolAndOrConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValues = values.OfType<bool>().ToArray();
        
        if (parameter is string operation && operation.Equals("Or", StringComparison.OrdinalIgnoreCase))
        {
            return boolValues.Any(v => v); // 逻辑或
        }

        return boolValues.All(v => v); // 逻辑与
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null;
    }
}