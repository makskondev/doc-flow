namespace DocFlow.Documents;

/// <summary>
/// Найти файл документа, если источник не передал путь (сценарий 4): ищем в каталоге поиска типа документа
/// (Storage:Types:{тип}:SearchDir). Файла пока нет — обычная ошибка: он может появиться позже, будут повторы с backoff.
/// Файл не копируем: дальше он переносится прямо в конечный каталог.
/// </summary>
public sealed class FindFileStep(IFileStorage fs, IOptions<StorageOptions> storage) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var path = !string.IsNullOrEmpty(ctx.Process.ContentPath)
            ? ctx.Process.ContentPath
            : Path.Combine(storage.Value.SearchDir(ctx.Process.RouteKey), FileNameOf(ctx));

        if (!await fs.ExistsAsync(path, ct))
            throw new FileNotFoundException($"Файл документа пока не найден: {path}");

        ctx.Bag[BagKeys.PdfPath] = path;
        ctx.Bag[BagKeys.FileName] = Path.GetFileName(path);
    }

    /// <summary>
    /// TODO (разработчик): правило имени файла. По умолчанию — колонка FileName из данных документа, иначе «{id}.pdf».
    /// Если нужен поиск по маске или по содержимому каталога — замените здесь.
    /// </summary>
    private static string FileNameOf(StepContext ctx)
        => ctx.Data.TryGetValue(DataKeys.FileName, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : ctx.ExternalId + ".pdf";
}
