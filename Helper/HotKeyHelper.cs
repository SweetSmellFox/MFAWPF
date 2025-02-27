using MFAWPF.Helper.ValueType;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MFAWPF.Helper;

public static class HotKeyHelper
{
    #region Windows API 封装

    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint fsModifiers = 0;
        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            fsModifiers |= 0x0001; // MOD_ALT
        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            fsModifiers |= 0x0002; // MOD_CONTROL
        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            fsModifiers |= 0x0004; // MOD_SHIFT
        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            fsModifiers |= 0x0008; // MOD_WIN
        return fsModifiers;
    }

    #endregion

    #region 热键管理核心逻辑

    private static readonly Dictionary<int, Action> _hotkeyCallbacks = new();
    private static int _hotkeyIdCounter = 0x0000;

    public static bool RegisterHotKey(Window? window, MFAHotKey hotKey, Action callback)
    {
        if (window == null || hotKey.IsTip)
            return false;
        var hWnd = new WindowInteropHelper(window).EnsureHandle();
        var hwndSource = HwndSource.FromHwnd(hWnd);
        hwndSource?.AddHook(WndProc);

        var id = _hotkeyIdCounter++;
        var fsModifiers = ConvertModifiers(hotKey.Modifiers);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);

        if (!RegisterHotKey(hWnd, id, fsModifiers, vk))
        {
            var errorCode = Marshal.GetLastWin32Error();
            LoggerService.LogError($"热键注册失败 (ErrorCode: {errorCode})");
            return false;
        }

        _hotkeyCallbacks[id] = callback;
        return true;
    }

    public static void UnregisterHotKey(Window? window, MFAHotKey hotKey)
    {
        if (window == null || hotKey.IsTip)
            return;
        var hWnd = new WindowInteropHelper(window).Handle;
        foreach (var pair in _hotkeyCallbacks)
        {
            if (pair.Value == null) continue;
            UnregisterHotKey(hWnd, pair.Key);
            _hotkeyCallbacks.Remove(pair.Key);
        }
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeyCallbacks.TryGetValue(id, out var callback))
            {
                Application.Current.Dispatcher.Invoke(callback);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    #endregion

    #region 扩展方法（可选）

    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    public static bool IsHotKeyRegistered(this Window window, MFAHotKey hotKey)
    {
        var hWnd = new WindowInteropHelper(window).EnsureHandle();
        var fsModifiers = HotKeyHelper.ConvertModifiers(hotKey.Modifiers);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);

        // 使用临时 ID 尝试注册热键
        const int tempId = 0x0000; // 特殊 ID 用于检测
        bool success = HotKeyHelper.RegisterHotKeyInternal(hWnd, tempId, fsModifiers, vk);

        if (!success)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED)
            {
                return true;
            }
        }

        // 立即注销临时热键
        HotKeyHelper.UnregisterHotKeyInternal(hWnd, tempId);
        return false;
    }

    // 内部注册/注销方法复用 HotKeyHelper 的逻辑
    private static bool RegisterHotKeyInternal(IntPtr hWnd, int id, uint fsModifiers, uint vk)
    {
        return HotKeyHelper.RegisterHotKey(hWnd, id, fsModifiers, vk);
    }

    private static bool UnregisterHotKeyInternal(IntPtr hWnd, int id)
    {
        return HotKeyHelper.UnregisterHotKey(hWnd, id);
    }

    #endregion
}
