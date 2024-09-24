using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MFAWPF.Utils.Converters;

public class StringOrListIntStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<int> intCollection)
        {
            return new ObservableCollection<CustomValue<string>>()
            {
                new(string.Join(",", intCollection.ToList()))
            };
        }

        if (value is IEnumerable<IEnumerable<int>> intCCollection)
        {
            return new ObservableCollection<CustomValue<string>>(
                intCCollection.Select(ic => new CustomValue<string>(string.Join(',', ic.ToList()))));
        }

        if (value is string s)
        {
            return new ObservableCollection<CustomValue<string>>()
            {
                new(s)
            };
        }

        return new ObservableCollection<CustomValue<string>>();
    }


    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> customValueList)
        {
            try
            {
                if (customValueList.Count() == 1)
                    return customValueList.ToList()[0].Value ?? string.Empty;
                
                var result = customValueList
                    .Select(cv => (cv.Value ?? string.Empty)
                        .Split(',')
                        .Select(int.Parse)
                        .ToList())
                    .ToList();
                return result;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }


        return null;
    }
}