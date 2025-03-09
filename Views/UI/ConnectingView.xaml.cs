using MFAWPF.Helper;
using MFAWPF.ViewModels.UI;



namespace MFAWPF.Views.UI;

public partial class ConnectingView
{
    public ConnectingViewModel ViewModel { get; set; }
    public ConnectingView()
    {
        ViewModel = Instances.ConnectingViewModel;
        DataContext = this;
        InitializeComponent();
    }
}
