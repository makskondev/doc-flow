namespace DocFlow.Documents;

/// <summary>
/// Сценарий «файл появился в каталоге»: сохранить информацию о файле во внешней БД. Идентификатор записи,
/// который выдала БД, запоминаем: он нужен шагам «подпись в БД» / «отметка». Повтор безопасен (БД идемпотентна по processId).
/// </summary>
public sealed class RegisterInExternalDbStep(IFileStorage fs, IOptions<StorageOptions> storage, IExternalDocumentDb db) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        if (ctx.Bag.ContainsKey(BagKeys.ExternalDocId)) return;

        var bytes = await fs.ReadAsync(storage.Value.Work(ctx.Id, ".pdf"), ct);
        var fileName = ctx.Bag.TryGetValue(BagKeys.FileName, out var n) ? n : ctx.ExternalId;
        var info = new DocumentInfo(fileName, bytes.Length, Hashing.Sha256Hex(bytes), ctx.Data);

        ctx.Bag[BagKeys.ExternalDocId] = await db.RegisterDocumentAsync(ctx.Id, info, ct);
    }
}
