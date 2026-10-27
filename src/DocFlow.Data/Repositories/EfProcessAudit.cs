namespace DocFlow.Data;

/// <summary>
/// Аудит в таблице process_events. Запись синхронная и короткая (одна вставка); сбой аудита логируется и не прерывает обработку.
/// </summary>
public sealed class EfProcessAudit(
    IDbContextFactory<DocFlowDbContext> factory, TimeProvider clock, ILogger<EfProcessAudit> log) : IProcessAudit
{
    public void Record(Guid processId, string nodeId, string kind, string? message = null)
    {
        log.LogInformation("[{Process}] {Node} {Kind} {Message}", processId, nodeId, kind, message);
        try
        {
            using var db = factory.CreateDbContext();
            db.Events.Add(new ProcessEventEntity
            {
                ProcessId = processId,
                At = clock.GetUtcNow().UtcDateTime,
                NodeId = Truncate(nodeId, 128),
                Kind = Truncate(kind, 64),
                Message = message
            });
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Не удалось записать событие аудита {Kind} процесса {Process}", kind, processId);
        }
    }

    public async Task<IReadOnlyList<ProcessEvent>> ForAsync(Guid processId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = await db.Events.AsNoTracking()
            .Where(e => e.ProcessId == processId)
            .OrderBy(e => e.At).ThenBy(e => e.Id)
            .ToListAsync(ct);
        return rows.Select(e => new ProcessEvent(e.ProcessId, e.At, e.NodeId, e.Kind, e.Message)).ToList();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
