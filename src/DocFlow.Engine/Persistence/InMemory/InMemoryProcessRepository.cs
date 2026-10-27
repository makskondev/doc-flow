namespace DocFlow.Engine;

/// <summary>Реализация для разработки и тестов. После перезапуска всё теряется.</summary>
public sealed class InMemoryProcessRepository : IProcessRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, DocumentProcess> _items = new();

    public Task<DocumentProcess?> GetAsync(Guid id, CancellationToken ct)
    {
        lock (_gate)
        {
            DocumentProcess? result = _items.TryGetValue(id, out var p) ? p.Clone() : null;
            return Task.FromResult(result);
        }
    }

    public Task<DocumentProcess?> FindLatestAsync(string sourceId, string externalId, CancellationToken ct)
    {
        lock (_gate)
        {
            DocumentProcess? result = _items.Values
                .Where(x => x.SourceId == sourceId && x.ExternalId == externalId)
                .OrderByDescending(x => x.Run)
                .FirstOrDefault()?.Clone();
            return Task.FromResult(result);
        }
    }

    public Task<bool> TryAddAsync(DocumentProcess process, CancellationToken ct)
    {
        lock (_gate)
        {
            var conflict = _items.Values.Any(x =>
                x.SourceId == process.SourceId && x.ExternalId == process.ExternalId
                && (x.Run == process.Run || !x.IsTerminal));
            if (conflict) return Task.FromResult(false);

            process.Version = 1;
            _items[process.Id] = process.Clone();
            return Task.FromResult(true);
        }
    }

    public Task SaveAsync(DocumentProcess process, CancellationToken ct)
    {
        lock (_gate)
        {
            if (!_items.TryGetValue(process.Id, out var current))
                throw new InvalidOperationException($"Процесс {process.Id} не найден");
            if (current.Version != process.Version)
                throw new ConcurrencyException($"Процесс {process.Id} изменён другим потоком");
            process.Version++;
            _items[process.Id] = process.Clone();
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<DocumentProcess>> ListAsync(ProcessStatus? status, CancellationToken ct)
        => Query(p => status is null || p.Status == status);

    public Task<IReadOnlyList<DocumentProcess>> ListReadyAsync(DateTime nowUtc, CancellationToken ct)
        => Query(p => p.Status == ProcessStatus.Running && (p.NextAttemptAt is null || p.NextAttemptAt <= nowUtc));

    public Task<IReadOnlyList<DocumentProcess>> ListWaitingAsync(string? signal, CancellationToken ct)
        => Query(p => p.Status == ProcessStatus.Waiting && (signal is null || p.WaitingFor == signal));

    public Task<IReadOnlyList<DocumentProcess>> ListWaitingExpiredAsync(DateTime nowUtc, CancellationToken ct)
        => Query(p => p.Status == ProcessStatus.Waiting && p.TimeoutAt is { } t && t <= nowUtc);

    public Task<IReadOnlyList<DocumentProcess>> ListUnnotifiedAttentionAsync(CancellationToken ct)
        => Query(p => p.Status == ProcessStatus.NeedsAttention && p.EscalationSentAt is null);

    private Task<IReadOnlyList<DocumentProcess>> Query(Func<DocumentProcess, bool> predicate)
    {
        lock (_gate)
        {
            IReadOnlyList<DocumentProcess> list = _items.Values.Where(predicate).Select(p => p.Clone()).ToList();
            return Task.FromResult(list);
        }
    }
}
