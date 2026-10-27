namespace DocFlow.Host;

public static class HostServiceCollectionExtensions
{
    /// <summary>
    /// Подключает инфраструктуру: файловое хранилище, источник «каталоги», внешние системы.
    /// Всё, что в папке Infrastructure/Stubs — заглушки: замените реальными реализациями (см. docs/before-production.md).
    /// </summary>
    public static IServiceCollection AddDocFlowHostInfrastructure(
        this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<IntakeOptions>(configuration.GetSection("Intake"));

        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // Источник «каталоги». Источники «вьюхи» создаются автоматически по описаниям IPendingDocumentsReader (DocFlow.Documents)
        services.AddSingleton<IDocumentSource, DirectoryDocumentSource>();

        // Реальные реализации
        services.AddSingleton<IEscalationNotifier, LogEscalationNotifier>();   // TODO: письмо (SMTP)

        // ЗАГЛУШКИ — заменить
        services.AddSingleton<IExternalDocumentDb, InMemoryExternalDocumentDb>();         // внешняя БД: регистрация документа, подпись и отметка
        services.AddSingleton<ISigningService, FakeSigningService>();                     // REST-сервис подписи
        services.AddSingleton<ISignatureVerifier, FakeSignatureVerifier>();               // REST-API проверки подписи
        return services;
    }
}
