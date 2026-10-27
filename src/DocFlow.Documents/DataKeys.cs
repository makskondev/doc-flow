namespace DocFlow.Documents;

/// <summary>
/// Имена атрибутов документа (колонки вьюхи и атрибуты каталога → входные данные процесса → StepContext.Data).
/// ПРИМЕР: замените на реальные колонки ваших вьюх.
/// </summary>
public static class DataKeys
{
    // Для правил распознавания типа (Matches)
    public const string DocKind = "DocKind";                 // колонка вьюхи с признаком вида документа (пример)
    public const string SourceDirectory = "SourceDirectory"; // имя каталога приёма (проставляет источник «каталоги»)
    public const string FileName = "FileName";               // имя файла (каталоги; во вьюхе — если есть такая колонка)

    // Для штампа
    public const string FullName = "FullName";
    public const string AgreementNumber = "AgreementNumber";
    public const string AgreementDate = "AgreementDate";     // ISO 8601: yyyy-MM-dd
}
