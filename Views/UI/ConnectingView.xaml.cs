using HandyControl.Controls;
using MaaFramework.Binding;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using MFAWPF.ViewModels.UI;
using MFAWPF.Views.UI.Dialog;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using WPFLocalizeExtension.Extensions;


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
