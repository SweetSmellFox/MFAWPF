using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace MFAWPF.Views.UserControl.Settings;

public partial class VersionUpdateSettingsUserControl 
{
    public VersionUpdateSettingsUserControl()
    {
        InitializeComponent();
    }
    
    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(((Hyperlink)sender).NavigateUri.AbsoluteUri)
        {
            UseShellExecute = true
        });
    }
    
    
}

