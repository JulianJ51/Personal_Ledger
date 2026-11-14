using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Models.Enums;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Services;

public class AccountService : IAccountService
{
    private static readonly Regex NumericStringPattern = new(@"^\d+$", RegexOptions.Compiled);
    private static readonly Regex MoneyPattern = new(@"^\d+(\.\d{1,2})?$", RegexOptions.Compiled);
    private readonly LedgerDbContext _context;

    public AccountService(LedgerDbContext context) => _context = context;

    public async Task<AccountCreationResult> CreateAsync(
        string? accountName,
        string? type,
        string? accountNumber,
        string? institution,
        string? balance,
        string? availableCredit)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            return AccountCreationResult.Failure("accountName", "Enter an account name.");

        if (!Enum.TryParse<AccountType>(type, true, out var accountType) ||
            !Enum.IsDefined(accountType))
            return AccountCreationResult.Failure("type", "Select a valid account type.");

        if (string.IsNullOrWhiteSpace(accountNumber) || !NumericStringPattern.IsMatch(accountNumber))
            return AccountCreationResult.Failure("accountNumber", "Account number must contain digits only.");

        if (string.IsNullOrWhiteSpace(institution) || !NumericStringPattern.IsMatch(institution))
            return AccountCreationResult.Failure("institution", "Institution must contain digits only.");

        if (!TryParseOptionalAmount(balance, out var balanceInCents))
            return AccountCreationResult.Failure("balance", "Enter a valid non-negative amount with up to two decimal places.");

        long? availableCreditInCents = null;
        if (accountType == AccountType.CreditCard &&
            !string.IsNullOrWhiteSpace(availableCredit))
        {
            if (!TryParseOptionalAmount(availableCredit, out var parsedCredit))
                return AccountCreationResult.Failure("availableCredit", "Enter a valid non-negative amount with up to two decimal places.");

            availableCreditInCents = parsedCredit;
        }

        var account = new Account
        {
            AccountName = accountName.Trim(),
            Type = accountType,
            AccountNumber = accountNumber,
            Institution = institution,
            Balance = balanceInCents ?? 0,
            AvailableCredit = accountType == AccountType.CreditCard
                ? availableCreditInCents
                : null
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return AccountCreationResult.Success();
    }

    private static bool TryParseOptionalAmount(string? value, out long? amountInCents)
    {
        amountInCents = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        if (!MoneyPattern.IsMatch(value) ||
            !decimal.TryParse(value, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount))
            return false;

        try
        {
            amountInCents = checked((long)(amount * 100m));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
