using MFAWPF.Helper;
using MFAWPF.ViewModels.UserControl.Settings;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl.Settings;

public partial class GuiSettingsUserControl
{
    public GuiSettingsUserControlModel ViewModel { get; set; }
    public GuiSettingsUserControl()
    {
        ViewModel = Instances.GuiSettingsUserControlModel;
        DataContext = this;
        InitializeComponent();
    }
}

