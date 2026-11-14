using PersonalLedger.Models;

namespace PersonalLedger.Services.Interfaces;

public interface IManualTransactionService
{
    Task<IReadOnlyList<Account>> GetAccountsAsync();

    Task<ManualTransactionResult> CreateAsync(
        int? accountId,
        string? description,
        DateOnly? date,
        string? creditDebitIndicator,
        string? amount,
        string? rewardCategory);
}

public sealed class ManualTransactionResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyDictionary<string, string> Errors { get; init; }
        = new Dictionary<string, string>();

    public static ManualTransactionResult Success() => new()
    {
        Succeeded = true
    };

    public static ManualTransactionResult Failure(
        string field,
        string message) => new()
    {
        Errors = new Dictionary<string, string>
        {
            [field] = message
        }
    };
}
