namespace DocFlow.Data;

/// <summary>Строка аудита процесса (таблица process_events).</summary>
public sealed class ProcessEventEntity
{
    public long Id { get; set; }
    public Guid ProcessId { get; set; }
    public DateTime At { get; set; }
    public string NodeId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Message { get; set; }
}
