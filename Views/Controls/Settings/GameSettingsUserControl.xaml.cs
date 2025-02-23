using System.Windows;

namespace MFAWPF.Views.Controls.Settings;

public partial class GameSettingsUserControl
{
    public GameSettingsUserControl()
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
        BeforeTaskSettings.Text = files?[0] ?? string.Empty;
    }

    private void EndsWithScript_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        AfterTaskSettings.Text = files?[0] ?? string.Empty;
    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }
}

