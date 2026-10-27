namespace DocFlow.Engine;

public interface IProcessAudit
{
    /// <summary>Запись события. Лёгкая и НЕ должна бросать исключения: сбой аудита не ломает обработку документа.</summary>
    void Record(Guid processId, string nodeId, string kind, string? message = null);
    Task<IReadOnlyList<ProcessEvent>> ForAsync(Guid processId, CancellationToken ct);
}
