using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocFlow.Tests;

public sealed class TestSigningService : ISigningService
{
    public Task<byte[]> SignAsync(byte[] pdf, CancellationToken ct)
        => Task.FromResult(System.Text.Encoding.UTF8.GetBytes("SIG:" + Hashing.Sha256Hex(pdf)));
}

public sealed class TestSignatureVerifier : ISignatureVerifier
{
    public Task<VerificationResult> VerifyAsync(byte[] pdf, byte[] signature, CancellationToken ct)
        => Task.FromResult(System.Text.Encoding.UTF8.GetString(signature) == "SIG:" + Hashing.Sha256Hex(pdf)
            ? new VerificationResult(true)
            : new VerificationResult(false, "подпись не соответствует файлу"));
}

/// <summary>Внешняя БД для тестов: запоминает, что в неё записали.</summary>
public sealed class FakeExternalDocumentDb : IExternalDocumentDb
{
    public Dictionary<Guid, string> Registered { get; } = new();          // processId -> id документа
    public Dictionary<string, byte[]?> Completed { get; } = new();        // id документа -> подпись (null = только отметка)

    public Task<string> RegisterDocumentAsync(Guid processId, DocumentInfo info, CancellationToken ct)
    {
        if (!Registered.TryGetValue(processId, out var id)) Registered[processId] = id = "doc-" + (Registered.Count + 1);
        return Task.FromResult(id);
    }

    public Task<bool> IsCompletedAsync(string documentId, CancellationToken ct) => Task.FromResult(Completed.ContainsKey(documentId));

    public Task CompleteAsync(string documentId, byte[]? signature, CancellationToken ct)
    {
        Completed[documentId] = signature;
        return Task.CompletedTask;
    }
}

public sealed class NoopNotifier : IEscalationNotifier
{
    public Task NotifyAsync(DocumentProcess process, CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Настоящие маршруты, шаги и сканер из DocFlow.Documents на файловом хранилище в памяти.</summary>
public sealed class ScenarioHarness
{
    public TestClock Clock { get; } = new();
    public InMemoryFileStorage Files { get; } = new();
    public FakeExternalDocumentDb Db { get; } = new();

    public StorageOptions Storage { get; } = new()
    {
        WorkingDir = "/work",
        Types =
        {
            ["zop"] = new TypeDirectories { SignInputDir = "/zop/in", SignResultDir = "/zop/out", FinalDir = "/zop/final" },
            ["report"] = new TypeDirectories { FinalDir = "/report/final" },
            ["inbox"] = new TypeDirectories { FinalDir = "/inbox/final" },
            ["archive"] = new TypeDirectories { SearchDir = "/archive/search", FinalDir = "/archive/final" },
        }
    };

    public ServiceProvider Services { get; }
    public ProcessEngine Engine { get; }
    public IProcessRepository Repo { get; }
    public DocumentRegistrar Registrar { get; }
    public ISignalSource Scanner { get; }

    public ScenarioHarness()
    {
        var s = new ServiceCollection();
        s.AddSingleton(Microsoft.Extensions.Options.Options.Create(new EngineOptions { MaxAttempts = 3, RetryBaseDelaySeconds = 10 }));
        s.AddSingleton(Microsoft.Extensions.Options.Options.Create(Storage));
        s.AddSingleton<TimeProvider>(Clock);
        s.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        s.AddSingleton<IFileStorage>(Files);
        s.AddSingleton<ISigningService, TestSigningService>();
        s.AddSingleton<ISignatureVerifier, TestSignatureVerifier>();
        s.AddSingleton<IExternalDocumentDb>(Db);
        s.AddSingleton<IEscalationNotifier, NoopNotifier>();
        s.AddSingleton<IProcessRepository, InMemoryProcessRepository>();
        s.AddSingleton<IProcessAudit, InMemoryProcessAudit>();
        s.AddSingleton<ProcessLocks>();
        s.AddSingleton<RouteRegistry>();
        s.AddSingleton<ProcessEngine>();
        s.AddSingleton<DocumentRegistrar>();
        s.AddDocFlowFlows(typeof(DocumentTypes).Assembly);     // маршруты, шаги и сканер из DocFlow.Documents
        Services = s.BuildServiceProvider();

        Engine = Services.GetRequiredService<ProcessEngine>();
        Repo = Services.GetRequiredService<IProcessRepository>();
        Registrar = Services.GetRequiredService<DocumentRegistrar>();
        Scanner = Services.GetServices<ISignalSource>().Single(x => x.Signal == Signals.SigningResult);
    }

    public static string Pdf(string body) => "%PDF-1.4 " + body;

    public async Task<Guid> RegisterAsync(DocumentCandidate candidate)
    {
        var ids = await Registrar.RegisterAsync(new[] { candidate }, Clock.Now, CancellationToken.None);
        return Assert.Single(ids);
    }

    public Task AdvanceAsync(Guid id) => Engine.AdvanceAsync(id, CancellationToken.None);
    public async Task<DocumentProcess> GetAsync(Guid id) => (await Repo.GetAsync(id, CancellationToken.None))!;
}
