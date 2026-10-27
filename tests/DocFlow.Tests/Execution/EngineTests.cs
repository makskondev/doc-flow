namespace DocFlow.Tests;

public class EngineTests
{
    [Fact]
    public async Task Linear_route_runs_to_completion()
    {
        var h = new Harness();
        var id = (await h.RegisterAsync("linear"))!.Value;

        await h.AdvanceAsync(id);

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.Completed, p.Status);
        Assert.NotNull(p.CompletedAt);
        Assert.Equal(new[] { "StepA", "StepB" }, h.Probe.Executed);
    }

    [Fact]
    public async Task Wait_route_parks_then_completes_after_signal()
    {
        var h = new Harness();
        var id = (await h.RegisterAsync("wait"))!.Value;

        await h.AdvanceAsync(id);
        var parked = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.Waiting, parked.Status);
        Assert.Equal("go", parked.WaitingFor);
        Assert.Equal(new[] { "StepA" }, h.Probe.Executed);

        Assert.False(await h.Engine.SignalAsync(id, "other", CancellationToken.None));   // чужой сигнал игнорируется
        Assert.True(await h.Engine.SignalAsync(id, "go", CancellationToken.None));
        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.Equal(new[] { "StepA", "StepB" }, h.Probe.Executed);
        Assert.False(await h.Engine.SignalAsync(id, "go", CancellationToken.None));      // дубль после завершения игнорируется
    }

    [Fact]
    public async Task Transient_failure_is_retried_with_backoff()
    {
        var h = new Harness();
        h.Probe.TransientFailures["StepB"] = 1;
        var id = (await h.RegisterAsync("linear"))!.Value;

        await h.AdvanceAsync(id);
        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.Running, p.Status);
        Assert.Equal(1, p.Attempts);
        Assert.True(p.NextAttemptAt > h.Clock.Now);

        await h.AdvanceAsync(id);                                  // слишком рано: ничего не происходит
        Assert.Equal(new[] { "StepA" }, h.Probe.Executed);

        h.Clock.Advance(TimeSpan.FromSeconds(11));
        await h.AdvanceAsync(id);
        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.Equal(new[] { "StepA", "StepB" }, h.Probe.Executed);
    }

    [Fact]
    public async Task Exhausted_retries_escalate()
    {
        var h = new Harness();
        h.Probe.TransientFailures["StepB"] = 99;
        var id = (await h.RegisterAsync("linear"))!.Value;

        for (var i = 0; i < 3; i++)
        {
            await h.AdvanceAsync(id);
            h.Clock.Advance(TimeSpan.FromHours(1));
        }

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.NeedsAttention, p.Status);
        Assert.Single(h.Probe.Escalations);
    }

    [Fact]
    public async Task Permanent_failure_escalates_once_and_retry_reruns_the_node()
    {
        var h = new Harness();
        h.Probe.PermanentFailures.Add("StepB");
        var id = (await h.RegisterAsync("linear"))!.Value;

        await h.AdvanceAsync(id);
        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.NeedsAttention, p.Status);
        Assert.Contains("boom", p.AttentionReason);
        Assert.NotNull(p.EscalationSentAt);
        Assert.Single(h.Probe.Escalations);

        h.Probe.PermanentFailures.Clear();
        Assert.True(await h.Engine.RetryAsync(id, CancellationToken.None));
        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.Equal(new[] { "StepA", "StepB" }, h.Probe.Executed);   // StepA второй раз не выполнялся
    }

    [Fact]
    public async Task Wait_timeout_escalates_and_retry_parks_again()
    {
        var h = new Harness();
        var id = (await h.RegisterAsync("wait"))!.Value;
        await h.AdvanceAsync(id);

        await h.Engine.TimeoutAsync(id, CancellationToken.None);          // ещё рано
        Assert.Equal(ProcessStatus.Waiting, (await h.GetAsync(id)).Status);

        h.Clock.Advance(TimeSpan.FromHours(2));
        await h.Engine.TimeoutAsync(id, CancellationToken.None);
        Assert.Equal(ProcessStatus.NeedsAttention, (await h.GetAsync(id)).Status);

        Assert.True(await h.Engine.RetryAsync(id, CancellationToken.None));
        await h.AdvanceAsync(id);

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.Waiting, p.Status);
        Assert.True(p.TimeoutAt > h.Clock.Now);                           // новый таймаут
    }

    [Fact]
    public async Task Continue_skips_the_failed_node()
    {
        var h = new Harness();
        h.Probe.PermanentFailures.Add("StepB");
        var id = (await h.RegisterAsync("linear"))!.Value;
        await h.AdvanceAsync(id);

        Assert.True(await h.Engine.ContinueAsync(id, CancellationToken.None));
        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Cancel_is_terminal()
    {
        var h = new Harness();
        var id = (await h.RegisterAsync("wait"))!.Value;
        await h.AdvanceAsync(id);

        Assert.True(await h.Engine.CancelAsync(id, CancellationToken.None));
        Assert.False(await h.Engine.SignalAsync(id, "go", CancellationToken.None));
        Assert.False(await h.Engine.CancelAsync(id, CancellationToken.None));
    }
}
