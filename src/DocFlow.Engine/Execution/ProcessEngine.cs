namespace DocFlow.Engine;

/// <summary>
/// Ядро выполнения: продвигает процесс по узлам маршрута, паркует на ожиданиях, повторяет сбойные шаги, эскалирует.
/// Класс разбит на части (partial):
///   ProcessEngine.cs            — продвижение процесса по маршруту (AdvanceAsync);
///   ProcessEngine.Commands.cs   — команды: сигнал, таймаут, действия оператора (retry / continue / cancel);
///   ProcessEngine.Escalation.cs — эскалация и отправка письма.
/// Команды только меняют состояние; шаги выполняет AdvanceAsync (его вызывает ProcessRunnerWorker).
/// </summary>
public sealed partial class ProcessEngine
{
    private readonly IProcessRepository _repo;
    private readonly RouteRegistry _routes;
    private readonly IServiceScopeFactory _scopes;
    private readonly IEscalationNotifier _notifier;
    private readonly IProcessAudit _audit;
    private readonly ProcessLocks _locks;
    private readonly IOptions<EngineOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<ProcessEngine> _log;

    public ProcessEngine(
        IProcessRepository repo,
        RouteRegistry routes,
        IServiceScopeFactory scopes,
        IEscalationNotifier notifier,
        IProcessAudit audit,
        ProcessLocks locks,
        IOptions<EngineOptions> options,
        TimeProvider clock,
        ILogger<ProcessEngine> log)
    {
        _repo = repo;
        _routes = routes;
        _scopes = scopes;
        _notifier = notifier;
        _audit = audit;
        _locks = locks;
        _options = options;
        _clock = clock;
        _log = log;
    }

    private EngineOptions Opt => _options.Value;
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    /// <summary>Выполняет узлы подряд, пока процесс не припаркуется, не уйдёт в ожидание повтора, на эскалацию или не завершится.</summary>
    public async Task AdvanceAsync(Guid id, CancellationToken ct)
    {
        using var gate = await _locks.AcquireAsync(id, ct);
        while (true)
        {
            var p = await _repo.GetAsync(id, ct);
            if (p is null || p.Status != ProcessStatus.Running) return;
            if (p.NextAttemptAt is { } due && due > Now) return;

            if (!_routes.TryGet(p.RouteKey, out var route) || route.Find(p.CurrentNodeId) is not { } node)
            {
                await EscalateAsync(p, $"Маршрут '{p.RouteKey}' или узел '{p.CurrentNodeId}' не найден (версия в процессе: {p.RouteVersion})", ct);
                return;
            }

            if (node is WaitNode wait)
            {
                p.Status = ProcessStatus.Waiting;
                p.WaitingFor = wait.Signal;
                p.TimeoutAt = Now + wait.Timeout;
                await SaveAsync(p, ct);
                _audit.Record(p.Id, wait.Id, "Waiting", wait.Signal);
                return;
            }

            var step = (StepNode)node;
            _audit.Record(p.Id, step.Id, "StepStarted");
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var impl = (IStep)scope.ServiceProvider.GetRequiredService(step.StepType);
                await impl.ExecuteAsync(new StepContext(p), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (PermanentStepException ex)
            {
                _audit.Record(p.Id, step.Id, "StepFailed", ex.Message);
                await EscalateAsync(p, $"{step.Id}: {ex.Message}", ct);
                return;
            }
            catch (Exception ex)
            {
                p.Attempts++;
                _audit.Record(p.Id, step.Id, "StepFailed", $"попытка {p.Attempts}/{Opt.MaxAttempts}: {ex.Message}");
                _log.LogWarning(ex, "Шаг {Step} процесса {Id} завершился ошибкой", step.Id, p.Id);
                if (p.Attempts >= Opt.MaxAttempts)
                {
                    await EscalateAsync(p, $"{step.Id}: {ex.Message}", ct);
                    return;
                }
                p.NextAttemptAt = Now.AddSeconds(Opt.RetryBaseDelaySeconds * Math.Pow(2, p.Attempts - 1));
                await SaveAsync(p, ct);
                return;
            }

            _audit.Record(p.Id, step.Id, "StepCompleted");
            p.Attempts = 0;
            p.NextAttemptAt = null;
            MoveNext(p, route);
            await SaveAsync(p, ct);
            if (p.Status == ProcessStatus.Completed)
            {
                _audit.Record(p.Id, step.Id, "Completed");
                return;
            }
        }
    }

    private void MoveNext(DocumentProcess p, CompiledRoute route)
    {
        var next = route.NextOf(p.CurrentNodeId);
        if (next is null)
        {
            p.Status = ProcessStatus.Completed;
            p.CompletedAt = Now;
        }
        else
        {
            p.CurrentNodeId = next.Id;
        }
    }

    private async Task SaveAsync(DocumentProcess p, CancellationToken ct)
    {
        p.UpdatedAt = Now;
        await _repo.SaveAsync(p, ct);
    }
}
