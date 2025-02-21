using System.Windows;
using System.Windows.Threading;

namespace MFAWPF.Helper;

public static class DispatcherHelper
{
    public static void RunOnMainThread(Action action)
    {
        RunOnUIThread(Application.Current, action);
    }

    public static void RunOnUIThread(this DispatcherObject d, Action action)
    {
        var dispatcher = d?.Dispatcher;
        if (dispatcher!=null)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(action);
            }
        }
    }
}
