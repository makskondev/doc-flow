namespace DocFlow.Engine;

/// <summary>Источник внешнего сигнала для узлов WaitFor (например, сканер каталога с файлами подписи).</summary>
public interface ISignalSource
{
    string Signal { get; }
    Task ScanAsync(CancellationToken ct);
}
