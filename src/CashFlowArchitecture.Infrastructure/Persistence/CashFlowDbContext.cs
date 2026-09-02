using CashFlowArchitecture.Core.Domain.Entries;
using Microsoft.EntityFrameworkCore;

namespace CashFlowArchitecture.Infrastructure.Persistence;

public sealed class CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : DbContext(options)
{
    public DbSet<FinancialEntryEntity> FinancialEntries => Set<FinancialEntryEntity>();

    public DbSet<DailyBalanceEntity> DailyBalances => Set<DailyBalanceEntity>();

    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinancialEntryEntity>(entry =>
        {
            entry.ToTable("financial_entries");
            entry.HasKey(entity => entity.Id);
            entry.HasIndex(entity => entity.Uid).IsUnique();
            entry.HasIndex(entity => entity.EntryDate);

            entry.Property(entity => entity.Id).HasColumnName("id");
            entry.Property(entity => entity.Uid).HasColumnName("uid").IsRequired();
            entry.Property(entity => entity.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();
            entry.Property(entity => entity.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entry.Property(entity => entity.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
            entry.Property(entity => entity.EntryDate).HasColumnName("entry_date").IsRequired();
            entry.Property(entity => entity.CreatedAt).HasColumnName("created_at").IsRequired();
        });

        modelBuilder.Entity<DailyBalanceEntity>(balance =>
        {
            balance.ToTable("daily_balances");
            balance.HasKey(entity => entity.Id);
            balance.HasIndex(entity => entity.Uid).IsUnique();
            balance.HasIndex(entity => entity.BalanceDate).IsUnique();

            balance.Property(entity => entity.Id).HasColumnName("id");
            balance.Property(entity => entity.Uid).HasColumnName("uid").IsRequired();
            balance.Property(entity => entity.BalanceDate).HasColumnName("balance_date").IsRequired();
            balance.Property(entity => entity.TotalCredits).HasColumnName("total_credits").HasPrecision(18, 2);
            balance.Property(entity => entity.TotalDebits).HasColumnName("total_debits").HasPrecision(18, 2);
            balance.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            balance.Property(entity => entity.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<DailyBalanceProcessedEventEntity>(processedEvent =>
        {
            processedEvent.ToTable("daily_balance_processed_events");
            processedEvent.HasKey(entity => entity.Id);
            processedEvent.HasIndex(entity => entity.EventUid).IsUnique();

            processedEvent.Property(entity => entity.Id).HasColumnName("id");
            processedEvent.Property(entity => entity.DailyBalanceId).HasColumnName("daily_balance_id");
            processedEvent.Property(entity => entity.EventUid).HasColumnName("event_uid").IsRequired();
            processedEvent.HasOne(entity => entity.DailyBalance)
                .WithMany(entity => entity.ProcessedEvents)
                .HasForeignKey(entity => entity.DailyBalanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdempotencyRecordEntity>(record =>
        {
            record.ToTable("idempotency_records");
            record.HasKey(entity => entity.Id);
            record.HasIndex(entity => new { entity.Operation, entity.Key }).IsUnique();
            record.HasIndex(entity => entity.ResourceUid);
            record.HasIndex(entity => entity.ExpiresAt);

            record.Property(entity => entity.Id).HasColumnName("id");
            record.Property(entity => entity.Operation).HasColumnName("operation").HasMaxLength(100).IsRequired();
            record.Property(entity => entity.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
            record.Property(entity => entity.RequestHash).HasColumnName("request_hash").HasMaxLength(128).IsRequired();
            record.Property(entity => entity.ResourceUid).HasColumnName("resource_uid").IsRequired();
            record.Property(entity => entity.CreatedAt).HasColumnName("created_at").IsRequired();
            record.Property(entity => entity.ExpiresAt).HasColumnName("expires_at").IsRequired();
        });

        modelBuilder.Entity<OutboxMessageEntity>(message =>
        {
            message.ToTable("outbox_messages");
            message.HasKey(entity => entity.Id);
            message.HasIndex(entity => entity.EventUid).IsUnique();
            message.HasIndex(entity => entity.ProcessedAt);
            message.HasIndex(entity => entity.NextAttemptAt);
            message.HasIndex(entity => entity.FailedAt);
            message.HasIndex(entity => entity.CreatedAt);

            message.Property(entity => entity.Id).HasColumnName("id");
            message.Property(entity => entity.EventUid).HasColumnName("event_uid").IsRequired();
            message.Property(entity => entity.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
            message.Property(entity => entity.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100).IsRequired();
            message.Property(entity => entity.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            message.Property(entity => entity.OccurredAt).HasColumnName("occurred_at").IsRequired();
            message.Property(entity => entity.CreatedAt).HasColumnName("created_at").IsRequired();
            message.Property(entity => entity.ProcessedAt).HasColumnName("processed_at");
            message.Property(entity => entity.NextAttemptAt).HasColumnName("next_attempt_at");
            message.Property(entity => entity.FailedAt).HasColumnName("failed_at");
            message.Property(entity => entity.RetryCount).HasColumnName("retry_count");
            message.Property(entity => entity.LastError).HasColumnName("last_error").HasMaxLength(1000);
        });
    }
}

public sealed class FinancialEntryEntity
{
    public long Id { get; set; }

    public Guid Uid { get; set; }

    public EntryType Type { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateOnly EntryDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class DailyBalanceEntity
{
    public long Id { get; set; }

    public Guid Uid { get; set; }

    public DateOnly BalanceDate { get; set; }

    public decimal TotalCredits { get; set; }

    public decimal TotalDebits { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public List<DailyBalanceProcessedEventEntity> ProcessedEvents { get; set; } = [];
}

public sealed class DailyBalanceProcessedEventEntity
{
    public long Id { get; set; }

    public long DailyBalanceId { get; set; }

    public Guid EventUid { get; set; }

    public DailyBalanceEntity DailyBalance { get; set; } = null!;
}

public sealed class IdempotencyRecordEntity
{
    public long Id { get; set; }

    public string Operation { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public Guid ResourceUid { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class OutboxMessageEntity
{
    public long Id { get; set; }

    public Guid EventUid { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
}
