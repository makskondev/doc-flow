using System.Text;

namespace DocFlow.Host;

public sealed class FakeSignatureVerifier : ISignatureVerifier
{
    public Task<VerificationResult> VerifyAsync(byte[] pdf, byte[] signature, CancellationToken ct)
    {
        var expected = "FAKE-SIG:" + Hashing.Sha256Hex(pdf);
        return Task.FromResult(Encoding.UTF8.GetString(signature) == expected
            ? new VerificationResult(true)
            : new VerificationResult(false, "подпись не соответствует файлу (заглушка)"));
    }
}
