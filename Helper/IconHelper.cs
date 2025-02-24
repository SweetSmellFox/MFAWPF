using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MFAWPF.Helper;

public class IconHelper
{
    private static readonly Lazy<BitmapSource> LazyIcon = new(LoadIconWithFallback);
    public static BitmapSource ICON => LazyIcon.Value;

    private static BitmapSource LoadIconWithFallback()
    {
        try
        {
            string exeDirectory = AppContext.BaseDirectory;
            string iconPath = Path.Combine(exeDirectory, "logo.ico");

            if (File.Exists(iconPath))
            {
                using var fileStream = File.OpenRead(iconPath);
                return CreateBitmapSource(fileStream);
            }

            var sri = Application.GetResourceStream(new Uri("pack://application:,,,/logo.ico"));
            if (sri != null)
            {
                using var resourceStream = sri.Stream;
                return CreateBitmapSource(resourceStream);
            }

            LoggerService.LogWarning("未找到内嵌图标资源");
            return CreateEmptyImage();
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"图标加载失败: {ex}");
            return CreateEmptyImage();
        }
    }

    private static BitmapSource CreateBitmapSource(Stream stream)
    {
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad
        );
        return decoder.Frames[0];
    }

    private static BitmapSource CreateEmptyImage()
    {
        return new BitmapImage(); // 返回空图像避免NullReference
    }
}
