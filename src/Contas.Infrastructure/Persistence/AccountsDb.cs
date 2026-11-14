using Microsoft.EntityFrameworkCore;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountsDb(DbContextOptions<AccountsDb> options) : DbContext(options)
{
    internal DbSet<StoredAccount> Accounts => Set<StoredAccount>();
    internal DbSet<IdempotencyRecord> Receipts => Set<IdempotencyRecord>();
    internal DbSet<OutboxRecord> Outbox => Set<OutboxRecord>();
    internal DbSet<NotificationRecord> Inbox => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<StoredAccount>().HasIndex(row => row.Cpf).IsUnique()
            .HasDatabaseName("accounts_cpf_current").HasFilter("NOT \"Deleted\"");
        model.Entity<StoredAccount>().Property(row => row.Version).IsConcurrencyToken();
        model.Entity<IdempotencyRecord>().HasKey(row => new { row.Scope, row.Key });
        model.Entity<IdempotencyRecord>().HasIndex(row => row.ExpiresAt);
        model.Entity<OutboxRecord>().HasKey(row => row.Sequence);
        model.Entity<OutboxRecord>().HasIndex(row => row.EventId).IsUnique();
        model.Entity<NotificationRecord>().HasKey(row => new { row.Consumer, row.EventId });
    }
}
