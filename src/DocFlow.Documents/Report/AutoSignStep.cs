namespace DocFlow.Documents;

/// <summary>
/// Автоподпись через REST. Подпись сохраняем у себя до завершения шага: повтор после сбоя не вызывает сервис подписи снова.
/// (Остаётся окно «подписали, но не успели сохранить» — тогда подпись будет создана повторно; проверка подписи это переживёт.)
/// </summary>
public sealed class AutoSignStep(IFileStorage fs, IOptions<StorageOptions> storage, ISigningService signing) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var s = storage.Value;
        var pdf = s.Work(ctx.Id, ".pdf");
        var sig = s.Work(ctx.Id, ".sig");

        if (!await fs.ExistsAsync(sig, ct))
        {
            var signature = await signing.SignAsync(await fs.ReadAsync(pdf, ct), ct);
            await fs.WriteAtomicAsync(sig, signature, ct);
        }

        ctx.Bag[BagKeys.PdfPath] = pdf;
        ctx.Bag[BagKeys.SigPath] = sig;
    }
}
