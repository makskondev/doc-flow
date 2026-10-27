namespace DocFlow.Tests;

public sealed class RecordingNotifier(Probe probe) : IEscalationNotifier
{
    public Task NotifyAsync(DocumentProcess process, CancellationToken ct)
    {
        probe.Escalations.Add(process.Id);
        return Task.CompletedTask;
    }
}

public sealed class FakeSignalSource(string signal) : ISignalSource
{
    public string Signal { get; } = signal;
    public Task ScanAsync(CancellationToken ct) => Task.CompletedTask;
}
