using HandyControl.Data;
using System.Windows;

namespace MFAWPF.Helper;

public static class MessageBoxHelper
{
    /// <summary>
    /// OK text
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static string OK = "Ok";

    /// <summary>
    /// Cancel text
    /// </summary>
    public static string Cancel = "ButtonCancel";

    /// <summary>
    /// Abort text
    /// </summary>
    public static string Abort = "Abort";

    /// <summary>
    /// Retry text
    /// </summary>
    public static string Retry = "Retry";

    /// <summary>
    /// Ignore text
    /// </summary>
    public static string Ignore = "Ignore";

    /// <summary>
    /// Yes text
    /// </summary>
    public static string Yes = "Yes";

    /// <summary>
    /// No text
    /// </summary>
    public static string No = "No";

    private static void SetImage(MessageBoxImage messageBoxImage, ref string iconKey, ref string iconBrushKey)
    {
        var key = string.Empty;
        var brushKey = string.Empty;

        switch (messageBoxImage)
        {
            case MessageBoxImage.Question:
                key = ResourceToken.AskGeometry;
                brushKey = ResourceToken.AccentBrush;
                break;

            case MessageBoxImage.Error:
                key = ResourceToken.ErrorGeometry;
                brushKey = ResourceToken.DangerBrush;
                break;

            case MessageBoxImage.Warning:
                key = ResourceToken.WarningGeometry;
                brushKey = ResourceToken.WarningBrush;
                break;

            case MessageBoxImage.Information:
                key = ResourceToken.InfoGeometry;
                brushKey = ResourceToken.InfoBrush;
                break;
        }

        iconKey = string.IsNullOrEmpty(iconKey) ? key : iconKey;
        iconBrushKey = string.IsNullOrEmpty(iconBrushKey) ? brushKey : iconBrushKey;
    }

    public static MessageBoxResult Show(MessageBoxInfo info) => HandyControl.Controls.MessageBox.Show(info);

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption = "",
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        string iconKey = "",
        string iconBrushKey = "",
        string ok = "",
        string cancel = "",
        string yes = "",
        string no = "")
    {
        caption = string.IsNullOrEmpty(caption) ? "Tip".ToLocalization() : caption;
        ok = string.IsNullOrEmpty(ok) ? OK.ToLocalization() : ok;
        cancel = string.IsNullOrEmpty(cancel) ? Cancel.ToLocalization() : cancel;
        yes = string.IsNullOrEmpty(yes) ? Yes.ToLocalization() : yes;
        no = string.IsNullOrEmpty(no) ? No.ToLocalization() : no;

        SetImage(icon, ref iconKey, ref iconBrushKey);
        var info = new MessageBoxInfo
        {
            Message = messageBoxText,
            Caption = caption,
            Button = buttons,
            IconKey = iconKey,
            IconBrushKey = iconBrushKey,
            ConfirmContent = ok,
            CancelContent = cancel,
            YesContent = yes,
            NoContent = no,
        };
        return HandyControl.Controls.MessageBox.Show(info);
    }
}
