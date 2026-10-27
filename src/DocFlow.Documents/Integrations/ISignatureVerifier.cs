namespace DocFlow.Documents;

/// <summary>API проверки подписи. Проверка обязательна для любой подписи (автоматической и пользовательской).</summary>
public interface ISignatureVerifier
{
    Task<VerificationResult> VerifyAsync(byte[] pdf, byte[] signature, CancellationToken ct);
}
