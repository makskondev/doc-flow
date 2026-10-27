namespace DocFlow.Tests;

public class FileStorageTests
{
    private static Task Move(InMemoryFileStorage fs, string from, string to, bool overwriteDifferent = false)
        => fs.MoveIdempotentAsync(from, to, overwriteDifferent, CancellationToken.None);

    [Fact]
    public async Task Moves_the_file()
    {
        var fs = new InMemoryFileStorage();
        fs.Put("/a/x.pdf", "1");

        await Move(fs, "/a/x.pdf", "/b/x.pdf");

        Assert.False(fs.Has("/a/x.pdf"));
        Assert.Equal("1", fs.Text("/b/x.pdf"));
    }

    [Fact]
    public async Task Repeat_after_a_completed_move_does_nothing()
    {
        var fs = new InMemoryFileStorage();
        fs.Put("/b/x.pdf", "1");
        await Move(fs, "/a/x.pdf", "/b/x.pdf");                // источника нет, результат есть
        Assert.Equal("1", fs.Text("/b/x.pdf"));
    }

    [Fact]
    public async Task Neither_source_nor_destination_is_an_error()
    {
        var fs = new InMemoryFileStorage();
        await Assert.ThrowsAsync<FileNotFoundException>(() => Move(fs, "/a/x.pdf", "/b/x.pdf"));
    }

    [Fact]
    public async Task Both_exist_with_the_same_content_means_the_previous_move_was_unfinished()
    {
        var fs = new InMemoryFileStorage();
        fs.Put("/a/x.pdf", "same");
        fs.Put("/b/x.pdf", "same");

        await Move(fs, "/a/x.pdf", "/b/x.pdf");

        Assert.False(fs.Has("/a/x.pdf"));
        Assert.Equal("same", fs.Text("/b/x.pdf"));
    }

    [Fact]
    public async Task Both_exist_with_different_content_is_a_permanent_error_unless_overwriting_is_allowed()
    {
        var fs = new InMemoryFileStorage();
        fs.Put("/a/x.pdf", "mine");
        fs.Put("/b/x.pdf", "other");

        await Assert.ThrowsAsync<PermanentStepException>(() => Move(fs, "/a/x.pdf", "/b/x.pdf"));
        Assert.Equal("other", fs.Text("/b/x.pdf"));

        await Move(fs, "/a/x.pdf", "/b/x.pdf", overwriteDifferent: true);
        Assert.Equal("mine", fs.Text("/b/x.pdf"));
    }

    [Fact]
    public void Missing_directory_setting_is_a_permanent_error_that_names_the_type_and_setting()
    {
        var options = new StorageOptions();
        var ex = Assert.Throws<PermanentStepException>(() => options.Final("zop", "a.pdf"));
        Assert.Contains("zop", ex.Message);
        Assert.Contains("FinalDir", ex.Message);
    }
}
