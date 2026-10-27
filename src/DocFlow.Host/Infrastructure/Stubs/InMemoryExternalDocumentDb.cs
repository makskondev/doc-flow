using System.Collections.Concurrent;

namespace DocFlow.Host;

/// <summary>ЗАГЛУШКА внешней БД: хранит всё в памяти. Замените реализацией для вашей сторонней БД.</summary>
public sealed class InMemoryExternalDocumentDb : IExternalDocumentDb
{
    private readonly ConcurrentDictionary<Guid, string> _registered = new();           // processId -> id документа
    private readonly ConcurrentDictionary<string, byte[]?> _completed = new();          // id документа -> подпись (если была)

    public Task<string> RegisterDocumentAsync(Guid processId, DocumentInfo info, CancellationToken ct)
        => Task.FromResult(_registered.GetOrAdd(processId, id => "doc-" + id.ToString("N")));   // идемпотентно по processId

    public Task<bool> IsCompletedAsync(string documentId, CancellationToken ct)
        => Task.FromResult(_completed.ContainsKey(documentId));

    public Task CompleteAsync(string documentId, byte[]? signature, CancellationToken ct)
    {
        _completed[documentId] = signature;
        return Task.CompletedTask;
    }
}
