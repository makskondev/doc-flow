namespace DocFlow.Engine;

/// <summary>Один прогон обработки документа. Сохраняется в БД целиком.</summary>
public sealed class DocumentProcess
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // Идентичность документа в источнике. (SourceId, ExternalId, Run) — уникальны.
    public string SourceId { get; init; } = "";
    public string ExternalId { get; init; } = "";
    public int Run { get; init; } = 1;

    /// <summary>RouteKey = тип документа.</summary>
    public string RouteKey { get; init; } = "";
    public string ContentPath { get; init; } = "";

    public int RouteVersion { get; set; }
    /// <summary>Идентификатор узла маршрута, на котором стоит курсор (а не индекс: правка маршрута не ломает живые процессы).</summary>
    public string CurrentNodeId { get; set; } = "";
    public ProcessStatus Status { get; set; } = ProcessStatus.Running;

    public string? WaitingFor { get; set; }
    public DateTime? TimeoutAt { get; set; }

    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }

    public string? AttentionReason { get; set; }
    public DateTime? EscalationSentAt { get; set; }

    /// <summary>Данные, которыми шаги обмениваются между собой (пути, хеши).</summary>
    public Dictionary<string, string> Bag { get; set; } = new();

    /// <summary>
    /// Входные данные документа из источника (например, поля для штампа: ФИО, даты, номер соглашения).
    /// Снимок на момент регистрации; шаги их только читают (для своих данных есть Bag).
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = new();

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Версия для optimistic lock.</summary>
    public long Version { get; set; }

    public bool IsTerminal => Status is ProcessStatus.Completed or ProcessStatus.Cancelled;

    public DocumentProcess Clone()
    {
        var copy = (DocumentProcess)MemberwiseClone();
        copy.Bag = new Dictionary<string, string>(Bag);
        copy.Data = new Dictionary<string, string>(Data);
        return copy;
    }
}
