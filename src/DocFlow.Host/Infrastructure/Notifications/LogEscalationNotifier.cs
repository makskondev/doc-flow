namespace DocFlow.Host;

/// <summary>Заглушка письма: пишет в лог. Реализуйте отправку через SMTP / почтовый сервис.</summary>
public sealed class LogEscalationNotifier(ILogger<LogEscalationNotifier> log) : IEscalationNotifier
{
    public Task NotifyAsync(DocumentProcess p, CancellationToken ct)
    {
        log.LogWarning("ЭСКАЛАЦИЯ: процесс {Id}, документ {Doc}, тип {Type}, узел {Node}: {Reason}",
            p.Id, p.ExternalId, p.RouteKey, p.CurrentNodeId, p.AttentionReason);
        return Task.CompletedTask;
    }
}
