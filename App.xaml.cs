using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using MFAWPF.Utils;
using HandyControl.Controls;

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
            LoggerService.Logger.LogError(ex.ToString());
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
            LoggerService.Logger.LogError(ex.ToString());
            sbEx.Append((ex).Message);
        }
        else
        {
            LoggerService.Logger.LogError(e.ToString());
            sbEx.Append(e.ExceptionObject);
        }

        Growls.Error(sbEx.ToString());
    }

    void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        //task线程内未处理捕获
        Growls.Error("Task线程异常：" + e.Exception.Message);
        LoggerService.Logger.LogError(e.ToString());
        e.SetObserved(); //设置该异常已察觉（这样处理后就不会引起程序崩溃）
    }
}