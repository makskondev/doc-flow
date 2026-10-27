namespace DocFlow.Documents;

/// <summary>
/// «Подпись в БД»: сохранить подпись во внешней БД и поставить отметку «подписан» — одной операцией.
/// После этого запись исчезает из вьюхи. Идемпотентно: если документ уже завершён — ничего не делаем.
/// </summary>
public sealed class PersistSignatureStep(IFileStorage fs, IExternalDocumentDb db) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var documentId = ctx.ExternalDocumentId();
        if (await db.IsCompletedAsync(documentId, ct)) return;

        var signature = await fs.ReadAsync(ctx.RequireBag(BagKeys.SigPath), ct);
        await db.CompleteAsync(documentId, signature, ct);
    }
}
