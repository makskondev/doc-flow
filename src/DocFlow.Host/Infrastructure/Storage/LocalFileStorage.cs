namespace DocFlow.Host;

public sealed class LocalFileStorage : IFileStorage
{
    public Task<bool> ExistsAsync(string path, CancellationToken ct) => Task.FromResult(File.Exists(path));

    public Task<IReadOnlyList<FileEntry>> ListAsync(string directory, string pattern, CancellationToken ct)
    {
        IReadOnlyList<FileEntry> list = Directory.Exists(directory)
            ? new DirectoryInfo(directory).EnumerateFiles(pattern)
                .Select(f => new FileEntry(f.FullName, f.Name, f.Length, f.LastWriteTimeUtc)).ToList()
            : new List<FileEntry>();
        return Task.FromResult(list);
    }

    public Task<byte[]> ReadAsync(string path, CancellationToken ct) => File.ReadAllBytesAsync(path, ct);

    public async Task WriteAtomicAsync(string path, byte[] content, CancellationToken ct)
    {
        EnsureDirectory(path);
        var tmp = path + ".tmp";
        await File.WriteAllBytesAsync(tmp, content, ct);
        File.Move(tmp, path, overwrite: true);
    }

    public Task CopyAsync(string source, string destination, bool overwrite, CancellationToken ct)
    {
        EnsureDirectory(destination);
        var tmp = destination + ".tmp";
        File.Copy(source, tmp, overwrite: true);
        File.Move(tmp, destination, overwrite);
        return Task.CompletedTask;
    }

    public Task MoveAsync(string source, string destination, bool overwrite, CancellationToken ct)
    {
        EnsureDirectory(destination);
        File.Move(source, destination, overwrite);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string path, CancellationToken ct)
    {
        File.Delete(path);   // не бросает, если файла нет
        return Task.CompletedTask;
    }

    private static void EnsureDirectory(string path)
        => Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
}
