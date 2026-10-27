using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocFlow.Tests;

public sealed class Harness
{
    public TestClock Clock { get; } = new();
    public Probe Probe { get; } = new();
    public ServiceProvider Services { get; }
    public ProcessEngine Engine { get; }
    public IProcessRepository Repo { get; }
    public DocumentRegistrar Registrar { get; }
    public RouteRegistry Routes { get; }
    public IProcessAudit Audit { get; }

    public Harness()
    {
        var s = new ServiceCollection();
        s.AddSingleton(Microsoft.Extensions.Options.Options.Create(new EngineOptions { MaxAttempts = 3, RetryBaseDelaySeconds = 10 }));
        s.AddSingleton<TimeProvider>(Clock);
        s.AddSingleton(Probe);
        s.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        s.AddSingleton<IProcessRepository, InMemoryProcessRepository>();
        s.AddSingleton<IProcessAudit, InMemoryProcessAudit>();
        s.AddSingleton<IEscalationNotifier, RecordingNotifier>();
        s.AddSingleton<ProcessLocks>();
        s.AddSingleton<IRoute, LinearRoute>();
        s.AddSingleton<IRoute, WaitRoute>();
        s.AddSingleton<IRoute, DataRoute>();
        s.AddSingleton<IRoute, KindARoute>();
        s.AddSingleton<IRoute, KindBRoute>();
        s.AddSingleton<IRoute, TwinRoute1>();
        s.AddSingleton<IRoute, TwinRoute2>();
        s.AddSingleton<IRoute, BrokenRoute>();
        s.AddTransient<StepA>();
        s.AddTransient<StepB>();
        s.AddSingleton<RouteRegistry>();
        s.AddSingleton<ProcessEngine>();
        s.AddSingleton<DocumentRegistrar>();
        Services = s.BuildServiceProvider();

        Engine = Services.GetRequiredService<ProcessEngine>();
        Repo = Services.GetRequiredService<IProcessRepository>();
        Registrar = Services.GetRequiredService<DocumentRegistrar>();
        Routes = Services.GetRequiredService<RouteRegistry>();
        Audit = Services.GetRequiredService<IProcessAudit>();
    }

    /// <summary>Зарегистрировать документ и вернуть id процесса (или null, если процесс не создан).</summary>
    public async Task<Guid?> RegisterAsync(
        string type, string externalId = "doc1", DateTime? pollStartedAt = null, Dictionary<string, string>? data = null)
    {
        // Тип задаётся атрибутом "type": по нему срабатывают правила Matches тестовых маршрутов
        var attributes = new Dictionary<string, string>(data ?? new Dictionary<string, string>()) { ["type"] = type };
        var ids = await RegisterCandidatesAsync(pollStartedAt, new DocumentCandidate("test", externalId, "/x", attributes));
        return ids.Count == 0 ? null : ids[0];
    }

    /// <summary>Зарегистрировать произвольных кандидатов (тип определяется правилами Matches).</summary>
    public Task<IReadOnlyList<Guid>> RegisterCandidatesAsync(DateTime? pollStartedAt, params DocumentCandidate[] candidates)
        => Registrar.RegisterAsync(candidates, pollStartedAt ?? Clock.Now, CancellationToken.None);

    public async Task<DocumentProcess> GetAsync(Guid id) => (await Repo.GetAsync(id, CancellationToken.None))!;
    public Task AdvanceAsync(Guid id) => Engine.AdvanceAsync(id, CancellationToken.None);
}
