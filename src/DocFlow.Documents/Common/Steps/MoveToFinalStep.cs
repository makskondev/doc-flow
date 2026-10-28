namespace DocFlow.Documents;

/// <summary>
/// Переложить файл (и подпись, если она есть) в конечный каталог типа документа и убрать рабочие копии.
/// Файл сохраняет исходное имя, подпись называется по соглашению Storage:SignatureNaming (по умолчанию «имя.pdf.sig»).
/// Повтор безопасен; другой файл с тем же именем в конечном каталоге не перезаписывается — эскалация.
/// </summary>
public sealed class MoveToFinalStep(IFileStorage fs, IOptions<StorageOptions> storage) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var s = storage.Value;
        var type = ctx.Process.RouteKey;
        var name = ctx.Bag.TryGetValue(BagKeys.FileName, out var n) ? n : ctx.Id + ".pdf";

        var pdfTarget = s.Final(type, name);
        await fs.MoveIdempotentAsync(ctx.RequireBag(BagKeys.PdfPath), pdfTarget, overwriteDifferent: false, ct);
        ctx.Bag[BagKeys.PdfPath] = pdfTarget;

        if (ctx.Bag.TryGetValue(BagKeys.SigPath, out var sigPath))
        {
            var sigTarget = s.FinalSignature(type, name);
            await fs.MoveIdempotentAsync(sigPath, sigTarget, overwriteDifferent: false, ct);
            ctx.Bag[BagKeys.SigPath] = sigTarget;
        }

        foreach (var suffix in new[] { ".pdf", ".orig.pdf", ".sig" })
            await fs.DeleteAsync(s.Work(ctx.Id, suffix), ct);
    }
}
