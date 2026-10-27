namespace DocFlow.Tests;

public class RegistrarTests
{
    [Fact]
    public async Task New_document_creates_run_1()
    {
        var h = new Harness();
        var id = await h.RegisterAsync("linear");
        Assert.NotNull(id);
        Assert.Equal(1, (await h.GetAsync(id!.Value)).Run);
    }

    [Fact]
    public async Task Active_process_blocks_duplicates()
    {
        var h = new Harness();
        Assert.NotNull(await h.RegisterAsync("wait"));
        Assert.Null(await h.RegisterAsync("wait"));
    }

    [Fact]
    public async Task Missing_required_data_prevents_registration()
    {
        var h = new Harness();
        Assert.Null(await h.RegisterAsync("data"));
        Assert.Null(await h.RegisterAsync("data", data: new() { ["fio"] = "  " }));
        Assert.NotNull(await h.RegisterAsync("data", data: new() { ["fio"] = "Иванов И.И." }));   // данные появились: процесс создан
    }

    [Fact]
    public async Task Input_data_is_a_snapshot_available_to_steps()
    {
        var h = new Harness();
        var source = new Dictionary<string, string> { ["fio"] = "Иванов И.И." };
        var id = (await h.RegisterAsync("data", data: source))!.Value;
        source["fio"] = "изменено после регистрации";

        var p = await h.GetAsync(id);
        Assert.Equal("Иванов И.И.", p.Data["fio"]);
        Assert.Equal("Иванов И.И.", new StepContext(p).RequireData("fio"));
        Assert.Throws<PermanentStepException>(() => new StepContext(p).RequireData("нет-такого"));
    }

    [Fact]
    public async Task Unknown_type_is_skipped()
    {
        var h = new Harness();
        Assert.Null(await h.RegisterAsync("nope"));
    }

    [Fact]
    public async Task Completed_document_reappearing_later_is_a_new_run()
    {
        var h = new Harness();
        var first = (await h.RegisterAsync("linear"))!.Value;
        h.Clock.Advance(TimeSpan.FromMinutes(1));
        await h.AdvanceAsync(first);

        h.Clock.Advance(TimeSpan.FromMinutes(1));
        var second = await h.RegisterAsync("linear", pollStartedAt: h.Clock.Now);   // снимок сделан после завершения

        Assert.NotNull(second);
        Assert.Equal(2, (await h.GetAsync(second!.Value)).Run);
    }

    [Fact]
    public async Task Stale_snapshot_taken_before_completion_is_ignored()
    {
        var h = new Harness();
        var pollStarted = h.Clock.Now;
        var first = (await h.RegisterAsync("linear"))!.Value;
        h.Clock.Advance(TimeSpan.FromMinutes(1));
        await h.AdvanceAsync(first);                                                // завершён уже после начала опроса

        Assert.Null(await h.RegisterAsync("linear", pollStartedAt: pollStarted));
    }

    [Fact]
    public async Task Cancelled_is_not_restarted_automatically_but_can_be_restarted_by_operator()
    {
        var h = new Harness();
        var first = (await h.RegisterAsync("wait"))!.Value;
        await h.Engine.CancelAsync(first, CancellationToken.None);
        h.Clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Null(await h.RegisterAsync("wait", pollStartedAt: h.Clock.Now));     // запись всё ещё во вьюхе, но автоперезапуска нет

        var restarted = await h.Registrar.RestartAsync(first, CancellationToken.None);
        Assert.NotNull(restarted);
        Assert.Equal(2, (await h.GetAsync(restarted!.Value)).Run);
    }
}
