namespace DocFlow.Engine;

/// <summary>
/// Шаг обработки. ДОЛЖЕН быть идемпотентным: после сбоя движок повторит его с начала.
/// Обычная ошибка = исключение (будут повторы с backoff). Ошибка без смысла повторять = <see cref="PermanentStepException"/>.
/// </summary>
public interface IStep
{
    Task ExecuteAsync(StepContext ctx, CancellationToken ct);
}
