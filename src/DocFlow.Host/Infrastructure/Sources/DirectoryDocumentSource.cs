namespace DocFlow.Host;

/// <summary>
/// Документ появился в каталоге. Тип документа здесь не определяется: источник сообщает имя каталога и имя файла,
/// а маршрут сам решает (IRoute.Matches), его это документ или нет. Файл считается готовым, когда размер и время записи
/// не менялись между двумя опросами. Исходный файл не перемещаем при регистрации: его заберёт IntakeStep после
/// надёжного копирования (идемпотентно, без окна потери).
/// </summary>
public sealed class DirectoryDocumentSource(IFileStorage fs, IOptions<IntakeOptions> options) : IDocumentSource
{
    private Dictionary<string, (long Size, DateTime Stamp)> _seen = new();

    public string SourceId => SourceIds.Directory;

    public async Task<IReadOnlyList<DocumentCandidate>> PollAsync(CancellationToken ct)
    {
        var result = new List<DocumentCandidate>();
        var current = new Dictionary<string, (long Size, DateTime Stamp)>();

        foreach (var dir in options.Value.Directories)
        {
            var files = await fs.ListAsync(dir.Path, dir.Mask, ct);
            foreach (var f in files)
            {
                var snapshot = (f.Size, f.LastWriteUtc);
                current[f.FullPath] = snapshot;
                if (f.Size > 0 && _seen.TryGetValue(f.FullPath, out var prev) && prev == snapshot)
                    result.Add(new DocumentCandidate(
                        SourceId,
                        ExternalId: $"{dir.Name}/{System.IO.Path.GetFileNameWithoutExtension(f.Name)}",
                        ContentPath: f.FullPath,
                        Data: new Dictionary<string, string>
                        {
                            [DataKeys.SourceDirectory] = dir.Name,
                            [DataKeys.FileName] = f.Name
                        }));
            }
        }

        _seen = current;
        return result;
    }

    public async Task AcknowledgeAsync(DocumentProcess process, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(process.ContentPath) || !await fs.ExistsAsync(process.ContentPath, ct)) return;   // уже забрали

        // Оригинал уходит в _processed под ИСХОДНЫМ именем; если такое имя там уже занято — к нему добавляется идентификатор процесса
        var archive = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(process.ContentPath)!, "_processed");
        var target = System.IO.Path.Combine(archive, System.IO.Path.GetFileName(process.ContentPath));
        if (await fs.ExistsAsync(target, ct))
            target = System.IO.Path.Combine(archive,
                $"{System.IO.Path.GetFileNameWithoutExtension(process.ContentPath)}.{process.Id:N}{System.IO.Path.GetExtension(process.ContentPath)}");

        await fs.MoveAsync(process.ContentPath, target, overwrite: true, ct);
    }
}
