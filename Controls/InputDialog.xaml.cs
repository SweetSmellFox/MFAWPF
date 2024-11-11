using System.Windows;

namespace MFAWPF.Controls;

public partial class InputDialog : CustomWindow
{
    public new string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string InputText { get; set; } = "";

    public InputDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnOK(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}