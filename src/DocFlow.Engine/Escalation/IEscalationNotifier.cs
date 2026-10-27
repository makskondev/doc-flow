namespace DocFlow.Engine;

/// <summary>Отправка письма об эскалации. Должна быть безопасной к повторам (письмо может уйти дважды при сбое сохранения).</summary>
public interface IEscalationNotifier
{
    Task NotifyAsync(DocumentProcess process, CancellationToken ct);
}
