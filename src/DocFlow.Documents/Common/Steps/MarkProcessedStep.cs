namespace DocFlow.Documents;

/// <summary>
/// Отметка во внешней БД БЕЗ подписи (маршруты без подписания, сценарий 4). После неё запись исчезает из вьюхи.
/// Идемпотентно.
/// </summary>
public sealed class MarkProcessedStep(IExternalDocumentDb db) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var documentId = ctx.ExternalDocumentId();
        if (await db.IsCompletedAsync(documentId, ct)) return;
        await db.CompleteAsync(documentId, signature: null, ct);
    }
}
