namespace DocFlow.Engine;

/// <summary>
/// Хранилище процессов. Для продакшена реализуйте поверх вашей СУБД:
/// уникальный индекс (SourceId, ExternalId, Run), RowVersion для SaveAsync.
/// </summary>
public interface IProcessRepository
{
    Task<DocumentProcess?> GetAsync(Guid id, CancellationToken ct);
    Task<DocumentProcess?> FindLatestAsync(string sourceId, string externalId, CancellationToken ct);
    /// <summary>false — конфликт уникальности (процесс уже есть).</summary>
    Task<bool> TryAddAsync(DocumentProcess process, CancellationToken ct);
    /// <summary>Optimistic lock по Version; при успехе увеличивает process.Version.</summary>
    Task SaveAsync(DocumentProcess process, CancellationToken ct);

    Task<IReadOnlyList<DocumentProcess>> ListAsync(ProcessStatus? status, CancellationToken ct);
    /// <summary>Running и (NextAttemptAt пуст или наступил).</summary>
    Task<IReadOnlyList<DocumentProcess>> ListReadyAsync(DateTime nowUtc, CancellationToken ct);
    Task<IReadOnlyList<DocumentProcess>> ListWaitingAsync(string? signal, CancellationToken ct);
    Task<IReadOnlyList<DocumentProcess>> ListWaitingExpiredAsync(DateTime nowUtc, CancellationToken ct);
    /// <summary>NeedsAttention, по которым письмо ещё не отправлено.</summary>
    Task<IReadOnlyList<DocumentProcess>> ListUnnotifiedAttentionAsync(CancellationToken ct);
}
