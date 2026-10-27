namespace DocFlow.Tests;

public class ClassificationTests
{
    private static DocumentCandidate Doc(string source, string id, params (string Key, string Value)[] data)
        => new(source, id, "/x", data.ToDictionary(d => d.Key, d => d.Value));

    [Fact]
    public async Task One_source_with_different_attribute_values_selects_different_routes()
    {
        var h = new Harness();

        var ids = await h.RegisterCandidatesAsync(null,
            Doc("shared", "1", ("kind", "a")),
            Doc("shared", "2", ("kind", "b")));

        Assert.Equal(2, ids.Count);
        Assert.Equal("kind-a", (await h.GetAsync(ids[0])).RouteKey);
        Assert.Equal("kind-b", (await h.GetAsync(ids[1])).RouteKey);
    }

    [Fact]
    public async Task The_rule_can_depend_on_the_source()
    {
        var h = new Harness();
        var ids = await h.RegisterCandidatesAsync(null, Doc("other-source", "1", ("kind", "a")));
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Document_matching_several_routes_is_not_registered()
    {
        var h = new Harness();
        var ids = await h.RegisterCandidatesAsync(null, Doc("test", "1", ("type", "twin")));
        Assert.Empty(ids);
    }

    [Fact]
    public async Task A_throwing_rule_does_not_stop_other_documents_of_the_batch()
    {
        var h = new Harness();

        var ids = await h.RegisterCandidatesAsync(null,
            Doc("test", "1", ("type", "boom")),
            Doc("test", "2", ("type", "linear")));

        var id = Assert.Single(ids);
        Assert.Equal("linear", (await h.GetAsync(id)).RouteKey);
    }
}
