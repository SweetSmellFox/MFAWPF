using MFAWPF.Helper;
using MFAWPF.ViewModels.UserControl.Settings;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl.Settings;

public partial class ExternalNotificationSettingsUserControl
{
    public ExternalNotificationSettingsUserControlModel ViewModel { get; set; }
    public ExternalNotificationSettingsUserControl()
    {
        DataContext = this;
        ViewModel = Instances.ExternalNotificationSettingsUserControlModel;
        InitializeComponent();
    }
}
