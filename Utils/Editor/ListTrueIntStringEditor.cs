using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Utils.Editor;

namespace MFAWPF.Utils.Converters;

public class ListTrueIntStringEditor : ListIntStringEditor
{
    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new ListTrueIntStringConverter();
}