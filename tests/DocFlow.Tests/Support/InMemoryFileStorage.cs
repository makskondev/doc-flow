using System.Text;

namespace DocFlow.Tests;

/// <summary>Файловое хранилище в памяти для тестов шагов и маршрутов. Разделители путей / и \\ равнозначны.</summary>
public sealed class InMemoryFileStorage : IFileStorage
{
    private sealed record Item(byte[] Content, DateTime Stamp);

    private readonly object _gate = new();
    private readonly Dictionary<string, Item> _files = new();
    private long _tick;

    private static string Norm(string path) => path.Replace('\\', '/');
    private static string DirectoryOf(string path) => path[..Math.Max(path.LastIndexOf('/'), 0)];

    public void Put(string path, string text) => Put(path, Encoding.UTF8.GetBytes(text));

    public void Put(string path, byte[] content)
    {
        lock (_gate) _files[Norm(path)] = new Item(content, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(++_tick));
    }

    public bool Has(string path) { lock (_gate) return _files.ContainsKey(Norm(path)); }
    public byte[] Bytes(string path) { lock (_gate) return _files[Norm(path)].Content; }
    public string Text(string path) => Encoding.UTF8.GetString(Bytes(path));
    public IReadOnlyList<string> All() { lock (_gate) return _files.Keys.OrderBy(k => k).ToList(); }

    public Task<bool> ExistsAsync(string path, CancellationToken ct) => Task.FromResult(Has(path));

    public Task<IReadOnlyList<FileEntry>> ListAsync(string directory, string pattern, CancellationToken ct)
    {
        var dir = Norm(directory).TrimEnd('/');
        lock (_gate)
        {
            IReadOnlyList<FileEntry> list = _files
                .Where(f => DirectoryOf(f.Key) == dir && FileMask.IsMatch(f.Key[(f.Key.LastIndexOf('/') + 1)..], pattern))
                .Select(f => new FileEntry(f.Key, f.Key[(f.Key.LastIndexOf('/') + 1)..], f.Value.Content.Length, f.Value.Stamp))
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task<byte[]> ReadAsync(string path, CancellationToken ct)
    {
        lock (_gate)
            return _files.TryGetValue(Norm(path), out var item)
                ? Task.FromResult(item.Content)
                : throw new FileNotFoundException(path);
    }

    public Task WriteAtomicAsync(string path, byte[] content, CancellationToken ct)
    {
        Put(path, content);
        return Task.CompletedTask;
    }

    public Task CopyAsync(string source, string destination, bool overwrite, CancellationToken ct)
    {
        if (!overwrite && Has(destination)) throw new IOException("файл уже существует: " + destination);
        Put(destination, Bytes(source));
        return Task.CompletedTask;
    }

    public Task MoveAsync(string source, string destination, bool overwrite, CancellationToken ct)
    {
        if (!overwrite && Has(destination)) throw new IOException("файл уже существует: " + destination);
        var content = Bytes(source);
        lock (_gate) _files.Remove(Norm(source));
        Put(destination, content);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string path, CancellationToken ct)
    {
        lock (_gate) _files.Remove(Norm(path));
        return Task.CompletedTask;
    }
}
