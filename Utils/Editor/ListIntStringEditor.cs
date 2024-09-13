using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Utils.Converters;

namespace MFAWPF.Utils.Editor;

public class ListIntStringEditor : ListStringEditor
{
    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new ListIntStringConverter();
}