using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace MFAWPF.Helper.Converters;

public class ListStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string> stringCollection)
        {
            return new ObservableCollection<CustomValue<string>>(stringCollection
                .Select(s => new CustomValue<string>(s)).ToList());
        }

        if (value is string sx)
        {
            return new ObservableCollection<CustomValue<string>>
            {
                new(sx)
            };
        }

        return new ObservableCollection<CustomValue<string>>();
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> customValueList)
        {
            var list = customValueList.Select(cv => cv.Value).ToList();
            return list.Count > 0 ? list : null;
        }

        return null;
    }
}