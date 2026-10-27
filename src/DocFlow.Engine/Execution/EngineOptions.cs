namespace DocFlow.Engine;

public sealed class EngineOptions
{
    /// <summary>Как часто опрашивать источники документов (каталоги, вьюху).</summary>
    public int PollIntervalSeconds { get; set; } = 15;
    /// <summary>Как часто проверять сигнальные источники (файлы подписи).</summary>
    public int SignalScanIntervalSeconds { get; set; } = 15;
    /// <summary>Как часто раннер подхватывает готовые процессы, таймауты и неотправленные письма.</summary>
    public int RunnerIntervalSeconds { get; set; } = 3;
    public int MaxAttempts { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 30;
    public int MaxParallelProcesses { get; set; } = 4;
}
