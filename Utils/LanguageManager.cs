using System.Globalization;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;

namespace MFAWPF.Utils;

public static class LanguageManager
{
    public static event EventHandler? LanguageChanged;

    public static void ChangeLanguage(CultureInfo newCulture)
    {
        // 设置应用程序的文化
        LocalizeDictionary.Instance.Culture = newCulture;
        // 触发语言变化事件
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }
}