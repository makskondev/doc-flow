namespace DocFlow.Engine;

/// <summary>
/// Создаёт процессы по найденным документам: определяет маршрут (правила IRoute.Matches), проверяет входные данные,
/// применяет правила дедупликации и повторной обработки.
/// </summary>
public sealed class DocumentRegistrar(
    IProcessRepository repo,
    RouteRegistry routes,
    IProcessAudit audit,
    TimeProvider clock,
    ILogger<DocumentRegistrar> log)
{
    private readonly object _gate = new();
    private readonly HashSet<string> _reported = new();   // чтобы одно и то же предупреждение не повторялось на каждом опросе

    /// <param name="pollStartedAtUtc">Момент начала опроса: данные источника — снимок на это время.</param>
    public async Task<IReadOnlyList<Guid>> RegisterAsync(
        IEnumerable<DocumentCandidate> batch, DateTime pollStartedAtUtc, CancellationToken ct)
    {
        var created = new List<Guid>();
        foreach (var d in batch)
        {
            var key = $"{d.SourceId}/{d.ExternalId}";

            var last = await repo.FindLatestAsync(d.SourceId, d.ExternalId, ct);
            int run;
            if (last is null) run = 1;
            else if (!last.IsTerminal) continue;                       // уже в работе (или ждёт оператора)
            else if (last.Status == ProcessStatus.Completed
                     && last.CompletedAt < pollStartedAtUtc) run = last.Run + 1;   // запись вернулась после завершения = переобработка
            else continue;                                             // Cancelled: только явный Restart; либо устаревший снимок

            var route = Classify(d, key);
            if (route is null) continue;

            // Неполные входные данные: процесс не создаём. Запись остаётся в источнике и подхватится, когда данные появятся.
            var missing = route.RequiredData
                .Where(k => string.IsNullOrWhiteSpace(d.Get(k)))
                .ToList();
            if (missing.Count > 0)
            {
                ReportOnce("missing:" + key, () => log.LogWarning(
                    "Документ {Doc} (маршрут {Route}) не зарегистрирован: во входных данных нет полей: {Fields}",
                    key, route.Key, string.Join(", ", missing)));
                continue;
            }
            Forget(key);

            var p = NewProcess(d, route, run);
            if (await repo.TryAddAsync(p, ct))
            {
                audit.Record(p.Id, p.CurrentNodeId, "Registered", $"{key} -> {route.Key}, run {run}");
                created.Add(p.Id);
            }
        }
        return created;
    }

    /// <summary>Оператор перезапускает отменённый процесс: новый Run с теми же исходными данными и тем же маршрутом.</summary>
    public async Task<Guid?> RestartAsync(Guid cancelledProcessId, CancellationToken ct)
    {
        var old = await repo.GetAsync(cancelledProcessId, ct);
        if (old is null || old.Status != ProcessStatus.Cancelled || !routes.TryGet(old.RouteKey, out var route))
            return null;

        var candidate = new DocumentCandidate(old.SourceId, old.ExternalId, old.ContentPath, old.Data);
        var p = NewProcess(candidate, route, old.Run + 1);
        if (!await repo.TryAddAsync(p, ct)) return null;
        audit.Record(p.Id, p.CurrentNodeId, "Registered", $"restart of {old.Id}");
        return p.Id;
    }

    /// <summary>Находит единственный подходящий маршрут. Ни одного или несколько — документ не регистрируется.</summary>
    private CompiledRoute? Classify(DocumentCandidate d, string key)
    {
        IReadOnlyList<CompiledRoute> matches;
        try
        {
            matches = routes.FindMatches(d);
        }
        catch (Exception ex)
        {
            ReportOnce("match-error:" + key, () => log.LogError(ex, "Ошибка в правиле Matches при распознавании документа {Doc}", key));
            return null;
        }

        switch (matches.Count)
        {
            case 1:
                return matches[0];
            case 0:
                ReportOnce("unmatched:" + key, () => log.LogWarning(
                    "Документ {Doc} не распознан: ни одно правило Matches не подошло (источник {Source})", key, d.SourceId));
                return null;
            default:
                ReportOnce("ambiguous:" + key, () => log.LogError(
                    "Документу {Doc} подходит несколько маршрутов: {Routes}. Уточните правила Matches",
                    key, string.Join(", ", matches.Select(r => r.Key))));
                return null;
        }
    }

    private void ReportOnce(string reportKey, Action report)
    {
        bool first;
        lock (_gate) first = _reported.Add(reportKey);
        if (first) report();
    }

    private void Forget(string key)
    {
        lock (_gate)
            foreach (var prefix in new[] { "missing:", "unmatched:", "ambiguous:", "match-error:" })
                _reported.Remove(prefix + key);
    }

    private DocumentProcess NewProcess(DocumentCandidate d, CompiledRoute route, int run)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new DocumentProcess
        {
            SourceId = d.SourceId,
            ExternalId = d.ExternalId,
            Run = run,
            RouteKey = route.Key,
            ContentPath = d.ContentPath ?? "",
            Data = d.Data is null ? new Dictionary<string, string>() : new Dictionary<string, string>(d.Data),
            RouteVersion = route.Version,
            CurrentNodeId = route.First.Id,
            Status = ProcessStatus.Running,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
