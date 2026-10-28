namespace DocFlow.Documents;

public sealed class StorageOptions
{
    /// <summary>
    /// Наша рабочая копия документа: {id}.pdf, {id}.orig.pdf, {id}.sig. Каталог внутренний, пользователи его не видят,
    /// поэтому имена здесь по идентификатору процесса (одинаковые имена файлов разных документов не конфликтуют).
    /// Во всех каталогах, которые видят пользователи (подпись, результат, конечный), файлы называются ИСХОДНЫМИ именами.
    /// </summary>
    public string WorkingDir { get; set; } = "./data/work";

    /// <summary>Как называется подпись относительно файла документа (по соглашению системы подписания).</summary>
    public SignatureNaming SignatureNaming { get; set; } = SignatureNaming.AppendSuffix;

    /// <summary>Каталоги по типам документов. Ключ — тип документа (ключ маршрута).</summary>
    public Dictionary<string, TypeDirectories> Types { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string Work(Guid id, string suffix) => Path.Combine(WorkingDir, id + suffix);

    /// <summary>Имя файла подписи для файла документа с данным именем.</summary>
    public string SignatureFileName(string fileName)
        => SignatureNaming == SignatureNaming.ReplaceExtension
            ? Path.ChangeExtension(fileName, ".sig")
            : fileName + ".sig";

    /// <summary>«Папка для проставления подписи»: сюда файл кладётся под исходным именем.</summary>
    public string SignInput(string documentType, string fileName)
        => Path.Combine(Require(documentType, d => d.SignInputDir, nameof(TypeDirectories.SignInputDir)), fileName);

    public string SignResultDir(string documentType)
        => Require(documentType, d => d.SignResultDir, nameof(TypeDirectories.SignResultDir));

    /// <summary>Где в «директории результата подписания» появится подписанный файл (то же имя, что при отправке).</summary>
    public string SignResultFile(string documentType, string fileName)
        => Path.Combine(SignResultDir(documentType), fileName);

    /// <summary>Где в «директории результата подписания» появится подпись.</summary>
    public string SignResultSignature(string documentType, string fileName)
        => Path.Combine(SignResultDir(documentType), SignatureFileName(fileName));

    /// <summary>Путь файла в конечном каталоге типа (исходное имя).</summary>
    public string Final(string documentType, string fileName)
        => Path.Combine(Require(documentType, d => d.FinalDir, nameof(TypeDirectories.FinalDir)), fileName);

    /// <summary>Путь подписи в конечном каталоге типа.</summary>
    public string FinalSignature(string documentType, string fileName)
        => Final(documentType, SignatureFileName(fileName));

    public string SearchDir(string documentType)
        => Require(documentType, d => d.SearchDir, nameof(TypeDirectories.SearchDir));

    // Каталог не настроен = ошибка конфигурации: повторять бессмысленно, сразу эскалация
    private string Require(string documentType, Func<TypeDirectories, string?> pick, string name)
        => Types.TryGetValue(documentType, out var dirs) && pick(dirs) is { Length: > 0 } path
            ? path
            : throw new PermanentStepException($"Для типа документа '{documentType}' не настроен каталог {name} (Storage:Types:{documentType}:{name})");
}
