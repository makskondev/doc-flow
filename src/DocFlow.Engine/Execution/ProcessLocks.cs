namespace DocFlow.Engine;

/// <summary>Блокировки по процессу (в рамках одного экземпляра сервиса). Полосатые, чтобы не копить объекты.</summary>
public sealed class ProcessLocks
{
    private readonly SemaphoreSlim[] _stripes = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<IDisposable> AcquireAsync(Guid id, CancellationToken ct)
    {
        var stripe = _stripes[(id.GetHashCode() & int.MaxValue) % _stripes.Length];
        await stripe.WaitAsync(ct);
        return new Releaser(stripe);
    }

    private sealed class Releaser(SemaphoreSlim stripe) : IDisposable
    {
        private int _released;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0) stripe.Release();
        }
    }
}
