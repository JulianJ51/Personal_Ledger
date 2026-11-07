
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Models;

public class Transaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public CreditDebitIndicator CreditDebitIndicator { get; set; }

    // Transaction amount, stored in cents.
    public long Amount { get; set; }

    // Institution-reported account balance after transaction.
    public long? AccountBalance { get; set; }

    // Optional transaction reward classification.
    public string? RewardCategory { get; set; }

    // Navigation property
    public Account Account { get; set; } = null!;
}
