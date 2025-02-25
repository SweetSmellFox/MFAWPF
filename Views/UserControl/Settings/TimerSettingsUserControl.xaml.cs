using MFAWPF.Helper;
using MFAWPF.ViewModels.UserControl.Settings;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl.Settings;

public partial class TimerSettingsUserControl
{
    public TimerSettingsUserControlModel ViewModel { get; set; }
    public TimerSettingsUserControl()
    {
        ViewModel = Instances.TimerSettingsUserControlModel;
        DataContext = this;
        InitializeComponent();
    }
}
