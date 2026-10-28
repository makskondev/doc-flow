namespace DocFlow.Documents;

/// <summary>
/// Ждёт, когда в «директории результата подписания» типа документа появятся И подписанный файл, И подпись. Имена берутся
/// из процесса (их запомнил SendToSignatureStep): файл называется как исходный, подпись — по соглашению Storage:SignatureNaming.
/// Оба файла должны быть готовы: размер и время записи не менялись между двумя сканами. Каталог у каждого типа свой.
/// </summary>
public sealed class SigningResultScanner(
    IFileStorage fs,
    IOptions<StorageOptions> storage,
    IProcessRepository repo,
    ProcessEngine engine,
    ILogger<SigningResultScanner> log) : ISignalSource
{
    private Dictionary<string, (long Size, DateTime Stamp)> _seen = new();

    public string Signal => Signals.SigningResult;

    public async Task ScanAsync(CancellationToken ct)
    {
        var waiting = await repo.ListWaitingAsync(Signal, ct);
        if (waiting.Count == 0)
        {
            _seen.Clear();
            return;
        }

        var current = new Dictionary<string, (long Size, DateTime Stamp)>();
        foreach (var group in waiting.GroupBy(p => p.RouteKey))
        {
            string directory;
            try
            {
                directory = storage.Value.SignResultDir(group.Key);
            }
            catch (PermanentStepException ex)
            {
                log.LogError("{Message}", ex.Message);
                continue;
            }

            // Один листинг на каталог; имена сравниваются без учёта регистра (SMB)
            var files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in await fs.ListAsync(directory, "*", ct))
                files[file.Name] = file;

            foreach (var process in group)
            {
                if (!process.Bag.TryGetValue(BagKeys.PdfPath, out var pdfPath)
                    || !process.Bag.TryGetValue(BagKeys.SigPath, out var sigPath)) continue;
                if (!files.TryGetValue(Path.GetFileName(pdfPath), out var pdf)
                    || !files.TryGetValue(Path.GetFileName(sigPath), out var sig)) continue;   // нужны оба файла

                var pdfSnapshot = (pdf.Size, pdf.LastWriteUtc);
                var sigSnapshot = (sig.Size, sig.LastWriteUtc);
                current[pdf.FullPath] = pdfSnapshot;
                current[sig.FullPath] = sigSnapshot;

                var stable = pdf.Size > 0 && sig.Size > 0
                    && _seen.TryGetValue(pdf.FullPath, out var prevPdf) && prevPdf == pdfSnapshot
                    && _seen.TryGetValue(sig.FullPath, out var prevSig) && prevSig == sigSnapshot;

                if (stable && await engine.SignalAsync(process.Id, Signal, ct))
                    log.LogInformation("Получен результат подписания для процесса {Id}", process.Id);
            }
        }
        _seen = current;
    }
}
