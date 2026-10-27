namespace DocFlow.Documents;

/// <summary>Сценарий 2. Запись во view_2 → подпись в REST-сервисе → проверка → подпись и отметка в БД → файл в конечный каталог.</summary>
public sealed class ReportRoute : IRoute
{
    public string Key => DocumentTypes.Report;
    public int Version => 1;

    // Правило распознавания (пример): всё, что пришло из view_2
    public bool Matches(DocumentCandidate c) => c.SourceId == SourceIds.View2;

    public void Build(IRouteBuilder r) => r
        .Step<IntakeStep>()
        .Step<AutoSignStep>()
        .Include(RouteFragments.Finish);
}
