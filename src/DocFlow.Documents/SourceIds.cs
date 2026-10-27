namespace DocFlow.Documents;

/// <summary>
/// Идентификаторы источников документов. Входят в ключ дедупликации (SourceId, ExternalId, Run), поэтому у каждой вьюхи
/// свой идентификатор и он уникален. Правила Matches маршрутов опираются на них.
/// </summary>
public static class SourceIds
{
    public const string View1 = "view_1";
    public const string View2 = "view_2";
    /// <summary>Все каталоги приёма; конкретный каталог различается атрибутом <see cref="DataKeys.SourceDirectory"/>.</summary>
    public const string Directory = "dir";
}
