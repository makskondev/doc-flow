using System.Diagnostics.CodeAnalysis;

namespace DocFlow.Engine;

public sealed class RouteRegistry
{
    private readonly Dictionary<string, CompiledRoute> _routes = new(StringComparer.OrdinalIgnoreCase);

    public RouteRegistry(IEnumerable<IRoute> routes)
    {
        foreach (var route in routes)
        {
            if (_routes.ContainsKey(route.Key))
                throw new InvalidOperationException($"Дублирующийся маршрут '{route.Key}'");
            var builder = new RouteBuilder();
            route.Build(builder);
            _routes[route.Key] = new CompiledRoute(route.Key, route.Version, builder.Nodes.ToList(), route.RequiredData, route.Matches);
        }
    }

    public IReadOnlyCollection<CompiledRoute> All => _routes.Values;

    /// <summary>Маршруты, правило Matches которых подходит документу (ожидается ровно один).</summary>
    public IReadOnlyList<CompiledRoute> FindMatches(DocumentCandidate candidate)
        => _routes.Values.Where(r => r.Matches(candidate)).ToList();

    public bool TryGet(string key, [NotNullWhen(true)] out CompiledRoute? route)
        => _routes.TryGetValue(key, out route);

    public CompiledRoute Get(string key)
        => _routes.TryGetValue(key, out var r) ? r : throw new NotSupportedException($"Нет маршрута для типа документа '{key}'");
}
