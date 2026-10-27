using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DocFlow.Data;

/// <summary>
/// Модель БД. Доменный класс <see cref="DocumentProcess"/> отображается напрямую (без дублирующих сущностей),
/// всё отображение — здесь, в Fluent API, чтобы домен не зависел от EF.
/// </summary>
public sealed class DocFlowDbContext(DbContextOptions<DocFlowDbContext> options) : DbContext(options)
{
    public DbSet<DocumentProcess> Processes => Set<DocumentProcess>();
    public DbSet<ProcessEventEntity> Events => Set<ProcessEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bag — словарь для обмена данными между шагами: храним как JSON в text
        var bagConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        var bagComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
            v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
            v => new Dictionary<string, string>(v));

        modelBuilder.Entity<DocumentProcess>(e =>
        {
            e.ToTable("processes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();

            e.Property(x => x.SourceId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(256).IsRequired();
            e.Property(x => x.RouteKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.ContentPath).HasMaxLength(1024).IsRequired();
            e.Property(x => x.CurrentNodeId).HasMaxLength(128).IsRequired();
            e.Property(x => x.WaitingFor).HasMaxLength(128);
            e.Property(x => x.AttentionReason);                       // text: тексты исключений бывают длинными

            // Статус храним строкой: читаемо в SQL и устойчиво к перестановке значений enum
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.Bag).HasConversion(bagConverter, bagComparer).HasColumnType("text").IsRequired();
            // Входные данные документа (поля для штампа и т.п.) — тоже JSON в text. Содержат персональные данные.
            e.Property(x => x.Data).HasConversion(bagConverter, bagComparer).HasColumnType("text").IsRequired();

            // Optimistic lock: UPDATE ... WHERE "Version" = @ожидаемая
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Ignore(x => x.IsTerminal);

            // Один прогон документа = одна строка
            e.HasIndex(x => new { x.SourceId, x.ExternalId, x.Run })
                .IsUnique().HasDatabaseName("ux_processes_document_run");
            // Не более одного активного процесса на документ (защита от гонок регистрации)
            e.HasIndex(x => new { x.SourceId, x.ExternalId })
                .IsUnique().HasDatabaseName("ux_processes_active_document")
                .HasFilter("\"Status\" NOT IN ('Completed', 'Cancelled')");
            // Запросы раннера и сканеров
            e.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("ix_processes_status_next_attempt");
            e.HasIndex(x => new { x.Status, x.WaitingFor }).HasDatabaseName("ix_processes_status_waiting_for");
        });

        modelBuilder.Entity<ProcessEventEntity>(e =>
        {
            e.ToTable("process_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.NodeId).HasMaxLength(128).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(64).IsRequired();
            e.HasIndex(x => new { x.ProcessId, x.At }).HasDatabaseName("ix_process_events_process_at");
        });
    }
}
