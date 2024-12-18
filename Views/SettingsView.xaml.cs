using System.Windows;
using System.Windows.Controls;

namespace MFAWPF.Views;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
    }
    
    private void StartsWithScript_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        beforeTaskSettings.Text = files?[0] ?? string.Empty;
    }

    private void EndsWithScript_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        afterTaskSettings.Text = files?[0] ?? string.Empty;
    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }
}

