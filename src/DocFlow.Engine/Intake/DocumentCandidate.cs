namespace DocFlow.Engine;

/// <summary>
/// Документ, обнаруженный источником. Тип документа здесь НЕ указан: его определяет маршрут правилом
/// <see cref="IRoute.Matches"/> по этим данным (источник, колонки вьюхи, имя файла, каталог...).
/// </summary>
/// <param name="SourceId">Уникальный идентификатор источника (вьюхи или каталога).</param>
/// <param name="ExternalId">Идентификатор документа в источнике (уникален в пределах источника).</param>
/// <param name="ContentPath">Путь к файлу, если источник его знает; иначе пусто (файл найдёт шаг маршрута).</param>
/// <param name="Data">
/// Атрибуты документа: колонки вьюхи, имя файла, каталог и т.д. Значения — строки; даты — ISO 8601, числа — инвариантная культура.
/// Сохраняются в процесс как входные данные (<see cref="DocumentProcess.Data"/>).
/// </param>
public sealed record DocumentCandidate(
    string SourceId, string ExternalId, string? ContentPath = null,
    IReadOnlyDictionary<string, string>? Data = null)
{
    /// <summary>Значение атрибута или null, если его нет.</summary>
    public string? Get(string key) => Data is not null && Data.TryGetValue(key, out var value) ? value : null;
}
