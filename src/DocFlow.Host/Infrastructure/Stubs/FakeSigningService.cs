using System.Text;

namespace DocFlow.Host;

public sealed class FakeSigningService : ISigningService
{
    public Task<byte[]> SignAsync(byte[] pdf, CancellationToken ct)
        => Task.FromResult(Encoding.UTF8.GetBytes("FAKE-SIG:" + Hashing.Sha256Hex(pdf)));
}
