namespace DocFlow.Engine;

public sealed class StepContext(DocumentProcess process)
{
    public DocumentProcess Process { get; } = process;
    public Guid Id => Process.Id;
    public string ExternalId => Process.ExternalId;
    public IDictionary<string, string> Bag => Process.Bag;

    /// <summary>Входные данные документа из источника (только чтение).</summary>
    public IReadOnlyDictionary<string, string> Data => Process.Data;

    /// <summary>Обязательное поле входных данных. Нет поля = ошибка, повторять бессмысленно (эскалация).</summary>
    public string RequireData(string key)
        => Process.Data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new PermanentStepException($"Во входных данных документа нет обязательного поля '{key}'");
}
