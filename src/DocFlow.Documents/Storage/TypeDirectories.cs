namespace DocFlow.Documents;

/// <summary>Каталоги одного типа документа (секция Storage:Types:{тип}). Настраиваются только те, что использует маршрут.</summary>
public sealed class TypeDirectories
{
    /// <summary>«Папка для проставления подписи»: сюда шаг SendToSignatureStep перекладывает файл.</summary>
    public string? SignInputDir { get; set; }

    /// <summary>«Директория результата подписания»: здесь появляются подписанный файл ({id}.pdf) и подпись ({id}.sig).</summary>
    public string? SignResultDir { get; set; }

    /// <summary>Конечный каталог, куда в конце перекладывается файл.</summary>
    public string? FinalDir { get; set; }

    /// <summary>Каталог, где шаг FindFileStep ищет файл, если источник не передал путь.</summary>
    public string? SearchDir { get; set; }
}
