namespace DocFlow.Engine;

/// <summary>Аудит в памяти + лог. В продакшене пишите события в таблицу.</summary>
public sealed class InMemoryProcessAudit(TimeProvider clock, ILogger<InMemoryProcessAudit> log) : IProcessAudit
{
    private readonly object _gate = new();
    private readonly List<ProcessEvent> _events = new();

    public void Record(Guid processId, string nodeId, string kind, string? message = null)
    {
        var e = new ProcessEvent(processId, clock.GetUtcNow().UtcDateTime, nodeId, kind, message);
        lock (_gate) _events.Add(e);
        log.LogInformation("[{Process}] {Node} {Kind} {Message}", processId, nodeId, kind, message);
    }

    public Task<IReadOnlyList<ProcessEvent>> ForAsync(Guid processId, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<ProcessEvent> list = _events.Where(e => e.ProcessId == processId).ToList();
            return Task.FromResult(list);
        }
    }
}
