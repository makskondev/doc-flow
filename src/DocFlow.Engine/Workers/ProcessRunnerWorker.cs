namespace DocFlow.Engine;

/// <summary>
/// Единственный исполнитель шагов: подхватывает готовые процессы (в том числе после падения сервиса),
/// обрабатывает истёкшие таймауты и досылает неотправленные письма об эскалации.
/// </summary>
public sealed class ProcessRunnerWorker(
    IProcessRepository repo,
    ProcessEngine engine,
    IOptions<EngineOptions> options,
    TimeProvider clock,
    ILogger<ProcessRunnerWorker> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => PeriodicLoop.RunAsync(TimeSpan.FromSeconds(options.Value.RunnerIntervalSeconds), TickAsync, log, stoppingToken);

    private async Task TickAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var p in await repo.ListWaitingExpiredAsync(now, ct))
            await engine.TimeoutAsync(p.Id, ct);

        foreach (var p in await repo.ListUnnotifiedAttentionAsync(ct))
            await engine.NotifyPendingAsync(p.Id, ct);

        var ready = await repo.ListReadyAsync(now, ct);
        await Parallel.ForEachAsync(
            ready,
            new ParallelOptions { MaxDegreeOfParallelism = options.Value.MaxParallelProcesses, CancellationToken = ct },
            async (p, token) =>
            {
                try
                {
                    await engine.AdvanceAsync(p.Id, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Ошибка при обработке процесса {Id}", p.Id);
                }
            });
    }
}
