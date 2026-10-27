namespace DocFlow.Data;

public sealed class DatabaseOptions
{
    /// <summary>Применять миграции при старте сервиса. Для продакшена с отдельным процессом деплоя миграций — выключите.</summary>
    public bool MigrateOnStartup { get; set; } = true;
}
