using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MFAWPF.Utils.Converters;

public class ListDoubleStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new ObservableCollection<CustomValue<string>>
            {
                new(d.ToString())
            };
        }

        if (value is IEnumerable<double> doubles)
        {
            return new ObservableCollection<CustomValue<string>>(
                doubles.Select(d => new CustomValue<string>(d.ToString())));
        }

        return new ObservableCollection<CustomValue<string>>();
    }


    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> customValueList)
        {
            var list = customValueList.ToList();

            try
            {
                var result = list
                    .Select(cv => double.Parse(cv.Value))
                    .ToList();
                if (result.Count == 1)
                    return result[0];
                return result.Count > 0 ? result : null;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        return null;
    }
}