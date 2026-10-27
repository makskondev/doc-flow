namespace DocFlow.Documents;

/// <summary>REST-сервис подписи сертификатом. Возвращает отсоединённую подпись.</summary>
public interface ISigningService
{
    Task<byte[]> SignAsync(byte[] pdf, CancellationToken ct);
}
