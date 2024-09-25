using System.Windows;
using HandyControl.Controls;
using HandyControl.Data;

namespace MFAWPF.Utils;

public static class Growls
{
    public static void Warning(string message, string token = "")
    {
        Process(() =>
        {
            Growl.Warning(new GrowlInfo
            {
                Message = message, WaitTime = 3,
                Token = token
            });
        });
    }

    public static void WarningGlobal(string message, string token = "")
    {
        Process(() =>
        {
            Growl.WarningGlobal(new GrowlInfo
            {
                Message = message, WaitTime = 3,
                Token = token
            });
        });
    }

    public static void Error(string message, string token = "")
    {
        Process(() =>
        {
            Growl.Warning(new GrowlInfo
            {
                Message = message, WaitTime = 6, IconKey = ResourceToken.ErrorGeometry,
                IconBrushKey = ResourceToken.DangerBrush, Icon = null,
                Token = token
            });
        });
    }

    public static void ErrorGlobal(string message, string token = "")
    {
        Process(() =>
        {
            Growl.WarningGlobal(new GrowlInfo
            {
                Message = message, WaitTime = 6, IconKey = ResourceToken.ErrorGeometry,
                IconBrushKey = ResourceToken.DangerBrush, Icon = null,
                Token = token
            });
        });
    }

    public static void Process(Action action)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }
}