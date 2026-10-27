namespace DocFlow.Documents;

/// <summary>Общие фрагменты маршрутов.</summary>
public static class RouteFragments
{
    /// <summary>
    /// Общий хвост маршрутов с подписью: проверка подписи → подпись и отметка во внешней БД → перенос в конечный каталог.
    /// </summary>
    public static void Finish(IRouteBuilder r) => r
        .Step<VerifySignatureStep>()
        .Step<PersistSignatureStep>()
        .Step<MoveToFinalStep>();
}
