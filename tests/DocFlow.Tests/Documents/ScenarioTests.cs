namespace DocFlow.Tests;

/// <summary>Сквозные тесты по четырём сценариям постановки на настоящих маршрутах и шагах.</summary>
public class ScenarioTests
{
    private static Dictionary<string, string> Zop() => new()
    {
        [DataKeys.DocKind] = DocumentTypes.Zop,
        [DataKeys.FullName] = "Иванов И.И.",
        [DataKeys.AgreementNumber] = "42/7",
        [DataKeys.AgreementDate] = "2026-03-01",
    };

    [Fact]
    public async Task Scenario1_view_1_zop_waits_for_file_and_signature_then_completes()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/src/z.pdf", ScenarioHarness.Pdf("zop"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "9", "/src/z.pdf", Zop()));

        await h.AdvanceAsync(id);

        // файл переложен (не скопирован) в папку для подписи; процесс ждёт результат подписания
        var parked = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.Waiting, parked.Status);
        Assert.Equal(Signals.SigningResult, parked.WaitingFor);
        var sent = h.Storage.SignInput("zop", "z.pdf");      // исходное имя, без идентификатора
        Assert.True(h.Files.Has(sent));
        Assert.False(h.Files.Has(h.Storage.Work(id, ".pdf")));

        // «система подписи» кладёт в директорию результата файл и подпись
        var bytes = h.Files.Bytes(sent);
        h.Files.Put(h.Storage.SignResultFile("zop", "z.pdf"), bytes);
        h.Files.Put(h.Storage.SignResultSignature("zop", "z.pdf"), System.Text.Encoding.UTF8.GetBytes("SIG:" + Hashing.Sha256Hex(bytes)));

        await h.Scanner.ScanAsync(CancellationToken.None);     // первый скан: файлы ещё могли дописываться
        Assert.Equal(ProcessStatus.Waiting, (await h.GetAsync(id)).Status);
        await h.Scanner.ScanAsync(CancellationToken.None);     // второй скан: файлы не менялись — готово
        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.NotNull(h.Db.Completed["9"]);                    // подпись и отметка во внешней БД
        Assert.True(h.Files.Has("/zop/final/z.pdf"));
        Assert.True(h.Files.Has("/zop/final/z.pdf.sig"));
        Assert.DoesNotContain(h.Files.All(), f => f.StartsWith("/work/"));
    }

    [Fact]
    public async Task Scenario1_signature_alone_without_the_signed_file_is_not_enough()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/src/z.pdf", ScenarioHarness.Pdf("zop"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "9", "/src/z.pdf", Zop()));
        await h.AdvanceAsync(id);

        h.Files.Put(h.Storage.SignResultSignature("zop", "z.pdf"), "SIG:whatever");
        await h.Scanner.ScanAsync(CancellationToken.None);
        await h.Scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(ProcessStatus.Waiting, (await h.GetAsync(id)).Status);
    }

    [Fact]
    public async Task Scenario2_view_2_is_signed_automatically()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/src/a.pdf", ScenarioHarness.Pdf("report"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View2, "7", "/src/a.pdf"));

        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.Equal("report", (await h.GetAsync(id)).RouteKey);
        Assert.NotNull(h.Db.Completed["7"]);
        Assert.True(h.Files.Has("/report/final/a.pdf"));
        Assert.True(h.Files.Has("/report/final/a.pdf.sig"));
        Assert.DoesNotContain(h.Files.All(), f => f.StartsWith("/work/"));
    }

    [Fact]
    public async Task Scenario3_file_in_directory_is_registered_in_the_external_db_then_signed()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/in/inbox/b.pdf", ScenarioHarness.Pdf("inbox"));
        var candidate = new DocumentCandidate(SourceIds.Directory, "inbox/b", "/in/inbox/b.pdf",
            new Dictionary<string, string> { [DataKeys.SourceDirectory] = "inbox", [DataKeys.FileName] = "b.pdf" });
        var id = await h.RegisterAsync(candidate);

        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        var externalId = h.Db.Registered[id];                   // идентификатор выдала внешняя БД
        Assert.NotNull(h.Db.Completed[externalId]);             // подпись и отметка — под этим идентификатором
        Assert.False(h.Db.Completed.ContainsKey("inbox/b"));
        Assert.True(h.Files.Has("/inbox/final/b.pdf"));
        Assert.True(h.Files.Has("/inbox/final/b.pdf.sig"));
    }

    [Fact]
    public async Task Scenario4_view_1_archive_finds_the_file_marks_it_and_moves_it_without_a_signature()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/archive/search/42.pdf", ScenarioHarness.Pdf("archive"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "42", null,
            new Dictionary<string, string> { [DataKeys.DocKind] = DocumentTypes.Archive }));

        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.Equal("archive", (await h.GetAsync(id)).RouteKey);
        Assert.Null(h.Db.Completed["42"]);                      // только отметка, подписи нет
        Assert.True(h.Files.Has("/archive/final/42.pdf"));
        Assert.False(h.Files.Has("/archive/search/42.pdf"));
        Assert.DoesNotContain(h.Files.All(), f => f.EndsWith(".sig"));
    }

    [Fact]
    public async Task Scenario4_file_that_has_not_arrived_yet_is_retried_then_found()
    {
        var h = new ScenarioHarness();
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "42", null,
            new Dictionary<string, string> { [DataKeys.DocKind] = DocumentTypes.Archive }));

        await h.AdvanceAsync(id);
        Assert.Equal(ProcessStatus.Running, (await h.GetAsync(id)).Status);       // файла нет: повтор с backoff

        h.Files.Put("/archive/search/42.pdf", ScenarioHarness.Pdf("archive"));
        h.Clock.Advance(TimeSpan.FromSeconds(11));
        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
    }

    [Fact]
    public async Task A_different_file_with_the_same_name_in_the_final_directory_escalates_instead_of_overwriting()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/archive/search/42.pdf", ScenarioHarness.Pdf("new"));
        h.Files.Put("/archive/final/42.pdf", ScenarioHarness.Pdf("someone else's"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "42", null,
            new Dictionary<string, string> { [DataKeys.DocKind] = DocumentTypes.Archive }));

        await h.AdvanceAsync(id);

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.NeedsAttention, p.Status);
        Assert.Contains("другой файл", p.AttentionReason);
        Assert.Equal(ScenarioHarness.Pdf("someone else's"), h.Files.Text("/archive/final/42.pdf"));   // чужой файл не тронут
    }

    [Fact]
    public async Task A_type_without_configured_directories_escalates_with_a_clear_reason()
    {
        var h = new ScenarioHarness();
        h.Storage.Types.Remove("archive");
        h.Files.Put("/archive/search/42.pdf", ScenarioHarness.Pdf("archive"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "42", null,
            new Dictionary<string, string> { [DataKeys.DocKind] = DocumentTypes.Archive }));

        await h.AdvanceAsync(id);

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.NeedsAttention, p.Status);
        Assert.Contains("SearchDir", p.AttentionReason);
    }

    // ---- имена файлов: пользователи видят исходные имена ----

    private static async Task<(ScenarioHarness H, Guid Id)> ZopWaitingForSignatureAsync(Action<ScenarioHarness>? arrange = null)
    {
        var h = new ScenarioHarness();
        arrange?.Invoke(h);
        h.Files.Put("/src/z.pdf", ScenarioHarness.Pdf("zop"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "9", "/src/z.pdf", Zop()));
        await h.AdvanceAsync(id);
        return (h, id);
    }

    private static async Task SignAsync(ScenarioHarness h, byte[] signedBytes)
    {
        // «система подписания» забирает файл из папки для подписи и кладёт результат в директорию результата
        await h.Files.DeleteAsync(h.Storage.SignInput("zop", "z.pdf"), CancellationToken.None);
        h.Files.Put(h.Storage.SignResultFile("zop", "z.pdf"), signedBytes);
        h.Files.Put(h.Storage.SignResultSignature("zop", "z.pdf"),
            System.Text.Encoding.UTF8.GetBytes("SIG:" + Hashing.Sha256Hex(signedBytes)));
        await h.Scanner.ScanAsync(CancellationToken.None);
        await h.Scanner.ScanAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Users_see_original_file_names_in_every_user_facing_directory()
    {
        var (h, id) = await ZopWaitingForSignatureAsync();
        Assert.Equal(new[] { "/zop/in/z.pdf" }, h.Files.All().Where(f => f.StartsWith("/zop/")));   // в папке для подписи — исходное имя

        await SignAsync(h, h.Files.Bytes("/zop/in/z.pdf"));
        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.Equal(new[] { "/zop/final/z.pdf", "/zop/final/z.pdf.sig" }, h.Files.All().Where(f => f.StartsWith("/zop/")));
        Assert.DoesNotContain(h.Files.All(), f => f.Contains(id.ToString()));           // идентификатор процесса нигде не виден
    }

    [Fact]
    public async Task Signature_file_name_follows_the_configured_convention()
    {
        var (h, id) = await ZopWaitingForSignatureAsync(x => x.Storage.SignatureNaming = SignatureNaming.ReplaceExtension);

        await SignAsync(h, h.Files.Bytes("/zop/in/z.pdf"));    // подпись ложится как «z.sig»
        Assert.True(h.Files.Has("/zop/out/z.sig"));
        await h.AdvanceAsync(id);

        Assert.Equal(ProcessStatus.Completed, (await h.GetAsync(id)).Status);
        Assert.True(h.Files.Has("/zop/final/z.pdf"));
        Assert.True(h.Files.Has("/zop/final/z.sig"));
    }

    [Fact]
    public async Task Another_document_with_the_same_name_already_waiting_for_signature_is_not_overwritten()
    {
        var h = new ScenarioHarness();
        h.Files.Put("/zop/in/z.pdf", ScenarioHarness.Pdf("другой документ, ждёт подписи"));
        h.Files.Put("/src/z.pdf", ScenarioHarness.Pdf("zop"));
        var id = await h.RegisterAsync(new DocumentCandidate(SourceIds.View1, "9", "/src/z.pdf", Zop()));

        await h.AdvanceAsync(id);

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.NeedsAttention, p.Status);
        Assert.Contains("другой файл", p.AttentionReason);
        Assert.Equal(ScenarioHarness.Pdf("другой документ, ждёт подписи"), h.Files.Text("/zop/in/z.pdf"));
    }

    [Fact]
    public async Task A_stale_result_with_the_same_name_but_different_content_fails_verification()
    {
        var (h, id) = await ZopWaitingForSignatureAsync();

        // в директории результата лежит «чужой» файл с тем же именем и своей валидной подписью
        await SignAsync(h, System.Text.Encoding.UTF8.GetBytes(ScenarioHarness.Pdf("устаревший результат")));
        await h.AdvanceAsync(id);

        var p = await h.GetAsync(id);
        Assert.Equal(ProcessStatus.NeedsAttention, p.Status);
        Assert.Contains("отличается", p.AttentionReason);
    }
}
