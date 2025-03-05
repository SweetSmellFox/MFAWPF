using HandyControl.Controls;

namespace MFAWPF.Helper;

public static class TaskManager
{
    /// <summary>
    /// 执行任务, 并带有更好的日志显示
    /// </summary>
    /// <param name="action">要执行的动作</param>
    /// <param name="name">日志显示名称</param>
    /// <param name="prompt">日志提示</param>
    /// <param name="catchException">是否捕获异常</param>
    public static void RunTask(
        Action action,
        string name = nameof(Action),
        string prompt = ">>> ",
        bool catchException = true)
    {
        Growl.Info($"{prompt}任务 {name} 开始.");

        if (catchException)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                GrowlHelper.Error($"{prompt}任务 {name} 失败.");
                LoggerService.LogError(e.ToString());
            }
        }
        else action();

        Growl.Info($"{prompt}任务 {name} 完成.");
    }

    /// <summary>
    /// 异步执行任务, 并带有更好的日志显示
    /// </summary>
    /// <param name="action">要执行的动作</param>
    /// <param name="handleError">处理异常的方法</param>
    /// <param name="name">任务名称</param>
    /// <param name="prompt">日志提示</param>
    /// <param name="catchException">是否捕获异常</param>
    /// <param name="shouldLog">异常是否写入日志</param>
    public async static Task RunTaskAsync(
        Action? action,
        Action? handleError = null,
        string name = nameof(Action),
        string prompt = ">>> ",
        bool catchException = true,
        bool shouldLog = true)
    {
        Console.WriteLine($"异步任务 {name} 开始.");
        if (catchException)
        {
            var task = Task.Run(action);
            await task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    handleError?.Invoke();
                    if (shouldLog)
                        LoggerService.LogError(t.Exception.GetBaseException());
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        else await Task.Run(action);

        Console.WriteLine($"{prompt}异步任务 {name} 已完成.");
    }

    public async static Task RunTaskAsync(
        Action? action,
        CancellationToken token,
        Action? handleError = null,
        string name = nameof(Action),
        string prompt = ">>> ",
        bool catchException = true,
        bool shouldLog = true)
    {
        Console.WriteLine($"异步任务 {name} 开始.");
        try
        {
            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                action?.Invoke();
            }, token);
        }
        catch (Exception ex) when (catchException && !(ex is OperationCanceledException))
        {
            handleError?.Invoke();
            if (shouldLog) LoggerService.LogError(ex.GetBaseException());
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        finally
        {
            Console.WriteLine($"{prompt}异步任务 {name} 已完成.");
        }
    }

    public async static Task<TResult> RunTaskAsync<TResult>(
        Func<TResult> function,
        CancellationToken token,
        Action<Exception>? handleError = null,
        string name = nameof(Action),
        bool catchException = true,
        bool shouldLog = true)
    {
        try
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                return function();
            }, token);
        }
        catch (Exception ex) when (catchException && !(ex is OperationCanceledException))
        {
            var baseEx = ex.GetBaseException();
            handleError?.Invoke(baseEx);
            if (shouldLog) LoggerService.LogError(baseEx);
            return default!; 
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        finally
        {
            Console.WriteLine($">>> 异步任务 {name} 已完成.");
        }
    }
}
