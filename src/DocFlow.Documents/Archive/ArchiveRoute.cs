namespace DocFlow.Documents;

/// <summary>Сценарий 4. Запись во view_1 о документе другого вида → найти файл → отметка в БД (без подписи) → файл в конечный каталог.</summary>
public sealed class ArchiveRoute : IRoute
{
    public string Key => DocumentTypes.Archive;
    public int Version => 1;

    // Правило распознавания (пример): та же вьюха view_1, но другой вид документа
    public bool Matches(DocumentCandidate c)
        => c.SourceId == SourceIds.View1 && c.Get(DataKeys.DocKind) == DocumentTypes.Archive;

    public void Build(IRouteBuilder r) => r
        .Step<FindFileStep>()
        .Step<MarkProcessedStep>()
        .Step<MoveToFinalStep>();
}
