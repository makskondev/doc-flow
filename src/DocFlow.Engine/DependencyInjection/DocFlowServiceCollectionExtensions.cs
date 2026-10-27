using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocFlow.Engine;

public static class DocFlowServiceCollectionExtensions
{
    /// <summary>
    /// Подключает движок: настройки, ядро, фоновые воркеры. Маршруты и шаги подключаются отдельно — <see cref="AddDocFlowFlows"/>.
    /// Репозиторий и аудит по умолчанию хранятся в памяти (разработка/тесты); хранилище на PostgreSQL регистрируется ДО этого вызова.
    /// IDocumentSource и IEscalationNotifier регистрирует хост.
    /// </summary>
    public static IServiceCollection AddDocFlowEngine(
        this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<EngineOptions>(configuration.GetSection("Engine"));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ProcessLocks>();
        services.TryAddSingleton<IProcessRepository, InMemoryProcessRepository>();
        services.TryAddSingleton<IProcessAudit, InMemoryProcessAudit>();
        services.AddSingleton<RouteRegistry>();
        services.AddSingleton<ProcessEngine>();
        services.AddSingleton<DocumentRegistrar>();

        services.AddHostedService<RouteValidationHostedService>();   // первым: упадёт до запуска воркеров
        services.AddHostedService<IntakeWorker>();
        services.AddHostedService<SignalScanWorker>();
        services.AddHostedService<ProcessRunnerWorker>();
        return services;
    }

    /// <summary>
    /// Находит в сборках и регистрирует все маршруты (IRoute), шаги (IStep) и источники сигналов (ISignalSource).
    /// Новый маршрут, шаг или сканер достаточно создать — регистрировать вручную не нужно.
    /// </summary>
    public static IServiceCollection AddDocFlowFlows(this IServiceCollection services, params Assembly[] assemblies)
    {
        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var type in types)
        {
            if (typeof(IRoute).IsAssignableFrom(type)) services.AddSingleton(typeof(IRoute), type);
            if (typeof(IStep).IsAssignableFrom(type)) services.AddTransient(type);
            if (typeof(ISignalSource).IsAssignableFrom(type)) services.AddSingleton(typeof(ISignalSource), type);
        }
        return services;
    }
}
