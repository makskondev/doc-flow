namespace DocFlow.Documents;

/// <summary>
/// Сценарий 3. Файл появился в каталоге → информация о нём в БД → подпись в REST-сервисе → проверка →
/// подпись и отметка в БД → файл в конечный каталог.
/// </summary>
public sealed class InboxRoute : IRoute
{
    public string Key => DocumentTypes.Inbox;
    public int Version => 1;

    // Правило распознавания (пример): каталог приёма "inbox" и маска имени файла.
    // Тип по каталогу — достаточно сравнить SourceDirectory; по маске — FileMask.IsMatch(...).
    public bool Matches(DocumentCandidate c)
        => c.SourceId == SourceIds.Directory
           && c.Get(DataKeys.SourceDirectory) == "inbox"
           && FileMask.IsMatch(c.Get(DataKeys.FileName) ?? "", "*.pdf");

    public void Build(IRouteBuilder r) => r
        .Step<IntakeStep>()
        .Step<RegisterInExternalDbStep>()
        .Step<AutoSignStep>()
        .Include(RouteFragments.Finish);
}
