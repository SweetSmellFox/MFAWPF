using System.Windows;
using HandyControl.Controls;
using HandyControl.Data;


namespace MFAWPF.Helper;

public static class GrowlHelper
{
    public static void Warning(string message, string token = "")
    {
        DispatcherHelper.RunOnMainThread(() =>
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
        HandyControl.Tools.DispatcherHelper.RunOnMainThread(() =>
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
        DispatcherHelper.RunOnMainThread(() =>
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
        DispatcherHelper.RunOnMainThread(() =>
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
        DispatcherHelper.RunOnMainThread(() =>
        {
            Growl.InfoGlobal(message);
        });
    }

    public static void Info(string message, string token = "")
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            Growl.Info(message);
        });
    }
    
}
