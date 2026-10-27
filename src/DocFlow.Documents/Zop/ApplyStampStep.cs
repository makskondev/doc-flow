using System.Text;

namespace DocFlow.Documents;

/// <summary>
/// Наложить штамп. Штамп накладывает сам сервис: это обычный шаг, его код пишет разработчик (метод <see cref="Stamp"/>).
/// Контракт шага, который важно сохранить: штампуем всегда сохранённый оригинал (поэтому повтор после сбоя даёт тот же
/// результат), пишем атомарно и обновляем хеш — он нужен проверке подписи.
/// </summary>
public sealed class ApplyStampStep(IFileStorage fs, IOptions<StorageOptions> storage) : IStep
{
    public async Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var s = storage.Value;
        var pdf = s.Work(ctx.Id, ".pdf");
        var original = s.Work(ctx.Id, ".orig.pdf");

        if (!await fs.ExistsAsync(original, ct))
            await fs.CopyAsync(pdf, original, overwrite: false, ct);

        var stamped = Stamp(await fs.ReadAsync(original, ct), ctx);
        await fs.WriteAtomicAsync(pdf, stamped, ct);

        ctx.Bag[BagKeys.Sha256] = Hashing.Sha256Hex(stamped);
        ctx.Bag[BagKeys.PdfPath] = pdf;
    }

    /// <summary>
    /// TODO (разработчик): наложить штамп. Вход — исходный PDF, выход — PDF со штампом.
    /// Данные для штампа (ФИО, даты, номер соглашения...) — во входных данных документа: ctx.Data / ctx.RequireData(...);
    /// какие поля обязательны, объявлено в ZopRoute.RequiredData. Результат должен быть детерминированным.
    /// СЕЙЧАС — ЗАГЛУШКА: дописывает в конец файла комментарий-маркер, визуально PDF не меняется.
    /// </summary>
    private static byte[] Stamp(byte[] originalPdf, StepContext ctx)
    {
        var fullName = ctx.RequireData(DataKeys.FullName);
        var number = ctx.RequireData(DataKeys.AgreementNumber);
        var date = ctx.RequireData(DataKeys.AgreementDate);
        return originalPdf.Concat(Encoding.UTF8.GetBytes($"\n%stamp:{number}:{date}:{fullName}\n")).ToArray();
    }
}
