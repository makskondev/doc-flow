namespace DocFlow.Engine;

public sealed record ProcessEvent(Guid ProcessId, DateTime At, string NodeId, string Kind, string? Message);
