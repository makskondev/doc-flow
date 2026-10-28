namespace DocFlow.Documents;

/// <summary>
/// Переложить файл в «папку для проставления подписи» (перенос, не копия) ПОД ИСХОДНЫМ ИМЕНЕМ. Запоминает, где после
/// подписания появятся подписанный файл и подпись: «директория результата подписания», то же имя файла.
/// Если в папке уже лежит другой файл с таким именем (другой документ ждёт подписи) — перезаписывать нельзя: эскалация оператору.
/// </summary>
public sealed class SendToSignatureStep(IFileStorage fs, IOptions<StorageOptions> storage) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var s = storage.Value;
        var type = ctx.Process.RouteKey;
        var name = ctx.RequireBag(BagKeys.FileName);

        await fs.MoveIdempotentAsync(s.Work(ctx.Id, ".pdf"), s.SignInput(type, name), overwriteDifferent: false, ct);

        ctx.Bag[BagKeys.PdfPath] = s.SignResultFile(type, name);
        ctx.Bag[BagKeys.SigPath] = s.SignResultSignature(type, name);
    }
}
