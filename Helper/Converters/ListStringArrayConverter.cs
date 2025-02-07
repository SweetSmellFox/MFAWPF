using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class ListStringArrayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string[]> ls)
        {
            return new ObservableCollection<CustomValue<string>>(
                ls.Select(array => new CustomValue<string>($"[{string.Join(",", array)}]")).ToList()
            );
        }

        return new ObservableCollection<CustomValue<string>>();
    }


    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> collection)
        {
            var result = collection.Select(customValue =>
            {
                var trimmed = customValue.Value?.Trim('[', ']');
                var splitArray = trimmed?.Split(",");
                return splitArray?.Length == 2 ? splitArray : null;
            }).ToList();

            if (result.Any(array => array == null))
            {
                return DependencyProperty.UnsetValue;
            }

            return result;
        }


        return null;
    }
}