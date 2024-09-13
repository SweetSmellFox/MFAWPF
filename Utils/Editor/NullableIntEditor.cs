using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;
using MFAWPF.Utils.Converters;

namespace MFAWPF.Utils.Editor;

public class NullableIntEditor : NullableStringEditor
{
    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new NullableIntConverter();
}