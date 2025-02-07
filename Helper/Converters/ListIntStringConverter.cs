using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class ListIntStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
                intCCollection.Select(ic => new CustomValue<string>(string.Join(",", ic.ToList()))));
        }

        return new ObservableCollection<CustomValue<string>>();
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> customValueList)
        {
            var list = customValueList.ToList();
            if (list.Count == 1)
            {
                if (bool.TryParse(list[0].Value, out var result) && result)
                    return result;
            }

            try
            {
                var result = list
                    .Where(cv => cv.Value != null) 
                    .Select(cv => cv.Value?
                        .Split(',')
                        .Select(int.Parse)
                        .ToList())
                    .ToList();
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