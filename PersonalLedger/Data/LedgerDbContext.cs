
using Microsoft.EntityFrameworkCore;
using PersonalLedger.Models;

namespace PersonalLedger.Data;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AccountName)
                .IsRequired();

            entity.Property(a => a.Type)
                .HasConversion<string>();

            entity.Property(a => a.AccountNumber)
                .IsRequired();

            entity.Property(a => a.Institution)
                .IsRequired();

            // One account can have many transactions.
            entity.HasMany(a => a.Transactions)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Description)
                .IsRequired();

            entity.Property(t => t.CreditDebitIndicator)
                .HasConversion<string>();

            entity.Property(t => t.RewardCategory)
                .IsRequired(false);

            entity.HasIndex(t => new { t.AccountId, t.Date });
        });
    }
}
