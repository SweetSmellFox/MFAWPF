using MFAWPF.Helper;
using MFAWPF.ViewModels.UserControl.Settings;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace MFAWPF.Views.UserControl.Settings;

public partial class VersionUpdateSettingsUserControl 
{
    public  VersionUpdateSettingsUserControlModel ViewModel { get; set; }
    public VersionUpdateSettingsUserControl()
    {
        ViewModel = Instances.VersionUpdateSettingsUserControlModel;
        DataContext = this;
        InitializeComponent();
    }
}

