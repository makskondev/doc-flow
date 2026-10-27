namespace DocFlow.Documents;

/// <summary>Сведения о файле, которые сохраняются во внешней БД при регистрации документа (сценарий «файл появился в каталоге»).</summary>
public sealed record DocumentInfo(string FileName, long Size, string Sha256, IReadOnlyDictionary<string, string> Data);
