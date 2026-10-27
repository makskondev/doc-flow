namespace DocFlow.Tests;

public abstract class ProbeStep(Probe probe) : IStep
{
    public Task ExecuteAsync(StepContext ctx, CancellationToken ct)
    {
        var name = GetType().Name;
        if (probe.PermanentFailures.Contains(name)) throw new PermanentStepException("boom");
        if (probe.TransientFailures.TryGetValue(name, out var left) && left > 0)
        {
            probe.TransientFailures[name] = left - 1;
            throw new InvalidOperationException("transient");
        }
        probe.Executed.Add(name);
        return Task.CompletedTask;
    }
}

public sealed class StepA(Probe probe) : ProbeStep(probe);

public sealed class StepB(Probe probe) : ProbeStep(probe);

public sealed class StepNotRegistered(Probe probe) : ProbeStep(probe);
