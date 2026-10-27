namespace DocFlow.Engine;

public sealed class CompiledRoute(
    string key, int version, IReadOnlyList<RouteNode> nodes,
    IReadOnlyCollection<string> requiredData, Func<DocumentCandidate, bool> matches)
{
    public IReadOnlyCollection<string> RequiredData { get; } = requiredData;
    public Func<DocumentCandidate, bool> Matches { get; } = matches;
    public string Key { get; } = key;
    public int Version { get; } = version;
    public IReadOnlyList<RouteNode> Nodes { get; } = nodes;

    public RouteNode First => Nodes[0];
    public RouteNode? Find(string id) => Nodes.FirstOrDefault(n => n.Id == id);

    public RouteNode? NextOf(string id)
    {
        for (var i = 0; i < Nodes.Count; i++)
            if (Nodes[i].Id == id)
                return i + 1 < Nodes.Count ? Nodes[i + 1] : null;
        return null;
    }
}
