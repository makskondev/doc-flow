namespace DocFlow.Engine;

public abstract record RouteNode(string Id);

public sealed record StepNode(string Id, Type StepType) : RouteNode(Id);

public sealed record WaitNode(string Id, string Signal, TimeSpan Timeout) : RouteNode(Id);
