namespace DocFlow.Documents;

public sealed class StorageOptions
{
    /// <summary>Наша рабочая копия документа: {id}.pdf, {id}.orig.pdf, {id}.sig.</summary>
    public string WorkingDir { get; set; } = "./data/work";

    /// <summary>Каталоги по типам документов. Ключ — тип документа (ключ маршрута).</summary>
    public Dictionary<string, TypeDirectories> Types { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string Work(Guid id, string suffix) => Path.Combine(WorkingDir, id + suffix);

    public string SignInput(string documentType, Guid id, string suffix)
        => Path.Combine(Require(documentType, d => d.SignInputDir, nameof(TypeDirectories.SignInputDir)), id + suffix);

    public string SignResultDir(string documentType)
        => Require(documentType, d => d.SignResultDir, nameof(TypeDirectories.SignResultDir));

    public string SignResult(string documentType, Guid id, string suffix)
        => Path.Combine(SignResultDir(documentType), id + suffix);

    /// <summary>Путь в конечном каталоге типа; имя файла — исходное, подпись — «имя.pdf.sig».</summary>
    public string Final(string documentType, string fileName)
        => Path.Combine(Require(documentType, d => d.FinalDir, nameof(TypeDirectories.FinalDir)), fileName);

    public string SearchDir(string documentType)
        => Require(documentType, d => d.SearchDir, nameof(TypeDirectories.SearchDir));

    // Каталог не настроен = ошибка конфигурации: повторять бессмысленно, сразу эскалация
    private string Require(string documentType, Func<TypeDirectories, string?> pick, string name)
        => Types.TryGetValue(documentType, out var dirs) && pick(dirs) is { Length: > 0 } path
            ? path
            : throw new PermanentStepException($"Для типа документа '{documentType}' не настроен каталог {name} (Storage:Types:{documentType}:{name})");
}
