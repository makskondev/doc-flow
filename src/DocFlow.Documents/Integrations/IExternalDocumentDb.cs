namespace DocFlow.Documents;

/// <summary>
/// Сторонняя БД с документами (не внутренняя PostgreSQL сервиса). Для документов из вьюх запись о документе уже есть;
/// после <see cref="CompleteAsync"/> она исчезает из вьюхи.
/// </summary>
public interface IExternalDocumentDb
{
    /// <summary>
    /// Документ появился в каталоге: сохранить информацию о нём и вернуть идентификатор записи во внешней БД.
    /// ДОЛЖНО быть идемпотентным по processId: повтор не создаёт вторую запись.
    /// </summary>
    Task<string> RegisterDocumentAsync(Guid processId, DocumentInfo info, CancellationToken ct);

    /// <summary>Подпись/отметка уже проставлены?</summary>
    Task<bool> IsCompletedAsync(string documentId, CancellationToken ct);

    /// <summary>
    /// Сохранить подпись (если она есть) и поставить отметку «обработан/подписан» — одной операцией (одна транзакция).
    /// Для маршрутов без подписания подпись = null: ставится только отметка.
    /// </summary>
    Task CompleteAsync(string documentId, byte[]? signature, CancellationToken ct);
}
