namespace DocFlow.Engine;

/// <summary>Опрашивает сигнальные источники (например, файлы подписи) и будит ожидающие процессы.</summary>
public sealed class SignalScanWorker(
    IEnumerable<ISignalSource> sources,
    IOptions<EngineOptions> options,
    ILogger<SignalScanWorker> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => PeriodicLoop.RunAsync(TimeSpan.FromSeconds(options.Value.SignalScanIntervalSeconds), ScanAsync, log, stoppingToken);

    private async Task ScanAsync(CancellationToken ct)
    {
        foreach (var source in sources)
        {
            try
            {
                await source.ScanAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Ошибка сканирования сигнала {Signal}", source.Signal);
            }
        }
    }
}
