namespace DocFlow.Host;

public sealed class IntakeDirectory
{
    /// <summary>
    /// Логическое имя каталога. Попадает в атрибут документа SourceDirectory — по нему правила Matches маршрутов
    /// различают каталоги — и входит в идентификатор документа (файлы с одинаковыми именами в разных каталогах не конфликтуют).
    /// </summary>
    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    /// <summary>Какие файлы забирать из каталога (маска * и ?). По умолчанию все PDF.</summary>
    public string Mask { get; set; } = "*.pdf";
}

public sealed class IntakeOptions
{
    public List<IntakeDirectory> Directories { get; set; } = new();
}
