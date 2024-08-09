using System.IO;
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

    public Win32ControllerTypes Touch { get; set; } = Win32ControllerTypes.TouchSeize |
                                                      Win32ControllerTypes.KeySeize;

    public Win32ControllerTypes ScreenCap { get; set; } = Win32ControllerTypes.ScreencapDXGIFramePool;

    [JsonIgnore] public Win32ControllerTypes ControlType => Touch | ScreenCap;
    public LinkOption Link { get; set; } = LinkOption.Start;
    public CheckStatusOption Check { get; set; } = CheckStatusOption.ThrowIfNotSuccess;
}

public class AdbCoreConfig
{
    public string Adb { get; set; } = "adb";
    public string AdbAddress { get; set; } = "127.0.0.1:5555";
    public string AdbConfig { get; set; } = MaaProcessor.AdbConfig;
    public AdbControllerTypes Touch { get; set; } = AdbControllerTypes.InputPresetMaaTouch;
    public AdbControllerTypes ScreenCap { get; set; } = AdbControllerTypes.ScreencapRawWithGzip;

    [JsonIgnore] public AdbControllerTypes ControlType => Touch | ScreenCap;
}