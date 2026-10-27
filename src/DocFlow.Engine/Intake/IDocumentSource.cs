namespace DocFlow.Engine;

public interface IDocumentSource
{
    string SourceId { get; }

    /// <summary>Что источник видит прямо сейчас. Повторные выдачи одного и того же документа — норма (дедуп в регистраторе).</summary>
    Task<IReadOnlyList<DocumentCandidate>> PollAsync(CancellationToken ct);

    /// <summary>Вызывается шагом приёма, когда документ надёжно скопирован в рабочий каталог. Должен быть идемпотентным.</summary>
    Task AcknowledgeAsync(DocumentProcess process, CancellationToken ct);
}
