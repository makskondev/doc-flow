namespace DocFlow.Documents;

/// <summary>Запись вьюхи о документе, ожидающем обработки. Тип документа здесь НЕ указывается: его определяют правила Matches маршрутов.</summary>
/// <param name="Id">Идентификатор документа во вьюхе (уникален в пределах этой вьюхи).</param>
/// <param name="FilePath">Путь к файлу на шаре, если вьюха его отдаёт; иначе файл ищет шаг маршрута (FindFileStep).</param>
/// <param name="Data">
/// Колонки вьюхи, нужные для распознавания типа и обработки (признак вида документа, ФИО, даты, номер соглашения...).
/// Ключи — имена из <see cref="DataKeys"/>; значения — строки (даты — ISO 8601, числа — InvariantCulture).
/// </param>
public sealed record PendingDocument(string Id, string? FilePath = null, IReadOnlyDictionary<string, string>? Data = null);
