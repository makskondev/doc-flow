namespace DocFlow.Engine;

// Команды: сигнал, таймаут и действия оператора. Меняют состояние процесса; выполнение шагов — в AdvanceAsync.
public sealed partial class ProcessEngine
{
    /// <summary>Пришёл сигнал. false — процесс не ждёт этот сигнал (поздний/дублирующий сигнал игнорируется).</summary>
    public Task<bool> SignalAsync(Guid id, string signal, CancellationToken ct)
        => MutateAsync(id, p =>
        {
            if (p.Status != ProcessStatus.Waiting || p.WaitingFor != signal) return false;
            _audit.Record(p.Id, p.CurrentNodeId, "Signaled", signal);
            p.WaitingFor = null;
            p.TimeoutAt = null;
            p.Status = ProcessStatus.Running;
            MoveNext(p, _routes.Get(p.RouteKey));
            return true;
        }, ct);

    /// <summary>Истёк таймаут ожидания: эскалация.</summary>
    public async Task TimeoutAsync(Guid id, CancellationToken ct)
    {
        using var gate = await _locks.AcquireAsync(id, ct);
        var p = await _repo.GetAsync(id, ct);
        if (p is null || p.Status != ProcessStatus.Waiting || p.TimeoutAt is not { } t || t > Now) return;
        await EscalateAsync(p, $"Таймаут ожидания '{p.WaitingFor}'", ct);
    }

    /// <summary>Повторить текущий узел (для WaitFor — снова встать в ожидание с новым таймаутом).</summary>
    public Task<bool> RetryAsync(Guid id, CancellationToken ct)
        => MutateAsync(id, p =>
        {
            if (p.Status != ProcessStatus.NeedsAttention) return false;
            _audit.Record(p.Id, p.CurrentNodeId, "Retry");
            ClearAttention(p);
            p.Status = ProcessStatus.Running;
            return true;
        }, ct);

    /// <summary>Пропустить текущий узел и идти дальше (осознанное решение оператора).</summary>
    public Task<bool> ContinueAsync(Guid id, CancellationToken ct)
        => MutateAsync(id, p =>
        {
            if (p.Status != ProcessStatus.NeedsAttention) return false;
            _audit.Record(p.Id, p.CurrentNodeId, "Continue", "узел пропущен оператором");
            ClearAttention(p);
            p.Status = ProcessStatus.Running;
            MoveNext(p, _routes.Get(p.RouteKey));
            return true;
        }, ct);

    public Task<bool> CancelAsync(Guid id, CancellationToken ct)
        => MutateAsync(id, p =>
        {
            if (p.IsTerminal) return false;
            _audit.Record(p.Id, p.CurrentNodeId, "Cancelled");
            p.Status = ProcessStatus.Cancelled;
            p.WaitingFor = null;
            p.TimeoutAt = null;
            return true;
        }, ct);

    private static void ClearAttention(DocumentProcess p)
    {
        p.Attempts = 0;
        p.NextAttemptAt = null;
        p.AttentionReason = null;
        p.EscalationSentAt = null;
    }

    private async Task<bool> MutateAsync(Guid id, Func<DocumentProcess, bool> mutate, CancellationToken ct)
    {
        using var gate = await _locks.AcquireAsync(id, ct);
        var p = await _repo.GetAsync(id, ct);
        if (p is null || !mutate(p)) return false;
        await SaveAsync(p, ct);
        return true;
    }
}
