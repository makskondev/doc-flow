namespace DocFlow.Tests;

/// <summary>Журнал выполнения и управляемые сбои для тестовых шагов.</summary>
public sealed class Probe
{
    public List<string> Executed { get; } = new();
    public Dictionary<string, int> TransientFailures { get; } = new();
    public HashSet<string> PermanentFailures { get; } = new();
    public List<Guid> Escalations { get; } = new();
}
