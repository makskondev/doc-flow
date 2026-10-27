namespace DocFlow.Tests;

public class RouteTests
{
    private sealed class FragmentRoute : IRoute
    {
        public string Key => "frag";
        public int Version => 1;
        public bool Matches(DocumentCandidate c) => false;
        public void Build(IRouteBuilder r) => r.Step<StepA>().Include(x => x.Step<StepB>().Step<StepA>());
    }

    private sealed class EndsWithWaitRoute : IRoute
    {
        public string Key => "bad-wait";
        public int Version => 1;
        public bool Matches(DocumentCandidate c) => false;
        public void Build(IRouteBuilder r) => r.Step<StepA>().WaitFor("go", TimeSpan.FromHours(1));
    }

    private sealed class UnregisteredStepRoute : IRoute
    {
        public string Key => "bad-step";
        public int Version => 1;
        public bool Matches(DocumentCandidate c) => false;
        public void Build(IRouteBuilder r) => r.Step<StepNotRegistered>();
    }

    [Fact]
    public void Include_inlines_fragment_and_repeated_steps_get_unique_ids()
    {
        var route = new RouteRegistry(new IRoute[] { new FragmentRoute() }).Get("frag");
        Assert.Equal(new[] { "StepA", "StepB", "StepA#2" }, route.Nodes.Select(n => n.Id));
        Assert.Equal("StepB", route.NextOf("StepA")!.Id);
        Assert.Null(route.NextOf("StepA#2"));
    }

    [Fact]
    public void Duplicate_route_keys_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new RouteRegistry(new IRoute[] { new LinearRoute(), new LinearRoute() }));
    }

    [Fact]
    public void Valid_routes_pass_validation()
    {
        var h = new Harness();
        var errors = RouteValidator.Validate(h.Routes, new ISignalSource[] { new FakeSignalSource("go") }, h.Services);
        Assert.Empty(errors);
    }

    [Fact]
    public void Wait_without_signal_source_is_reported()
    {
        var h = new Harness();
        var errors = RouteValidator.Validate(h.Routes, Array.Empty<ISignalSource>(), h.Services);
        Assert.Contains(errors, e => e.Contains("'go'"));
    }

    [Fact]
    public void Route_ending_with_wait_and_unregistered_step_are_reported()
    {
        var h = new Harness();
        var routes = new RouteRegistry(new IRoute[] { new EndsWithWaitRoute(), new UnregisteredStepRoute() });
        var errors = RouteValidator.Validate(routes, new ISignalSource[] { new FakeSignalSource("go") }, h.Services);

        Assert.Contains(errors, e => e.Contains("bad-wait") && e.Contains("последним"));
        Assert.Contains(errors, e => e.Contains("StepNotRegistered"));
    }

    private sealed class StubSource(string id) : IDocumentSource
    {
        public string SourceId => id;
        public Task<IReadOnlyList<DocumentCandidate>> PollAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DocumentCandidate>>(new List<DocumentCandidate>());
        public Task AcknowledgeAsync(DocumentProcess process, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public void Duplicate_source_ids_are_reported()
    {
        var errors = RouteValidator.ValidateSources(new IDocumentSource[] { new StubSource("view:a"), new StubSource("view:b"), new StubSource("view:a") });

        Assert.Single(errors);
        Assert.Contains("view:a", errors[0]);
        Assert.Empty(RouteValidator.ValidateSources(new IDocumentSource[] { new StubSource("view:a"), new StubSource("view:b") }));
    }
}
