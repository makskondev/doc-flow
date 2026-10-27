namespace DocFlow.Engine;

/// <summary>Проверка маршрутов. Вызывается при старте сервиса: ошибка конфигурации ловится сразу, а не в ночи на живом документе.</summary>
public static class RouteValidator
{
    public static IReadOnlyList<string> Validate(RouteRegistry routes, IEnumerable<ISignalSource> signalSources, IServiceProvider services)
    {
        var errors = new List<string>();
        var signals = signalSources.Select(s => s.Signal).ToHashSet();
        var isService = services.GetService<IServiceProviderIsService>();

        foreach (var route in routes.All)
        {
            if (route.Nodes.Count == 0)
            {
                errors.Add($"Маршрут '{route.Key}': нет ни одного узла");
                continue;
            }

            if (route.Nodes[^1] is WaitNode)
                errors.Add($"Маршрут '{route.Key}': последним узлом не может быть ожидание");

            foreach (var node in route.Nodes)
            {
                switch (node)
                {
                    case StepNode s when isService is not null && !isService.IsService(s.StepType):
                        errors.Add($"Маршрут '{route.Key}': шаг {s.StepType.Name} не зарегистрирован в DI");
                        break;
                    case WaitNode w when !signals.Contains(w.Signal):
                        errors.Add($"Маршрут '{route.Key}': для сигнала '{w.Signal}' нет ISignalSource (некому будить процесс)");
                        break;
                    case WaitNode w when w.Timeout <= TimeSpan.Zero:
                        errors.Add($"Маршрут '{route.Key}': у ожидания '{w.Signal}' не задан таймаут");
                        break;
                }
            }
        }
        return errors;
    }

    /// <summary>SourceId входит в ключ дедупликации: у двух источников он не может совпадать (иначе документы затрут друг друга).</summary>
    public static IReadOnlyList<string> ValidateSources(IEnumerable<IDocumentSource> sources)
        => sources
            .GroupBy(s => s.SourceId)
            .Where(g => g.Count() > 1)
            .Select(g => $"Источник документов '{g.Key}' зарегистрирован несколько раз: SourceId должен быть уникальным")
            .ToList();
}
