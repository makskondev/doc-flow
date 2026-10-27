namespace DocFlow.Data;

/// <summary>Применяет миграции при старте. Если миграций в сборке нет — падает сразу с понятным сообщением.</summary>
public sealed class DatabaseMigrator(
    IDbContextFactory<DocFlowDbContext> factory, IOptions<DatabaseOptions> options, ILogger<DatabaseMigrator> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MigrateOnStartup) return;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!db.Database.GetMigrations().Any())
            throw new InvalidOperationException(
                "В проекте DocFlow.Data нет миграций. Создайте: dotnet ef migrations add InitialCreate --project src/DocFlow.Data");

        await db.Database.MigrateAsync(cancellationToken);
        log.LogInformation("Миграции БД применены");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
