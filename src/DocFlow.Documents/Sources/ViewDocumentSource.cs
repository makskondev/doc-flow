namespace DocFlow.Documents;

/// <summary>
/// Адаптер «вьюха → источник документов». Вьюха сторонней БД — это очередь: пока запись есть, документ ждёт обработки;
/// исчезает она после того, как во внешней БД проставлена подпись или отметка. Подтверждать получение нечем (только чтение),
/// поэтому защита от дублей — уникальность в регистраторе. Тип документа здесь не определяется: это делают правила Matches.
/// </summary>
public sealed class ViewDocumentSource(IPendingDocumentsReader reader) : IDocumentSource
{
    public string SourceId => reader.SourceId;

    public async Task<IReadOnlyList<DocumentCandidate>> PollAsync(CancellationToken ct)
    {
        var rows = await reader.ReadAsync(ct);
        return rows.Select(r => new DocumentCandidate(SourceId, r.Id, r.FilePath, r.Data)).ToList();
    }

    public Task AcknowledgeAsync(DocumentProcess process, CancellationToken ct) => Task.CompletedTask;
}
