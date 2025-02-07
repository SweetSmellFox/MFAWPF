
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Helper.Converters;

namespace MFAWPF.Helper.Editor;

public class NullableUIntStringEditor : NullableStringEditor
{
    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new NullableUIntStringConverter();
}