using MaaFramework.Binding;
using MFAWPF.Utils;

namespace MFAWPF.Data;

public class Config
{
    public AdbDeviceCoreConfig AdbDevice { get; set; } = new();
    public DesktopWindowCoreConfig DesktopWindow { get; set; } = new();
    public string BasePath = MaaProcessor.ResourceBase;
    public bool IsConnected = false;
}

public class DesktopWindowCoreConfig
{
    public nint HWnd { get; set; }

    public Win32InputMethod Input { get; set; } = Win32InputMethod.Seize;

    public Win32ScreencapMethod ScreenCap { get; set; } = Win32ScreencapMethod.FramePool;
    public LinkOption Link { get; set; } = LinkOption.Start;
    public CheckStatusOption Check { get; set; } = CheckStatusOption.ThrowIfNotSucceeded;
}

public class AdbDeviceCoreConfig
{
    public string Name { get; set; } = string.Empty;
    public string AdbPath { get; set; } = "adb";
    public string AdbSerial { get; set; } = "";
    public string Config { get; set; } = "{}";
    public AdbInputMethods Input { get; set; } = AdbInputMethods.Maatouch;
    public AdbScreencapMethods ScreenCap { get; set; } = AdbScreencapMethods.Default;
}