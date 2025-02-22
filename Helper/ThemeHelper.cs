using HandyControl.Themes;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MFAWPF.Helper;

public class ThemeHelper
{
    public static void UpdateThemeIndexChanged(int value)
    {
        switch (value)
        {
            case 0:
                ThemeManager.Current.UsingWindowsAppTheme = false;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                break;
            case 1:
                ThemeManager.Current.UsingWindowsAppTheme = false;
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                break;
            default:
                ThemeManager.Current.UsingWindowsAppTheme = true;
                break;
        }
    }
}
