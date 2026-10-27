namespace DocFlow.Documents;

public static class StepContextExtensions
{
    /// <summary>Значение, которое должен был оставить один из предыдущих шагов (StepContext.Bag). Нет значения = ошибка маршрута.</summary>
    public static string RequireBag(this StepContext ctx, string key)
        => ctx.Bag.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new PermanentStepException($"Нет значения '{key}': его должен установить один из предыдущих шагов маршрута");

    /// <summary>
    /// Идентификатор документа во внешней БД: выданный шагом регистрации (документы из каталогов)
    /// либо идентификатор записи во вьюхе (документы из вьюх).
    /// </summary>
    public static string ExternalDocumentId(this StepContext ctx)
        => ctx.Bag.TryGetValue(BagKeys.ExternalDocId, out var id) ? id : ctx.ExternalId;
}
