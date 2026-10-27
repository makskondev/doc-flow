namespace DocFlow.Documents;

/// <summary>Обязательная проверка подписи. Невалидная подпись или подменённый PDF — повторять бессмысленно (эскалация).</summary>
public sealed class VerifySignatureStep(IFileStorage fs, ISignatureVerifier verifier) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var pdf = await fs.ReadAsync(ctx.RequireBag(BagKeys.PdfPath), ct);
        var signature = await fs.ReadAsync(ctx.RequireBag(BagKeys.SigPath), ct);   // нет файла = обычная ошибка (возможен лаг SMB): повторится

        // Файл не должен измениться с момента отправки на подпись
        if (ctx.Bag.TryGetValue(BagKeys.Sha256, out var expected) && Hashing.Sha256Hex(pdf) != expected)
            throw new PermanentStepException("PDF отличается от отправленного на подпись (хеш не совпал)");

        var result = await verifier.VerifyAsync(pdf, signature, ct);
        if (!result.IsValid)
            throw new PermanentStepException($"Подпись не прошла проверку: {result.Reason}");
    }
}
