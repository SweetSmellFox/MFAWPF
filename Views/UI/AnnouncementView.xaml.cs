using MFAWPF.Helper;
using MFAWPF.ViewModels.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MFAWPF.Views.UI;

public partial class AnnouncementView
{
    public AnnouncementViewModel ViewModel { get; set; }
    public AnnouncementView()
    {
        DataContext = this;
        ViewModel = Instances.AnnouncementViewModel;
        InitializeComponent();
    }

    public void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
        };
        var parent = ((Control)sender).Parent as UIElement;
        parent?.RaiseEvent(eventArg);
    }
    private void Close(object sender, RoutedEventArgs e) => Close();
}
