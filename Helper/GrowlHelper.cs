using System.Windows;
using HandyControl.Controls;
using HandyControl.Data;

namespace MFAWPF.Helper;

public static class GrowlHelper
{
    public static void Warning(string message, string token = "")
    {
        OnUIThread(() =>
        {
            Growl.Warning(new GrowlInfo
            {
                IsCustom = true,
                Message = message,
                WaitTime = 3,
                Token = token,
                IconKey = ResourceToken.WarningGeometry,
                IconBrushKey = ResourceToken.WarningBrush,
            });
        });
    }

    public static void WarningGlobal(string message, string token = "")
    {
        OnUIThread(() =>
        {
            Growl.InfoGlobal(new GrowlInfo
            {
                IsCustom = true,
                Message = message,
                WaitTime = 3,
                IconKey = ResourceToken.WarningGeometry,
                IconBrushKey = ResourceToken.WarningBrush,
                Token = token
            });
        });
    }

    public static void Error(string message, string token = "")
    {
        OnUIThread(() =>
        {
            Growl.Info(new GrowlInfo
            {
                IsCustom = true,
                Message = message,
                WaitTime = 6,
                IconKey = ResourceToken.ErrorGeometry,
                IconBrushKey = ResourceToken.DangerBrush,
                Icon = null,
                Token = token
            });

        });
    }

    public static void ErrorGlobal(string message, string token = "")
    {
        OnUIThread(() =>
        {
            Growl.InfoGlobal(new GrowlInfo
            {
                IsCustom = true,
                Message = message,
                WaitTime = 6,
                IconKey = ResourceToken.ErrorGeometry,
                IconBrushKey = ResourceToken.DangerBrush,
                Icon = null,
                Token = token
            });
        });
    }
    public static void InfoGlobal(string message, string token = "")
    {
        OnUIThread(() =>
        {
            Growl.InfoGlobal(message);
        });
    }

    public static void Info(string message, string token = "")
    {
        OnUIThread(() =>
        {
            Growl.Info(message);
        });
    }

    public static void OnUIThread(Action action)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }
}
