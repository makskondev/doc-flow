namespace DocFlow.Engine;

// Эскалация: перевод процесса в NeedsAttention и письмо оператору (с повтором отправки при сбое).
public sealed partial class ProcessEngine
{
    /// <summary>Отправить письмо по процессам, где отправка не удалась.</summary>
    public async Task NotifyPendingAsync(Guid id, CancellationToken ct)
    {
        using var gate = await _locks.AcquireAsync(id, ct);
        var p = await _repo.GetAsync(id, ct);
        if (p is { Status: ProcessStatus.NeedsAttention, EscalationSentAt: null })
            await TryNotifyAsync(p, ct);
    }

    private async Task EscalateAsync(DocumentProcess p, string reason, CancellationToken ct)
    {
        p.Status = ProcessStatus.NeedsAttention;
        p.AttentionReason = reason;
        p.WaitingFor = null;
        p.TimeoutAt = null;
        p.NextAttemptAt = null;
        await SaveAsync(p, ct);
        _audit.Record(p.Id, p.CurrentNodeId, "Escalated", reason);
        await TryNotifyAsync(p, ct);
    }

    private async Task TryNotifyAsync(DocumentProcess p, CancellationToken ct)
    {
        try
        {
            await _notifier.NotifyAsync(p, ct);
            p.EscalationSentAt = Now;
            await SaveAsync(p, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Не страшно: ProcessRunnerWorker повторит отправку (EscalationSentAt остался пустым).
            _log.LogError(ex, "Не удалось отправить письмо об эскалации процесса {Id}", p.Id);
        }
    }
}
