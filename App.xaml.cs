using System.Configuration;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using MFAWPF.Utils;
using HandyControl.Controls;
using HandyControl.Tools;
using WPFLocalizeExtension.Engine;

namespace MFAWPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        this.Startup += new StartupEventHandler(App_Startup);
        this.Exit += new ExitEventHandler(App_Exit);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
    }

    void App_Startup(object sender, StartupEventArgs e)
    {
        //UI线程未捕获异常处理事件
        this.DispatcherUnhandledException +=
            new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
        //Task线程内未捕获异常处理事件
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        //非UI线程未捕获异常处理事件
        AppDomain.CurrentDomain.UnhandledException +=
            new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
    }

    void App_Exit(object sender, ExitEventArgs e)
    {
        //程序退出时需要处理的业务
    }

    void App_DispatcherUnhandledException(object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出      
            Console.WriteLine(e.Exception);
        }
        catch (Exception ex)
        {
            //此时程序出现严重异常，将强制结束退出
            LoggerService.LogError(ex.ToString());
            Growls.Error("UI线程发生致命错误！");
        }
    }

    void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        StringBuilder sbEx = new StringBuilder();
        if (e.IsTerminating)
        {
            sbEx.Append("非UI线程发生致命错误");
        }

        sbEx.Append("非UI线程异常：");
        if (e.ExceptionObject is Exception ex)
        {
            Console.WriteLine(ex);
            LoggerService.LogError(ex.ToString());
            sbEx.Append((ex).Message);
        }
        else
        {
            LoggerService.LogError(e.ExceptionObject);
            sbEx.Append(e.ExceptionObject);
        }

        Growls.Error(sbEx.ToString());
    }

    void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        //task线程内未处理捕获
        Growls.Error("Task线程异常：" + e.Exception.Message);
        LoggerService.LogError(e.Exception);
        foreach (Exception item in e.Exception.InnerExceptions)
        {
            Console.WriteLine("异常类型：{0}{1}来自：{2}{3}异常内容：{4}",
                item.GetType(), Environment.NewLine, item.Source,
                Environment.NewLine, item.Message);
            LoggerService.LogError(string.Format("异常类型：{0}{1}来自：{2}{3}异常内容：{4}",
                item.GetType(), Environment.NewLine, item.Source,
                Environment.NewLine, item.Message));
        }

        e.SetObserved(); //设置该异常已察觉（这样处理后就不会引起程序崩溃）
    }
}