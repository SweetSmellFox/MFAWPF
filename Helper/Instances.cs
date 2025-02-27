using System;
using System.Collections.Concurrent;
using MFAWPF.ViewModels;
using MFAWPF.ViewModels.UI;
using MFAWPF.ViewModels.UserControl.Settings;
using MFAWPF.Views.UI;
using MFAWPF.Views.UserControl.Settings;
using Microsoft.Extensions.DependencyInjection;
using RootViewModel = MFAWPF.ViewModels.UI.RootViewModel;
using SettingsViewModel = MFAWPF.ViewModels.UI.SettingsViewModel;

namespace MFAWPF.Helper;

/// <summary>
/// 全局服务访问点（线程安全版）
/// </summary>
public static class Instances
{
    #region Core Resolver

    private static readonly ConcurrentDictionary<Type, Lazy<object>> ServiceCache = new();

    /// <summary>
    /// 解析服务（自动缓存 + 循环依赖检测）
    /// </summary>
    private static T Resolve<T>() where T : class
    {
        if (App.Services == null)
            throw new InvalidOperationException("ServiceProvider not initialized!");

        var serviceType = typeof(T);
        var lazy = ServiceCache.GetOrAdd(serviceType, _ =>
            new Lazy<object>(
                () =>
                {
                    try
                    {
                        return App.Services.GetRequiredService<T>();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to resolve service {typeof(T).Name}. Possible causes: " + "1. Service not registered; " + "2. Circular dependency detected; " + "3. Thread contention during initialization.", ex);
                    }
                },
                LazyThreadSafetyMode.ExecutionAndPublication // 显式指定线程安全模式
            ));

        return (T)lazy.Value;
    }

    #endregion

    #region Public Accessors

    public static RootView RootView => Resolve<RootView>();
    public static RootViewModel RootViewModel => Resolve<RootViewModel>();
    public static TaskQueueView TaskQueueView => Resolve<TaskQueueView>();
    public static TaskQueueViewModel TaskQueueViewModel => Resolve<TaskQueueViewModel>();
    public static TaskQueueSettingsUserControl TaskQueueSettingsUserControl => Resolve<TaskQueueSettingsUserControl>();
    public static TaskOptionSettingsUserControl TaskOptionSettingsUserControl => Resolve<TaskOptionSettingsUserControl>();
    public static ConnectingView ConnectingView => Resolve<ConnectingView>();
    public static ConnectingViewModel ConnectingViewModel => Resolve<ConnectingViewModel>();
    
    public static AnnouncementViewModel AnnouncementViewModel => Resolve<AnnouncementViewModel>();
    public static SettingsView SettingsView => Resolve<SettingsView>();
    public static SettingsViewModel SettingsViewModel => Resolve<SettingsViewModel>();
    
    public static ConfigurationMgrUserControl ConfigurationMgrUserControl => Resolve<ConfigurationMgrUserControl>();
    public static ConnectSettingsUserControl ConnectSettingsUserControl => Resolve<ConnectSettingsUserControl>();
    public static ConnectSettingsUserControlModel ConnectSettingsUserControlModel => Resolve<ConnectSettingsUserControlModel>();
    public static ExternalNotificationSettingsUserControl ExternalNotificationSettingsUserControl => Resolve<ExternalNotificationSettingsUserControl>();
    public static ExternalNotificationSettingsUserControlModel ExternalNotificationSettingsUserControlModel => Resolve<ExternalNotificationSettingsUserControlModel>();
    public static HotKeySettingsUserControl HotKeySettingsUserControl => Resolve<HotKeySettingsUserControl>();
    public static GameSettingsUserControl GameSettingsUserControl => Resolve<GameSettingsUserControl>();
    public static GameSettingsUserControlModel GameSettingsUserControlModel => Resolve<GameSettingsUserControlModel>();
    public static GuiSettingsUserControl GuiSettingsUserControl => Resolve<GuiSettingsUserControl>();
    public static GuiSettingsUserControlModel GuiSettingsUserControlModel => Resolve<GuiSettingsUserControlModel>();
    public static PerformanceUserControl PerformanceUserControl => Resolve<PerformanceUserControl>();
    public static PerformanceUserControlModel PerformanceUserControlModel => Resolve<PerformanceUserControlModel>();
    public static StartSettingsUserControl StartSettingsUserControl => Resolve<StartSettingsUserControl>();
    public static StartSettingsUserControlModel StartSettingsUserControlModel => Resolve<StartSettingsUserControlModel>();
    public static TimerSettingsUserControl TimerSettingsUserControl => Resolve<TimerSettingsUserControl>();
    public static TimerSettingsUserControlModel TimerSettingsUserControlModel => Resolve<TimerSettingsUserControlModel>();
    public static VersionUpdateSettingsUserControl VersionUpdateSettingsUserControl => Resolve<VersionUpdateSettingsUserControl>();
    public static VersionUpdateSettingsUserControlModel VersionUpdateSettingsUserControlModel => Resolve<VersionUpdateSettingsUserControlModel>();
    public static AboutUserControl AboutUserControl => Resolve<AboutUserControl>();
    #endregion

    #region Lifetime Management

    public static void ClearCache<T>() where T : class =>
        ServiceCache.TryRemove(typeof(T), out _);

    public static void ClearAllCache() =>
        ServiceCache.Clear();

    #endregion
}
