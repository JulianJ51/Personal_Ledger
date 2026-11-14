using PersonalLedger.Models;

namespace PersonalLedger.Services.Interfaces;

public interface IAccountService
{
    Task<AccountCreationResult> CreateAsync(
        string? accountName,
        string? type,
        string? accountNumber,
        string? institution,
        string? balance,
        string? availableCredit);
}

public sealed class AccountCreationResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyDictionary<string, string> Errors { get; init; }
        = new Dictionary<string, string>();

    public static AccountCreationResult Success() => new() { Succeeded = true };

    public static AccountCreationResult Failure(string field, string message) => new()
    {
        Errors = new Dictionary<string, string> { [field] = message }
    };
}
