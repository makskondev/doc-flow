namespace DocFlow.Documents;

public sealed record VerificationResult(bool IsValid, string? Reason = null);
