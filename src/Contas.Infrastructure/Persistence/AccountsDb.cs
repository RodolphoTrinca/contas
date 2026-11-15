using Microsoft.EntityFrameworkCore;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountsDb(DbContextOptions<AccountsDb> options) : DbContext(options)
{
    internal DbSet<StoredAccount> Accounts => Set<StoredAccount>();
    internal DbSet<IdempotencyRecord> Receipts => Set<IdempotencyRecord>();
    internal DbSet<OutboxRecord> Outbox => Set<OutboxRecord>();
    internal DbSet<NotificationRecord> Inbox => Set<NotificationRecord>();

    internal DbSet<AccountAuditEntry> AuditEntries => Set<AccountAuditEntry>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var audit = model.Entity<AccountAuditEntry>();
        audit.ToTable("account_changes", "audit");
        audit.Property(row => row.Action).HasMaxLength(40);
        audit.HasIndex(row => new { row.AccountId, row.Id });

        model.Entity<StoredAccount>().HasIndex(row => row.Cpf).IsUnique()
            .HasDatabaseName("accounts_cpf_current").HasFilter("NOT \"Deleted\"");
        model.Entity<StoredAccount>().Property(row => row.Version).IsConcurrencyToken();
        model.Entity<StoredAccount>().Property(row => row.ManagerSubject).IsConcurrencyToken().HasMaxLength(36);
        model.Entity<StoredAccount>().Property(row => row.OwnerSubject).HasMaxLength(36).IsConcurrencyToken();
        model.Entity<StoredAccount>().HasIndex(row => row.ManagerSubject);
        model.Entity<StoredAccount>().HasIndex(row => row.OwnerSubject).IsUnique().HasFilter("NOT \"Deleted\"");
        model.Entity<IdempotencyRecord>().HasKey(row => new { row.Scope, row.Key });
        model.Entity<IdempotencyRecord>().HasIndex(row => row.ExpiresAt);
        model.Entity<OutboxRecord>().HasKey(row => row.Sequence);
        model.Entity<OutboxRecord>().HasIndex(row => row.EventId).IsUnique();
        model.Entity<NotificationRecord>().HasKey(row => new { row.Consumer, row.EventId });
    }
}
