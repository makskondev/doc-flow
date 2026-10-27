using Npgsql;

namespace DocFlow.Data;

/// <summary>
/// Хранилище процессов на PostgreSQL. На каждую операцию — отдельный короткоживущий DbContext (репозиторий singleton).
/// Возвращаемые процессы не отслеживаются (detached): движок меняет их и сохраняет через <see cref="SaveAsync"/>.
/// </summary>
public sealed class EfProcessRepository(IDbContextFactory<DocFlowDbContext> factory) : IProcessRepository
{
    /// <summary>Ограничение пачки для раннера: при большом хвосте берём самые старые, остальное — на следующем тике.</summary>
    private const int ReadyBatchSize = 500;

    public async Task<DocumentProcess?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Processes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<DocumentProcess?> FindLatestAsync(string sourceId, string externalId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Processes.AsNoTracking()
            .Where(p => p.SourceId == sourceId && p.ExternalId == externalId)
            .OrderByDescending(p => p.Run)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> TryAddAsync(DocumentProcess process, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        process.Version = 1;
        db.Processes.Add(process);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;   // процесс уже есть (дубль прогона или активный процесс на этот документ)
        }
    }

    public async Task SaveAsync(DocumentProcess process, CancellationToken ct)
    {
        var expected = process.Version;

        await using var db = await factory.CreateDbContextAsync(ct);
        var entry = db.Attach(process);                 // исходное значение Version = expected
        entry.State = EntityState.Modified;             // пишем все поля
        process.Version = expected + 1;                 // новое значение; в WHERE уйдёт исходное (токен конкурентности)

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            process.Version = expected;
            throw new ConcurrencyException($"Процесс {process.Id} изменён другим потоком или удалён");
        }
    }

    public Task<IReadOnlyList<DocumentProcess>> ListAsync(ProcessStatus? status, CancellationToken ct)
        => QueryAsync(q => status is null ? q : q.Where(p => p.Status == status.Value), ct);

    public Task<IReadOnlyList<DocumentProcess>> ListReadyAsync(DateTime nowUtc, CancellationToken ct)
        => QueryAsync(q => q
            .Where(p => p.Status == ProcessStatus.Running && (p.NextAttemptAt == null || p.NextAttemptAt <= nowUtc))
            .OrderBy(p => p.CreatedAt)
            .Take(ReadyBatchSize), ct);

    public Task<IReadOnlyList<DocumentProcess>> ListWaitingAsync(string? signal, CancellationToken ct)
        => QueryAsync(q => q.Where(p => p.Status == ProcessStatus.Waiting && (signal == null || p.WaitingFor == signal)), ct);

    public Task<IReadOnlyList<DocumentProcess>> ListWaitingExpiredAsync(DateTime nowUtc, CancellationToken ct)
        => QueryAsync(q => q.Where(p => p.Status == ProcessStatus.Waiting && p.TimeoutAt != null && p.TimeoutAt <= nowUtc), ct);

    public Task<IReadOnlyList<DocumentProcess>> ListUnnotifiedAttentionAsync(CancellationToken ct)
        => QueryAsync(q => q.Where(p => p.Status == ProcessStatus.NeedsAttention && p.EscalationSentAt == null), ct);

    private async Task<IReadOnlyList<DocumentProcess>> QueryAsync(
        Func<IQueryable<DocumentProcess>, IQueryable<DocumentProcess>> build, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await build(db.Processes.AsNoTracking()).ToListAsync(ct);
    }
}
