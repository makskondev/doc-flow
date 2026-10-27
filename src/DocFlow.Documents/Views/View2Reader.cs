namespace DocFlow.Documents;

/// <summary>Вьюха view_2 (сценарий 2: автоподпись). ПРИМЕР — см. View1Reader.</summary>
public sealed class View2Reader : IPendingDocumentsReader
{
    public string SourceId => SourceIds.View2;

    // TODO (разработчик): SELECT из вьюхи view_2: id, путь к файлу и нужные колонки.
    public Task<IReadOnlyList<PendingDocument>> ReadAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<PendingDocument>>(new List<PendingDocument>());
}
