namespace DocFlow.Documents;

/// <summary>
/// Вьюха view_1. ПРИМЕР описания вьюхи — по такому образцу заводится каждая вьюха. Отдаёт документы разных типов
/// (сценарии 1 и 4): тип различается колонкой DocKind, по ней работают правила Matches в ZopRoute и ArchiveRoute.
/// </summary>
public sealed class View1Reader : IPendingDocumentsReader
{
    public string SourceId => SourceIds.View1;

    // TODO (разработчик): SELECT из вьюхи. Для каждой строки: id, путь к файлу (если есть) и колонки в Data, например:
    //   new PendingDocument(row.Id, row.FilePath, new Dictionary<string, string>
    //   {
    //       [DataKeys.DocKind] = row.DocKind,
    //       [DataKeys.FullName] = row.FullName,
    //       [DataKeys.AgreementNumber] = row.AgreementNumber,
    //       [DataKeys.AgreementDate] = row.AgreementDate.ToString("yyyy-MM-dd"),   // ISO 8601
    //   });
    // Подключение к сторонней БД получите через конструктор (зарегистрируйте нужную зависимость в хосте).
    public Task<IReadOnlyList<PendingDocument>> ReadAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<PendingDocument>>(new List<PendingDocument>());
}
