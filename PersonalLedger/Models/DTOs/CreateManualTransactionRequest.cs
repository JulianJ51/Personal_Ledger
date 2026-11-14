namespace PersonalLedger.Models.DTOs;

public sealed class CreateManualTransactionRequest
{
    public int? AccountId { get; init; }

    public string? Description { get; init; }

    public DateOnly? Date { get; init; }

    public string? CreditDebitIndicator { get; init; }

    public string? Amount { get; init; }

    public string? RewardCategory { get; init; }
}
