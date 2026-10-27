namespace DocFlow.Tests;

public class ViewDocumentSourceTests
{
    private sealed class FakeReader(string sourceId, params PendingDocument[] rows) : IPendingDocumentsReader
    {
        public string SourceId => sourceId;
        public Task<IReadOnlyList<PendingDocument>> ReadAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<PendingDocument>>(rows);
    }

    [Fact]
    public async Task Rows_become_candidates_without_a_document_type()
    {
        var reader = new FakeReader("view_1",
            new PendingDocument("42", "/f.pdf", new Dictionary<string, string> { ["DocKind"] = "zop" }),
            new PendingDocument("43"));                                    // путь и данные необязательны

        var docs = await new ViewDocumentSource(reader).PollAsync(CancellationToken.None);

        Assert.Equal(2, docs.Count);
        Assert.Equal(("view_1", "42", "/f.pdf"), (docs[0].SourceId, docs[0].ExternalId, docs[0].ContentPath));
        Assert.Equal("zop", docs[0].Get("DocKind"));
        Assert.Null(docs[1].ContentPath);
        Assert.Null(docs[1].Get("DocKind"));
    }

    [Fact]
    public async Task Same_document_id_in_different_views_does_not_collide()
    {
        var h = new Harness();
        var kind = new Dictionary<string, string> { ["type"] = "linear" };
        var a = new ViewDocumentSource(new FakeReader("view:a", new PendingDocument("1", "/a.pdf", kind)));
        var b = new ViewDocumentSource(new FakeReader("view:b", new PendingDocument("1", "/b.pdf", kind)));

        var all = (await a.PollAsync(CancellationToken.None)).Concat(await b.PollAsync(CancellationToken.None)).ToArray();
        var created = await h.RegisterCandidatesAsync(null, all);

        Assert.Equal(2, created.Count);
    }
}
