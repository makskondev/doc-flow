namespace DocFlow.Engine;

public sealed class RouteValidationHostedService(
    RouteRegistry routes,
    IEnumerable<ISignalSource> signalSources,
    IEnumerable<IDocumentSource> documentSources,
    IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var errors = RouteValidator.Validate(routes, signalSources, services)
            .Concat(RouteValidator.ValidateSources(documentSources))
            .ToList();
        if (errors.Count > 0)
            throw new InvalidOperationException("Ошибки конфигурации:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
