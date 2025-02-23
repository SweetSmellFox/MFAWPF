using System.Windows;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl.Settings;

public partial class StartSettingsUserControl 
{
    public StartSettingsUserControl()
    {
        InitializeComponent();
    }
    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }
    private void File_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (sender is TextBox textBox)
            textBox.Text = files?[0] ?? string.Empty;
    }
}

