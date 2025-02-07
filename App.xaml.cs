using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Lierda.WPFHelper;
using MaaFramework.Binding.Interop.Native;
using MFAWPF.Services;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace MFAWPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            var basePath =
                Path.GetDirectoryName(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException(
                    "Unable to find the base directory of the application."
                );
            _ = c.SetBasePath(basePath);
        })
        .ConfigureServices(
            (context, services) =>
            {
                // App Host
                _ = services.AddHostedService<ApplicationHostService>();

                // Main window with navigation
                _ = services.AddSingleton<Window, Views.MainWindow>();
                _ = services.AddSingleton<MainViewModel>();

                // Views and ViewModels
                _ = services.AddSingleton<Views.SettingsView>();
                _ = services.AddSingleton<ViewModels.SettingViewModel>();
                // _ = services.AddSingleton<Views.Pages.DataPage>();
                // _ = services.AddSingleton<ViewModels.DataPageViewModel>();
                // _ = services.AddSingleton<Views.Pages.SettingsPage>();
                // _ = services.AddSingleton<ViewModels.SettingsViewModel>();

            }
        )
        .Build();

    /// <summary>
    /// Gets services.
    /// </summary>
    public static IServiceProvider Services => Host.Services;


    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    async private void OnStartup(object sender, StartupEventArgs e)
    {
        var dllFolderPath = Path.Combine(AppContext.BaseDirectory, "DLL");
        if (Directory.Exists(dllFolderPath))
        {
            var dllFiles = Directory.GetFiles(dllFolderPath, "*.dll");
            foreach (var dllName in dllFiles)
            {
                var handle = LoadLibrary(dllName);
                if (handle == IntPtr.Zero)
                {
                    LoggerService.LogError($"Can't load {Path.GetFileName(dllName)}");
                }
            }
        }

        //Task线程内未捕获异常处理事件
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        //非UI线程未捕获异常处理事件
        AppDomain.CurrentDomain.UnhandledException +=
            CurrentDomain_UnhandledException;
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        var cracker = new LierdaCracker();
        cracker.Cracker();
        await Host.StartAsync();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    async private void OnExit(object sender, ExitEventArgs e)
    {
        await Host.StopAsync();

        Host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出      
            LoggerService.LogError(e.Exception);
            ErrorView.ShowException(e.Exception);
        }
        catch (Exception ex)
        {
            //此时程序出现严重异常，将强制结束退出
            LoggerService.LogError(ex.ToString());
            ErrorView.ShowException(ex, true);
        }
    }


    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private extern static IntPtr LoadLibrary(string lpFileName);


    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle or UserPreferenceCategory.General)
        {
            Views.MainWindow.FollowSystemTheme();
        }
    }

    void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var sbEx = new StringBuilder();
        if (e.IsTerminating)
        {
            sbEx.Append("非UI线程发生致命错误");
        }

        sbEx.Append("非UI线程异常：");
        if (e.ExceptionObject is Exception ex)
        {
            Console.WriteLine(ex);
            LoggerService.LogError(ex.ToString());
            ErrorView.ShowException(ex);
            sbEx.Append((ex).Message);
        }
        else
        {
            LoggerService.LogError(e.ExceptionObject);
            sbEx.Append(e.ExceptionObject);
        }

        GrowlHelper.Error(sbEx.ToString());
    }

    void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        //task线程内未处理捕获
        LoggerService.LogError(e.Exception);
        ErrorView.ShowException(e.Exception);
        foreach (var item in e.Exception.InnerExceptions)
        {
            LoggerService.LogError(string.Format("异常类型：{0}{1}来自：{2}{3}异常内容：{4}",
                item.GetType(), Environment.NewLine, item.Source,
                Environment.NewLine, item.Message));
        }

        e.SetObserved(); //设置该异常已察觉（这样处理后就不会引起程序崩溃）
    }
}
