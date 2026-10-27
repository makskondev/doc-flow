namespace DocFlow.Engine;

internal sealed class RouteBuilder : IRouteBuilder
{
    private readonly List<RouteNode> _nodes = new();
    public IReadOnlyList<RouteNode> Nodes => _nodes;

    public IRouteBuilder Step<TStep>() where TStep : IStep
        => Add(typeof(TStep).Name, id => new StepNode(id, typeof(TStep)));

    public IRouteBuilder WaitFor(string signal, TimeSpan timeout)
        => Add("wait:" + signal, id => new WaitNode(id, signal, timeout));

    public IRouteBuilder Include(Action<IRouteBuilder> fragment)
    {
        fragment(this);
        return this;
    }

    // Один и тот же шаг можно использовать дважды: id получит суффикс #2, #3...
    private IRouteBuilder Add(string baseId, Func<string, RouteNode> make)
    {
        var id = baseId;
        var n = 1;
        while (_nodes.Any(x => x.Id == id)) id = $"{baseId}#{++n}";
        _nodes.Add(make(id));
        return this;
    }
}
