namespace DocFlow.Documents;

/// <summary>
/// Ждёт, когда в «директории результата подписания» типа документа появятся И подписанный файл ({processId}.pdf),
/// И подпись ({processId}.sig). Оба файла должны быть готовы: размер и время записи не менялись между двумя сканами.
/// Каталог у каждого типа свой (Storage:Types:{тип}:SignResultDir).
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

            var pdfs = await IndexAsync(directory, "*.pdf", ct);
            var sigs = await IndexAsync(directory, "*.sig", ct);

            foreach (var process in group)
            {
                if (!pdfs.TryGetValue(process.Id, out var pdf) || !sigs.TryGetValue(process.Id, out var sig)) continue;   // нужны оба файла

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

    // Файлы результата называются по Guid процесса; дубли имён (регистр) скан не роняют
    private async Task<Dictionary<Guid, FileEntry>> IndexAsync(string directory, string pattern, CancellationToken ct)
    {
        var map = new Dictionary<Guid, FileEntry>();
        foreach (var file in await fs.ListAsync(directory, pattern, ct))
            if (Guid.TryParse(Path.GetFileNameWithoutExtension(file.Name), out var id))
                map[id] = file;
        return map;
    }
}
