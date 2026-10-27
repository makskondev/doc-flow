using Microsoft.EntityFrameworkCore.Design;

namespace DocFlow.Data;

/// <summary>Нужна утилите dotnet ef: создаёт контекст без запуска хоста. Строка подключения — из DOCFLOW_CONNECTION (или локальная по умолчанию).</summary>
public sealed class DocFlowDbContextFactory : IDesignTimeDbContextFactory<DocFlowDbContext>
{
    public DocFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOCFLOW_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=docflow;Username=docflow;Password=docflow";
        var options = new DbContextOptionsBuilder<DocFlowDbContext>().UseNpgsql(connectionString).Options;
        return new DocFlowDbContext(options);
    }
}
