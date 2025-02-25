using MFAWPF.Helper;
using MFAWPF.ViewModels.UserControl.Settings;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl.Settings;

public partial class ConnectSettingsUserControl
{
    public ConnectSettingsUserControlModel ViewModel { get; set; }
    public ConnectSettingsUserControl()
    {
        DataContext = this;
        ViewModel = Instances.ConnectSettingsUserControlModel;
        InitializeComponent();
    }
}
