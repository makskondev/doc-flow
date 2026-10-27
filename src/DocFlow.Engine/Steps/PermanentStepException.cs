namespace DocFlow.Engine;

/// <summary>Повторять бессмысленно: сразу эскалация оператору.</summary>
public sealed class PermanentStepException(string message, Exception? inner = null) : Exception(message, inner);
