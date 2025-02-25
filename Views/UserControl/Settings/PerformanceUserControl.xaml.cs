using MFAWPF.Helper;
using MFAWPF.ViewModels.UserControl.Settings;
using System.Windows.Controls;

namespace MFAWPF.Views.UserControl.Settings;

public partial class PerformanceUserControl
{
    public PerformanceUserControlModel ViewModel { get; set; }
    public PerformanceUserControl()
    {
        ViewModel = Instances.PerformanceUserControlModel;
        DataContext = this;
        InitializeComponent();
    }
}

