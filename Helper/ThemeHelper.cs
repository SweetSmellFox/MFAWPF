using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MFAWPF.Helper;

public class ThemeHelper
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";

    public static bool IsLightTheme()
    {
        return !IsDarkTheme();
    }

    public static bool IsDarkTheme()
    {
        var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        using (key)
        {
            var value = key?.GetValue(RegistryValueName);
            return value != null && (int)value == 0;
        }
    }
}