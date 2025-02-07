using System.Windows;
using System.Windows.Media.Imaging;

namespace MFAWPF.Helper;

public class IconHelper
{
    private static readonly Lazy<BitmapSource> lazyIcon = new(ExtractIcon);

    private static BitmapSource ExtractIcon()
    {
        try
        {
            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/logo.ico"));
            if (sri == null)
            {
                return new BitmapImage();
            }

            using var s = sri.Stream;
            var decoder = BitmapDecoder.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var imageSource = decoder.Frames[0];

            return imageSource;
        }
        catch (Exception e)
        {
            LoggerService.LogError(e);
            return new BitmapImage();
        }
    }

    public static BitmapSource ICON => lazyIcon.Value;
}
