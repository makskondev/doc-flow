namespace DocFlow.Documents;

/// <summary>
/// Переложить файл в «папку для проставления подписи» (перенос, не копия). Запоминает, где после подписания появятся
/// подписанный файл и подпись: «директория результата подписания», имена {id}.pdf и {id}.sig.
/// </summary>
public sealed class SendToSignatureStep(IFileStorage fs, IOptions<StorageOptions> storage) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var s = storage.Value;
        var type = ctx.Process.RouteKey;

        await fs.MoveIdempotentAsync(s.Work(ctx.Id, ".pdf"), s.SignInput(type, ctx.Id, ".pdf"), overwriteDifferent: true, ct);

        ctx.Bag[BagKeys.PdfPath] = s.SignResult(type, ctx.Id, ".pdf");
        ctx.Bag[BagKeys.SigPath] = s.SignResult(type, ctx.Id, ".sig");
    }
}
