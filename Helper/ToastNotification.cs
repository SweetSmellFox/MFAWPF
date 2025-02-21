using MFAWPF.Helper.Notification;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications.Management;

namespace MFAWPF.Helper;

public class ToastNotification : IDisposable
{
    private unsafe struct OSVERSIONINFOEXW
    {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        private fixed char _szCSDVersion[128];
        public short wServicePackMajor;
        public short wServicePackMinor;
        public short wSuiteMask;
        public byte wProductType;
        public byte wReserved;

        public Span<char> szCSDVersion => MemoryMarshal.CreateSpan(ref _szCSDVersion[0], 128);
    }

    [DllImport("ntdll.dll", ExactSpelling = true)]
    private static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInfo);

    private static INotificationPoster _notificationPoster;


    static ToastNotification()
    {
        _notificationPoster = GetNotificationPoster();
        _notificationPoster.ActionActivated += OnActionActivated;
    }

    private static unsafe bool IsWindows10OrGreater()
    {
        var osVersionInfo = default(OSVERSIONINFOEXW);
        osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);
        RtlGetVersion(ref osVersionInfo);
        var version = new Version(osVersionInfo.dwMajorVersion, osVersionInfo.dwMinorVersion, osVersionInfo.dwBuildNumber);
        return version > new Version(10, 0, 10240);
    }

    public static event EventHandler<string>? ActionActivated;

    private string _notificationTitle = string.Empty;

    private StringBuilder _contentCollection = new StringBuilder();


    public ToastNotification(string? title = null)
    {
        // 初始化通知标题
        _notificationTitle = title ?? _notificationTitle;
    }

    private static void OnActionActivated(object sender, string tag)
    {

        ActionActivated?.Invoke(sender, tag);

    }


    /// <summary>
    /// 添加一行文本内容
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <returns>返回本类，可继续调用其它方法</returns>
    public ToastNotification AppendContentText(string? text = null)
    {
        _contentCollection.AppendLine(text ?? string.Empty);
        return this;
    }

    public enum NotificationSounds
    {
        /// <summary>
        /// 默认响声。
        /// </summary>
        [Description("默认响声")] Beep,

        /// <summary>
        /// 感叹号。
        /// </summary>
        [Description("感叹号")] Exclamation,

        /// <summary>
        /// 星号。
        /// </summary>
        [Description("星号")] Asterisk,

        /// <summary>
        /// 问题。
        /// </summary>
        [Description("问题")] Question,

        /// <summary>
        /// 关键性停止。
        /// </summary>
        [Description("关键性停止")] Hand,

        /// <summary>
        /// 通知 (Windows 10 及以上特有，低版本系统直接用也可以)。
        /// </summary>
        [Description("通知 (Windows 10 及以上特有，低版本系统直接用也可以)")]
        Notification,

        /// <summary>
        /// 无声。
        /// </summary>
        [Description("无声")] None,
    }

    protected static void PlayNotificationSound(NotificationSounds sounds = NotificationSounds.None)
    {
        try
        {
            switch (sounds)
            {
                case NotificationSounds.Exclamation: SystemSounds.Exclamation.Play(); break;
                case NotificationSounds.Asterisk: SystemSounds.Asterisk.Play(); break;
                case NotificationSounds.Question: SystemSounds.Question.Play(); break;
                case NotificationSounds.Beep: SystemSounds.Beep.Play(); break;
                case NotificationSounds.Hand: SystemSounds.Hand.Play(); break;

                case NotificationSounds.Notification:
                    using (var key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                    {
                        if (key != null)
                        {
                            // 获取 (Default) 项
                            var o = key.GetValue(null);
                            if (o != null)
                            {
                                var theSound = new SoundPlayer((string)o);
                                theSound.Play();
                            }
                        }
                        else
                        {
                            // 如果不支持就播放其它提示音
                            PlayNotificationSound(NotificationSounds.Asterisk);
                        }
                    }

                    break;

                case NotificationSounds.None:
                default: break;
            }
        }
        catch (Exception)
        {
            // Ignore
        }
    }
    private List<NotificationAction> _actions = new List<NotificationAction>();


    public ToastNotification AddButton(string label, string tag)
    {
        _actions.Add(new NotificationAction(label, tag));
        return this;
    }
    private static INotificationPoster GetNotificationPoster()
    {

        if (IsWindows10OrGreater())
        {
            return new NotificationImplWinRT();
        }

        return new NotificationImplWpf();
    }

    public void Show(double lifeTime = 10d,
        uint row = 1,
        NotificationSounds sound = NotificationSounds.Notification,
        params NotificationHint[] hints)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {

            var content = new NotificationContent
            {
                Summary = _notificationTitle,
                Body = _contentCollection.ToString(),
            };

            foreach (var action in _actions)
            {
                content.Actions.Add(action);
            }

            content.Hints.Add(NotificationHint.RowCount((int)row));

            // 调整显示时间，如果存在按钮的情况下显示时间将强制设为最大时间
            lifeTime = lifeTime < 3d ? 3d : lifeTime;

            var timeSpan = _actions.Count != 0
                ? TimeSpan.FromSeconds(lifeTime)
                : TimeSpan.MaxValue;

            content.Hints.Add(NotificationHint.ExpirationTime(timeSpan));

            foreach (var hint in hints)
            {
                content.Hints.Add(hint);
            }

            // 显示通知
            _notificationPoster.ShowNotification(content);

            // 播放通知提示音
            PlayNotificationSound(sound);

            // 任务栏闪烁
            FlashWindowEx();
        });
    }

    async private Task<bool> CheckNotificationSettingsAsync()
    {
        // 检查当前系统是否支持 UserNotificationListener
        if (ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
        {
            // 获取 UserNotificationListener 实例
            var listener = UserNotificationListener.Current;

            // 检查通知访问状态
            var accessStatus = await listener.RequestAccessAsync();
            // var quietHoursStatus = listener.GetSettings().QuietHoursStatus;
            // switch (quietHoursStatus)
            // {
            //     case QuietHoursStatus.Enabled:
            //         MessageBox.Show("免打扰模式已开启");
            //         break;
            //     case QuietHoursStatus.Disabled:
            //         MessageBox.Show("免打扰模式未开启");
            //         break;
            //     case QuietHoursStatus.Unknown:
            //         MessageBox.Show("无法确定免打扰模式状态");
            //         break;
            // }
            return accessStatus switch
            {
                UserNotificationListenerAccessStatus.Allowed => true,
                UserNotificationListenerAccessStatus.Denied => false,
                UserNotificationListenerAccessStatus.Unspecified => false,
                _ => false,
            };
        }

        return false;

    }
    public void ShowMore(double lifeTime = 12d,
        uint row = 2,
        NotificationSounds sound = NotificationSounds.None,
        params NotificationHint[] hints)
    {
        var morehints = new NotificationHint[hints.Length + 1];
        hints.CopyTo(morehints, 0);
        morehints[hints.Length] = NotificationHint.Expandable;
        Show(lifeTime: lifeTime,
            row: row,
            sound: sound,
            hints: morehints);
    }


    public void ShowUpdateVersion(uint row = 3)
    {
        ShowMore(row: row,
            sound: NotificationSounds.Notification,
            hints: NotificationHint.NewVersion);
    }

    public static void ShowDirect(string message)
    {
        using var toast = new ToastNotification(message);
        toast.Show();
    }


    public void Dispose()
    {
        _contentCollection.Clear();
    }

    public static void Cleanup()
    {
        try
        {
            (_notificationPoster as IDisposable)?.Dispose();
        }
        catch (Exception e)
        {
            LoggerService.LogError(e);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct FLASHWINFO
    {
        /// <summary>
        /// 结构大小
        /// </summary>
        public uint cbSize;

        /// <summary>
        /// 要闪烁或停止的窗口句柄
        /// </summary>
        public IntPtr hwnd;

        /// <summary>
        /// 闪烁的类型
        /// </summary>
        public uint dwFlags;

        /// <summary>
        /// 闪烁窗口的次数
        /// </summary>
        public uint uCount;

        /// <summary>
        /// 窗口闪烁的频率（毫秒）
        /// </summary>
        public uint dwTimeout;
    }

#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    /// <summary>
    /// 闪烁类型。
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum FlashType : uint
    {
        /// <summary>
        /// 停止闪烁。
        /// </summary>
        [Description("停止闪烁")] FLASHW_STOP = 0,

        /// <summary>
        /// 只闪烁标题。
        /// </summary>
        [Description("只闪烁标题")] FALSHW_CAPTION = 1,

        /// <summary>
        /// 只闪烁任务栏。
        /// </summary>
        [Description("只闪烁任务栏")] FLASHW_TRAY = 2,

        /// <summary>
        /// 标题和任务栏同时闪烁。
        /// </summary>
        [Description("标题和任务栏同时闪烁")] FLASHW_ALL = 3,

        /// <summary>
        /// 与 <see cref="FLASHW_TRAY"/> 配合使用。
        /// </summary>
        FLASHW_PARAM1 = 4,

        /// <summary>
        /// 与 <see cref="FLASHW_TRAY"/> 配合使用。
        /// </summary>
        FLASHW_PARAM2 = 12,

        /// <summary>
        /// 闪烁直到达到次数或收到停止。
        /// </summary>
        [Description("闪烁直到达到次数或收到停止")] FLASHW_TIMER = FLASHW_TRAY | FLASHW_PARAM1,

        /// <summary>
        /// 未激活时闪烁直到窗口被激活或收到停止。
        /// </summary>
        [Description("未激活时闪烁直到窗口被激活或收到停止")] FLASHW_TIMERNOFG = FLASHW_TRAY | FLASHW_PARAM2,
    }

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    /// <summary>
    /// 闪烁窗口任务栏
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="type">闪烁类型</param>
    /// <param name="count">闪烁次数</param>
    /// <returns>是否成功</returns>
    public static bool FlashWindowEx(IntPtr hWnd = default, FlashType type = FlashType.FLASHW_TIMERNOFG, uint count = 5)
    {
        var fInfo = default(FLASHWINFO);
        fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
        fInfo.hwnd = hWnd != default ? hWnd : new WindowInteropHelper(System.Windows.Application.Current.MainWindow!).Handle;
        fInfo.dwFlags = (uint)type;
        fInfo.uCount = count;
        fInfo.dwTimeout = 0;
        return FlashWindowEx(ref fInfo);
    }
}
