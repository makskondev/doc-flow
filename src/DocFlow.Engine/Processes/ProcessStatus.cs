namespace DocFlow.Engine;

public enum ProcessStatus
{
    /// <summary>Выполняется или ждёт запуска следующего узла.</summary>
    Running,
    /// <summary>Припаркован на узле WaitFor до внешнего сигнала.</summary>
    Waiting,
    /// <summary>Таймаут или ошибка: отправлено письмо, ждём оператора (retry / continue / cancel).</summary>
    NeedsAttention,
    Completed,
    Cancelled
}
