namespace DocFlow.Tests;

/// <summary>Правила распознавания типа в настоящих маршрутах: каждому документу подходит ровно один маршрут.</summary>
public class RouteMatchingTests
{
    private static readonly RouteRegistry Routes =
        new(new IRoute[] { new ZopRoute(), new ReportRoute(), new InboxRoute(), new ArchiveRoute() });

    private static string[] Keys(DocumentCandidate c) => Routes.FindMatches(c).Select(r => r.Key).ToArray();

    private static DocumentCandidate View(string source, string kind)
        => new(source, "1", null, new Dictionary<string, string> { [DataKeys.DocKind] = kind });

    private static DocumentCandidate File(string directory, string fileName)
        => new(SourceIds.Directory, directory + "/" + fileName, "/p/" + fileName,
            new Dictionary<string, string> { [DataKeys.SourceDirectory] = directory, [DataKeys.FileName] = fileName });

    [Fact]
    public void One_view_serves_two_types_by_the_kind_column()
    {
        Assert.Equal(new[] { "zop" }, Keys(View(SourceIds.View1, "zop")));
        Assert.Equal(new[] { "archive" }, Keys(View(SourceIds.View1, "archive")));
        Assert.Empty(Keys(View(SourceIds.View1, "unknown-kind")));
    }

    [Fact]
    public void Another_view_selects_its_own_route()
    {
        Assert.Equal(new[] { "report" }, Keys(new DocumentCandidate(SourceIds.View2, "1", "/x")));
    }

    [Fact]
    public void Directory_documents_are_matched_by_directory_and_file_mask()
    {
        Assert.Equal(new[] { "inbox" }, Keys(File("inbox", "a.pdf")));
        Assert.Empty(Keys(File("other", "a.pdf")));        // другой каталог
        Assert.Empty(Keys(File("inbox", "a.txt")));        // не подходит под маску
    }

    [Theory]
    [InlineData("INV_1.PDF", "inv_*.pdf", true)]
    [InlineData("x.pdf", "inv_*.pdf", false)]
    [InlineData("a1.pdf", "a?.pdf", true)]
    [InlineData("a12.pdf", "a?.pdf", false)]
    [InlineData("a(1).pdf", "a(1).pdf", true)]             // спецсимволы regex не ломают маску
    public void File_masks(string name, string mask, bool expected)
        => Assert.Equal(expected, FileMask.IsMatch(name, mask));
}
