namespace DocFlow.Engine;

internal static class PeriodicLoop
{
    /// <summary>Выполняет body сразу и затем по таймеру. Ошибки логирует и не падает; выходит при остановке хоста.</summary>
    public static async Task RunAsync(TimeSpan interval, Func<CancellationToken, Task> body, ILogger log, CancellationToken stop)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                try
                {
                    await body(stop);
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Ошибка в цикле фонового воркера");
                }
            } while (await timer.WaitForNextTickAsync(stop));
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
    }
}
