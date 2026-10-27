namespace DocFlow.Tests;

// Правила Matches тестовых маршрутов опираются на атрибут "type" (его проставляет Harness.RegisterAsync)

public sealed class LinearRoute : IRoute
{
    public string Key => "linear";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.Get("type") == "linear";
    public void Build(IRouteBuilder r) => r.Step<StepA>().Step<StepB>();
}

public sealed class WaitRoute : IRoute
{
    public string Key => "wait";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.Get("type") == "wait";
    public void Build(IRouteBuilder r) => r.Step<StepA>().WaitFor("go", TimeSpan.FromHours(1)).Step<StepB>();
}

public sealed class DataRoute : IRoute
{
    public string Key => "data";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.Get("type") == "data";
    public IReadOnlyCollection<string> RequiredData => new[] { "fio" };
    public void Build(IRouteBuilder r) => r.Step<StepA>();
}

// Один источник, разные типы по значению атрибута
public sealed class KindARoute : IRoute
{
    public string Key => "kind-a";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.SourceId == "shared" && c.Get("kind") == "a";
    public void Build(IRouteBuilder r) => r.Step<StepA>();
}

public sealed class KindBRoute : IRoute
{
    public string Key => "kind-b";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.SourceId == "shared" && c.Get("kind") == "b";
    public void Build(IRouteBuilder r) => r.Step<StepA>();
}

// Два маршрута подходят одному документу: ошибка конфигурации
public sealed class TwinRoute1 : IRoute
{
    public string Key => "twin-1";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.Get("type") == "twin";
    public void Build(IRouteBuilder r) => r.Step<StepA>();
}

public sealed class TwinRoute2 : IRoute
{
    public string Key => "twin-2";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.Get("type") == "twin";
    public void Build(IRouteBuilder r) => r.Step<StepA>();
}

// Правило с ошибкой: не должно ронять остальные документы пачки
public sealed class BrokenRoute : IRoute
{
    public string Key => "broken";
    public int Version => 1;
    public bool Matches(DocumentCandidate c) => c.Get("type") == "boom" ? throw new InvalidOperationException("ошибка в правиле") : false;
    public void Build(IRouteBuilder r) => r.Step<StepA>();
}
