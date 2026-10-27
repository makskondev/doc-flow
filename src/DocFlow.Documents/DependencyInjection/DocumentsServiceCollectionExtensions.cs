namespace DocFlow.Documents;

public static class DocumentsServiceCollectionExtensions
{
    /// <summary>
    /// Подключает бизнес-логику документов: настройки каталогов (секция Storage), все маршруты, шаги и сканеры этой сборки
    /// и источники документов по описаниям вьюх.
    /// </summary>
    public static IServiceCollection AddDocFlowDocuments(
        this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        // На каждое описание вьюхи (IPendingDocumentsReader) — свой источник документов со своим SourceId
        var readers = typeof(DocumentTypes).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IPendingDocumentsReader).IsAssignableFrom(t));
        foreach (var readerType in readers)
        {
            services.AddSingleton(readerType);
            services.AddSingleton<IDocumentSource>(sp => new ViewDocumentSource((IPendingDocumentsReader)sp.GetRequiredService(readerType)));
        }

        return services.AddDocFlowFlows(typeof(DocumentTypes).Assembly);
    }
}
