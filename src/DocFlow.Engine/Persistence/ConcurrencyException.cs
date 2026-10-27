namespace DocFlow.Engine;

public sealed class ConcurrencyException(string message) : Exception(message);
