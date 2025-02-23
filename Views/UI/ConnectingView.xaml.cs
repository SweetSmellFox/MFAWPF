using System.Windows;
using System.Windows.Controls;
using static MFAWPF.Views.UI.RootView;

namespace MFAWPF.Views.UI;

public partial class ConnectingView
{
    public ConnectingView()
    {
        InitializeComponent();
    }

    private void DeviceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Instance.DeviceComboBox_OnSelectionChanged();

    private void Refresh(object sender, RoutedEventArgs e) => Instance.AutoDetectDevice();

    private void CustomAdb(object sender, RoutedEventArgs e)
        => Instance.CustomAdb();
}
