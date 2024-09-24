
using MaaFramework.Binding;
using System.Text.Json.Serialization;
using MFAWPF.Utils;

namespace MFAWPF.Data;

public class Config
{
    public AdbCoreConfig Adb { get; set; } = new();
    public Win32CoreConfig Win32 { get; set; } = new();
    public string BasePath = MaaProcessor.ResourceBase;
    public bool IsConnected = false;
}

public class Win32CoreConfig
{
    public nint HWnd { get; set; }

    public Win32InputMethod Input { get; set; } = Win32InputMethod.Seize;

    public Win32ScreencapMethod ScreenCap { get; set; } = Win32ScreencapMethod.FramePool;

    // [JsonIgnore] public Win32ControllerTypes ControlType => Input | ScreenCap;
    public LinkOption Link { get; set; } = LinkOption.Start;
    public CheckStatusOption Check { get; set; } = CheckStatusOption.ThrowIfNotSucceeded;
}

public class AdbCoreConfig
{
    public string Adb { get; set; } = "adb";
    public string AdbAddress { get; set; } = "127.0.0.1:5555";
    public string AdbConfig { get; set; } = MaaProcessor.AdbConfig;
    public AdbInputMethods Input { get; set; } = AdbInputMethods.Maatouch;
    public AdbScreencapMethods ScreenCap { get; set; } = AdbScreencapMethods.RawWithGzip;

    // [JsonIgnore] public AdbControllerTypes ControlType => Input | ScreenCap;
}