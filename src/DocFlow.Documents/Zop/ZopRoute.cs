namespace DocFlow.Documents;

/// <summary>
/// Сценарий 1. Запись во view_1 → штамп → файл в папку для подписи → ждём файл и подпись в директории результата подписания →
/// проверка → подпись и отметка в БД → файл в конечный каталог.
/// </summary>
public sealed class ZopRoute : IRoute
{
    public string Key => DocumentTypes.Zop;
    public int Version => 1;

    // Правило распознавания (пример): запись из view_1 с признаком вида "zop". Задайте своё правило.
    public bool Matches(DocumentCandidate c)
        => c.SourceId == SourceIds.View1 && c.Get(DataKeys.DocKind) == DocumentTypes.Zop;

    // Данные для штампа должны прийти из вьюхи; без них процесс не создаётся
    public IReadOnlyCollection<string> RequiredData => new[] { DataKeys.FullName, DataKeys.AgreementNumber, DataKeys.AgreementDate };

    public void Build(IRouteBuilder r) => r
        .Step<IntakeStep>()
        .Step<ApplyStampStep>()
        .Step<SendToSignatureStep>()
        .WaitFor(Signals.SigningResult, TimeSpan.FromDays(7))   // таймаут -> эскалация (письмо + оператор)
        .Include(RouteFragments.Finish);
}
