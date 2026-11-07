using PersonalLedger.Models.Enums;

namespace PersonalLedger.Services.Import;

public class ImportedTransaction
{
    public string AccountNumber { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public CreditDebitIndicator CreditDebitIndicator { get; set; }

    // Amount in cents.
    public long Amount { get; set; }

    // Institution-reported balance in cents.
    public long? AccountBalance { get; set; }
}