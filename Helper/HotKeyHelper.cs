using MFAWPF.Helper.ValueType;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MFAWPF.Helper;

public static class HotKeyHelper
{
    #region Windows API 封装
    private const int WM_HOTKEY = 0x0312;
    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;
    private const int ERROR_ACCESS_DENIED = 5;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("kernel32", SetLastError = true)]
    private static extern ushort GlobalAddAtom(string lpString);

    [DllImport("kernel32", SetLastError = true)]
    private static extern ushort GlobalDeleteAtom(ushort nAtom);

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint fsModifiers = 0;
        if ((modifiers & ModifierKeys.Alt) != 0) fsModifiers |= 0x0001;
        if ((modifiers & ModifierKeys.Control) != 0) fsModifiers |= 0x0002;
        if ((modifiers & ModifierKeys.Shift) != 0) fsModifiers |= 0x0004;
        if ((modifiers & ModifierKeys.Windows) != 0) fsModifiers |= 0x0008;
        return fsModifiers;
    }
    #endregion
    

    #region 热键管理
    private static readonly Dictionary<int, Action> _hotkeyCallbacks = new();
    private static int _hotkeyIdCounter = 0x0000;
    private static readonly Dictionary<Tuple<uint, uint>, int> _registeredKeyCombos = new();
        
    public static bool RegisterHotKey(Window? window, MFAHotKey hotKey, Action callback)
    {
        if (window == null || hotKey.IsTip)
            return false;

        var hWnd = new WindowInteropHelper(window).EnsureHandle();
        var fsModifiers = ConvertModifiers(hotKey.Modifiers);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);
        var keyCombo = Tuple.Create(fsModifiers, vk);
        
        if (_registeredKeyCombos.ContainsKey(keyCombo))
        {
            LoggerService.LogWarning($"重复注册热键: {hotKey}");
            return false;
        }

        var id = _hotkeyIdCounter++;
        if (!RegisterHotKey(hWnd, id, fsModifiers, vk))
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED)
                LoggerService.LogError($"热键已被其他程序占用: {hotKey}");
            return false;
        }
        
        _registeredKeyCombos[keyCombo] = id;
        _hotkeyCallbacks[id] = callback;
        return true;
    }

    public static void UnregisterHotKey(Window? window, MFAHotKey hotKey)
    {
        if (window == null || hotKey.IsTip)
            return;

        var hWnd = new WindowInteropHelper(window).Handle;
        var fsModifiers = ConvertModifiers(hotKey.Modifiers);
        var vk = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);
        var keyCombo = Tuple.Create(fsModifiers, vk);

        if (_registeredKeyCombos.TryGetValue(keyCombo, out int id))
        {
            UnregisterHotKey(hWnd, id);
            
            _registeredKeyCombos.Remove(keyCombo);
            _hotkeyCallbacks.Remove(id);
        }
    }
    #endregion
}