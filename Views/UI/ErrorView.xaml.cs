using HandyControl.Tools;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace MFAWPF.Views.UI;

/// <summary>
///     ErrorView.xaml 的交互逻辑
/// </summary>
public partial class ErrorView : INotifyPropertyChanged
{
    protected bool ShouldExit { get; set; }

    public string ExceptionMessage { get; set; } = string.Empty;

    public string ExceptionDetails { get; set; } = string.Empty;

    public ErrorView()
    {
        InitializeComponent();
    }

    public static void ShowException(Exception e, bool shouldExit = false)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            var errorView = new ErrorView(e, shouldExit);
            errorView.Show();
        });
    }

    public ErrorView(Exception exc, bool shouldExit)
    {
        InitializeComponent();
        var exc0 = exc;
        var errorStr = new StringBuilder();
        while (true)
        {
            errorStr.Append(exc.Message);
            if (exc.InnerException is not null)
            {
                errorStr.AppendLine();
                exc = exc.InnerException;
            }
            else
            {
                break;
            }
        }

        var error = errorStr.ToString();
        var details = exc0.ToString();
        ExceptionMessage = error;
        ExceptionDetails = details;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExceptionMessage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExceptionDetails)));
        ShouldExit = shouldExit;

        // var isZhCn = ConfigurationHelper.GetValue(ConfigurationKeys.Localization, LocalizationHelper.DefaultLanguage) ==
        //              "zh-cn";
        // ErrorQqGroupLink.Visibility = isZhCn ? Visibility.Visible : Visibility.Collapsed;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected override void OnClosed(EventArgs e)
    {
        if (ShouldExit)
        {
            Environment.Exit(0);
        }

        base.OnClosed(e);
    }

    private void Hyperlink_OnClick(object sender, RoutedEventArgs _)
    {
        Process.Start(new ProcessStartInfo(((Hyperlink)sender).NavigateUri.AbsoluteUri)
        {
            UseShellExecute = true
        });
    }

    private void CopyToClipboard()
    {
        var range = new TextRange(RichTextBox.Document.ContentStart, RichTextBox.Document.ContentEnd);
        var data = new DataObject();
        data.SetText(range.Text);
        if (range.CanSave(DataFormats.Rtf))
        {
            var ms = new MemoryStream();
            range.Save(ms, DataFormats.Rtf);
            var arr = ms.ToArray();
            
            data.SetData(DataFormats.Rtf, Encoding.UTF8.GetString(arr));
        }

        try
        {
            Clipboard.SetDataObject(data, true);
        }
        catch
        {
            // 有时候报错了也能复制上去，这个时候复制不了也没办法了
        }
    }

    async private void CopyErrorMessage_Click(object sender, RoutedEventArgs e)
    {
        CopyToClipboard();
        CopiedTip.IsOpen = true;
        await Task.Delay(3000);
        CopiedTip.IsOpen = false;
    }
}
