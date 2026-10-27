using Microsoft.Extensions.Configuration;

namespace DocFlow.Data;

public static class PostgresServiceCollectionExtensions
{
    /// <summary>
    /// Подключает PostgreSQL (EF Core) как хранилище процессов и аудита. Строка подключения — ConnectionStrings:DocFlow.
    /// Вызывать до AddDocFlowEngine: хостед-сервис миграции должен стартовать раньше воркеров.
    /// </summary>
    public static IServiceCollection AddDocFlowPostgres(
        this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DocFlow")
            ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:DocFlow");

        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.AddDbContextFactory<DocFlowDbContext>(o => o.UseNpgsql(connectionString));

        services.AddSingleton<IProcessRepository, EfProcessRepository>();
        services.AddSingleton<IProcessAudit, EfProcessAudit>();
        services.AddHostedService<DatabaseMigrator>();
        return services;
    }
}
