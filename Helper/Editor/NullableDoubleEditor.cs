
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Helper.Converters;

namespace MFAWPF.Helper.Editor;

public class NullableDoubleEditor : NullableStringEditor
{
    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new NullableDoubleConverter();
}