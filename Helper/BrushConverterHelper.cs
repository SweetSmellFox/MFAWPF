using System.Windows;
using System.Windows.Media;

namespace MFAWPF.Helper;

public static class BrushConverterHelper
{
    // 缓存转换器提升性能
    private static readonly BrushConverter _brushConverter = new BrushConverter();

    /// <summary>
    /// 将字符串转换为Brush，支持以下格式：
    /// 1. 颜色名称（如 "Red", "Blue"）
    /// 2. 十六进制格式（如 "#FF0000", "#80FF0000", "FF0000"）
    /// 3. RGB/A格式（如 "sc#1,0,0,1"）
    /// </summary>
    /// <param name="colorString">颜色字符串</param>
    /// <param name="defaultBrush">转换失败时返回的默认画刷</param>
    /// <returns>对应的SolidColorBrush</returns>
    public static Brush? ConvertToBrush(string colorString, Brush? defaultBrush = null)
    {
        if (string.IsNullOrWhiteSpace(colorString))
            return defaultBrush ?? Brushes.Transparent;

        // 统一处理字符串格式
        var normalizedString = NormalizeColorString(colorString);

        try
        {
            // 确保在UI线程执行（DependencyObject的线程要求）
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() =>
                    ConvertToBrushInternal(normalizedString, defaultBrush));
            }

            return ConvertToBrushInternal(normalizedString, defaultBrush);
        }
        catch (FormatException)
        {
            return defaultBrush;
        }
    }

    private static Brush? ConvertToBrushInternal(string normalizedString, Brush? defaultBrush = null)
    {
        // 尝试直接转换颜色名称
        if (TryConvertNamedColor(normalizedString, out var color) && color != Colors.Transparent)
            return new SolidColorBrush(color);

        // 尝试转换十六进制格式
        if (TryConvertHexColor(normalizedString, out color) && color != Colors.Transparent)
            return new SolidColorBrush(color);

        // 尝试系统画刷转换器
        try
        {
            return _brushConverter.ConvertFromString(normalizedString) as Brush ?? defaultBrush;
        }
        catch
        {
            return defaultBrush;
        }
    }

    private static string NormalizeColorString(string input)
    {
        // 移除空格并统一为小写
        var trimmed = input.Trim().ToLowerInvariant();

        // 自动添加#前缀（如果缺失）
        if (!trimmed.StartsWith("#") && IsHexFormat(trimmed))
        {
            return "#" + trimmed;
        }

        return trimmed;
    }

    private static bool IsHexFormat(string input)
    {
        // 验证是否为有效的十六进制格式
        return input.All(c => "0123456789abcdefABCDEF".Contains(c)) && (input.Length == 3 || input.Length == 4 || input.Length == 6 || input.Length == 8);
    }

    private static bool TryConvertNamedColor(string colorName, out Color color)
    {
        try
        {
            var colorObj = ColorConverter.ConvertFromString(colorName);
            if (colorObj is Color c)
            {
                color = c;
                return true;
            }
        }
        catch
        {
            // 忽略转换错误
        }

        color = Colors.Transparent;
        return false;
    }

    private static bool TryConvertHexColor(string hexString, out Color color)
    {
        try
        {
            hexString = hexString.TrimStart('#');

            // 处理不同长度的十六进制值
            var hex = hexString.Length switch
            {
                3 => $"FF{hexString[0]}{hexString[0]}{hexString[1]}{hexString[1]}{hexString[2]}{hexString[2]}",
                4 => $"{hexString[0]}{hexString[0]}{hexString[1]}{hexString[1]}{hexString[2]}{hexString[2]}{hexString[3]}{hexString[3]}",
                6 => $"FF{hexString}",
                8 => hexString,
                _ => throw new FormatException("Invalid hex format")
            };

            color = Color.FromArgb(
                (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16)),
                (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16)),
                (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16)),
                (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16)));

            return true;
        }
        catch
        {
            color = Colors.Transparent;
            return false;
        }
    }
}
