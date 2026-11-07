using PersonalLedger.Models.Enums;

namespace PersonalLedger.Models;

public class Account
{
    public int Id { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public string AccountNumber { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    // Current account balance, stored in cents.
    // For credit cards, this represents the amount owed.
    public long Balance { get; set; }

    // Current available credit, stored in cents.
    // Null for non-credit-card accounts.
    public long? AvailableCredit { get; set; }

    // Navigation property
    public ICollection<Transaction> Transactions { get; set; }
        = new List<Transaction>();
}
