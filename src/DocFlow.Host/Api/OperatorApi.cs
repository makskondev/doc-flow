namespace DocFlow.Host;

/// <summary>Операторский API: просмотр процессов и команды для процессов в статусе NeedsAttention.</summary>
public static class OperatorApi
{
    public static IEndpointRouteBuilder MapOperatorApi(this IEndpointRouteBuilder app)
    {
        var processes = app.MapGroup("/processes");

        processes.MapGet("", async (IProcessRepository repo, ProcessStatus? status, CancellationToken ct)
            => Results.Ok(await repo.ListAsync(status, ct)));

        processes.MapGet("{id:guid}", async (Guid id, IProcessRepository repo, IProcessAudit audit, CancellationToken ct) =>
        {
            var process = await repo.GetAsync(id, ct);
            return process is null ? Results.NotFound() : Results.Ok(new { process, events = await audit.ForAsync(id, ct) });
        });

        processes.MapPost("{id:guid}/retry", async (Guid id, ProcessEngine engine, CancellationToken ct)
            => await engine.RetryAsync(id, ct) ? Results.Accepted() : Results.Conflict("Процесс не в статусе NeedsAttention"));

        processes.MapPost("{id:guid}/continue", async (Guid id, ProcessEngine engine, CancellationToken ct)
            => await engine.ContinueAsync(id, ct) ? Results.Accepted() : Results.Conflict("Процесс не в статусе NeedsAttention"));

        processes.MapPost("{id:guid}/cancel", async (Guid id, ProcessEngine engine, CancellationToken ct)
            => await engine.CancelAsync(id, ct) ? Results.Accepted() : Results.Conflict("Процесс уже завершён или не найден"));

        processes.MapPost("{id:guid}/restart", async (Guid id, DocumentRegistrar registrar, CancellationToken ct) =>
        {
            var newId = await registrar.RestartAsync(id, ct);
            return newId is null
                ? Results.Conflict("Перезапуск возможен только для отменённого процесса")
                : Results.Ok(new { processId = newId });
        });

        return app;
    }
}
