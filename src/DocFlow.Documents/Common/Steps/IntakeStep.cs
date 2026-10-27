namespace DocFlow.Documents;

/// <summary>
/// Принять документ: надёжно скопировать в рабочий каталог, проверить, что это PDF, запомнить хеш,
/// после чего источник может «забрать» исходник. Исходник чужой шары не изменяем и не удаляем здесь.
/// </summary>
public sealed class IntakeStep(
    IFileStorage fs, IOptions<StorageOptions> storage, IEnumerable<IDocumentSource> sources) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var p = ctx.Process;
        var target = storage.Value.Work(p.Id, ".pdf");

        // Идемпотентность: если копия уже сделана и учтена — повторно не копируем
        var alreadyDone = ctx.Bag.ContainsKey(BagKeys.Sha256) && await fs.ExistsAsync(target, ct);
        if (!alreadyDone)
        {
            if (string.IsNullOrEmpty(p.ContentPath))
                throw new PermanentStepException("Источник не передал путь к файлу: для такого маршрута путь нужно получить шагом поиска (FindFileStep)");
            if (!await fs.ExistsAsync(p.ContentPath, ct))
                throw new PermanentStepException($"Исходный файл не найден: {p.ContentPath}");

            var bytes = await fs.ReadAsync(p.ContentPath, ct);
            if (bytes.Length < 5 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
                throw new PermanentStepException("Файл пустой или не является PDF");

            await fs.WriteAtomicAsync(target, bytes, ct);
            ctx.Bag[BagKeys.Sha256] = Hashing.Sha256Hex(bytes);
            ctx.Bag[BagKeys.PdfPath] = target;
            ctx.Bag[BagKeys.FileName] = Path.GetFileName(p.ContentPath);
        }

        var source = sources.FirstOrDefault(s => s.SourceId == p.SourceId);
        if (source is not null) await source.AcknowledgeAsync(p, ct);
    }
}
