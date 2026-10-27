namespace DocFlow.Documents;

/// <summary>Работа с файлами (в том числе на SMB-шаре). Всё через интерфейс, чтобы шаги тестировались без диска.</summary>
public interface IFileStorage
{
    Task<bool> ExistsAsync(string path, CancellationToken ct);
    Task<IReadOnlyList<FileEntry>> ListAsync(string directory, string pattern, CancellationToken ct);
    Task<byte[]> ReadAsync(string path, CancellationToken ct);
    /// <summary>Пишет во временный файл и переименовывает: читатель никогда не увидит недописанный файл.</summary>
    Task WriteAtomicAsync(string path, byte[] content, CancellationToken ct);
    Task CopyAsync(string source, string destination, bool overwrite, CancellationToken ct);
    Task MoveAsync(string source, string destination, bool overwrite, CancellationToken ct);
    Task DeleteAsync(string path, CancellationToken ct);
}
