namespace DocFlow.Engine;

/// <summary>Опрашивает источники документов (каталоги, вьюха) и регистрирует новые процессы.</summary>
public sealed class IntakeWorker(
    IEnumerable<IDocumentSource> sources,
    DocumentRegistrar registrar,
    IOptions<EngineOptions> options,
    TimeProvider clock,
    ILogger<IntakeWorker> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => PeriodicLoop.RunAsync(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), PollAsync, log, stoppingToken);

    private async Task PollAsync(CancellationToken ct)
    {
        foreach (var source in sources)
        {
            var startedAt = clock.GetUtcNow().UtcDateTime;
            IReadOnlyList<DocumentCandidate> batch;
            try
            {
                batch = await source.PollAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Источник {Source} недоступен", source.SourceId);
                continue;
            }
            await registrar.RegisterAsync(batch, startedAt, ct);
        }
    }
}
