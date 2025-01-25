using MFAWPF.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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

    private void UpdateResource(object sender, RoutedEventArgs e)
    {
        VersionChecker.UpdateResourceAsync();
    }

    private void CheckResourceUpdate(object sender, RoutedEventArgs e)
    {
        VersionChecker.CheckResourceVersionAsync();
    }

    private void UpdateMFA(object sender, RoutedEventArgs e)
    {
        VersionChecker.UpdateMFAAsync();
    }

    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(((Hyperlink)sender).NavigateUri.AbsoluteUri)
        {
            UseShellExecute = true
        });
    }
    private void ExternalNotificationSendTest(object sender, RoutedEventArgs e)
    {
        MaaProcessor.ExternalNotificationAsync();
    }
}
