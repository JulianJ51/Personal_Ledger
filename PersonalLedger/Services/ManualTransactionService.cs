using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Models.DTOs;
using PersonalLedger.Models.Enums;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Services;

public class ManualTransactionService : IManualTransactionService
{
    private static readonly Regex AmountPattern = new(@"^\d+\.\d{2}$", RegexOptions.Compiled);
    private readonly LedgerDbContext _context;

    public ManualTransactionService(LedgerDbContext context) => _context = context;

    public async Task<IReadOnlyList<Account>> GetAccountsAsync() =>
        await _context.Accounts.AsNoTracking()
            .OrderBy(account => account.AccountName).ToListAsync();

    public async Task<Transaction?> GetTransactionAsync(int id) =>
        await _context.Transactions
            .Include(transaction => transaction.Account)
            .SingleOrDefaultAsync(transaction => transaction.Id == id);

    public async Task<ManualTransactionResult> CreateAsync(
        int? accountId, string? description, DateOnly? date,
        string? creditDebitIndicator, string? amount, string? rewardCategory)
    {
        var validation = await ValidateAsync(accountId, date, creditDebitIndicator, amount);
        if (validation.Error is not null) return validation.Error;

        var (account, indicator, amountInCents) = validation.Value!.Value;
        var increasesBalance = account.Type == AccountType.CreditCard
            ? indicator == CreditDebitIndicator.Debit
            : indicator == CreditDebitIndicator.Credit;

        try
        {
            account.Balance = checked(increasesBalance
                ? account.Balance + amountInCents
                : account.Balance - amountInCents);
        }
        catch (OverflowException)
        {
            return ManualTransactionResult.Failure(
                "amount", "This transaction would exceed the supported account balance range.");
        }

        _context.Transactions.Add(new Transaction
        {
            AccountId = account.Id,
            Description = description ?? string.Empty,
            Date = date!.Value,
            CreditDebitIndicator = indicator,
            Amount = amountInCents,
            AccountBalance = account.Balance,
            RewardCategory = NormalizeCategory(rewardCategory)
        });

        await _context.SaveChangesAsync();
        return ManualTransactionResult.Success();
    }

    public async Task<ManualTransactionResult> UpdateAsync(
        int id, UpdateManualTransactionRequest request)
    {
        var transaction = await _context.Transactions
            .SingleOrDefaultAsync(item => item.Id == id);

        if (transaction is null)
            return ManualTransactionResult.Failure(string.Empty, "The transaction could not be found.");

        var validation = await ValidateAsync(
            request.AccountId,
            request.Date,
            request.CreditDebitIndicator,
            request.Amount);

        if (validation.Error is not null) return validation.Error;

        var (account, indicator, amountInCents) = validation.Value!.Value;

        long? balanceInCents = null;
        if (!string.IsNullOrWhiteSpace(request.AccountBalance))
        {
            if (!AmountPattern.IsMatch(request.AccountBalance) ||
                !decimal.TryParse(request.AccountBalance, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var parsedBalance) ||
                parsedBalance < 0)
            {
                return ManualTransactionResult.Failure(
                    "accountBalance", "Enter a valid account balance with exactly two decimal places, or leave it blank.");
            }

            try { balanceInCents = checked((long)(parsedBalance * 100m)); }
            catch (OverflowException)
            {
                return ManualTransactionResult.Failure("accountBalance", "The entered account balance is too large.");
            }
        }

        transaction.AccountId = account.Id;
        transaction.Description = request.Description ?? string.Empty;
        transaction.Date = request.Date!.Value;
        transaction.CreditDebitIndicator = indicator;
        transaction.Amount = amountInCents;
        transaction.AccountBalance = balanceInCents;
        transaction.RewardCategory = NormalizeCategory(request.RewardCategory);

        await _context.SaveChangesAsync();
        return ManualTransactionResult.Success();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _context.Transactions
            .SingleOrDefaultAsync(item => item.Id == id);

        if (transaction is null) return false;

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<(ManualTransactionResult? Error, (Account Value, CreditDebitIndicator Indicator, long Amount)? Value)>
        ValidateAsync(int? accountId, DateOnly? date, string? creditDebitIndicator, string? amount)
    {
        if (!accountId.HasValue)
            return (ManualTransactionResult.Failure("accountId", "Select an account."), null);
        if (!date.HasValue)
            return (ManualTransactionResult.Failure("date", "Enter a valid transaction date."), null);
        if (!Enum.TryParse<CreditDebitIndicator>(creditDebitIndicator, true, out var indicator) ||
            !Enum.IsDefined(indicator))
            return (ManualTransactionResult.Failure("creditDebitIndicator",
                "Select a valid credit or debit indicator."), null);
        if (string.IsNullOrWhiteSpace(amount) || !AmountPattern.IsMatch(amount) ||
            !decimal.TryParse(amount, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var parsedAmount) || parsedAmount <= 0)
            return (ManualTransactionResult.Failure("amount",
                "Enter an amount greater than zero with exactly two decimal places."), null);

        long amountInCents;
        try { amountInCents = checked((long)(parsedAmount * 100m)); }
        catch (OverflowException)
        {
            return (ManualTransactionResult.Failure("amount", "The entered amount is too large."), null);
        }

        var account = await _context.Accounts.SingleOrDefaultAsync(a => a.Id == accountId.Value);
        if (account is null)
            return (ManualTransactionResult.Failure("accountId",
                "The selected account could not be found."), null);

        return (null, (account, indicator, amountInCents));
    }

    private static string? NormalizeCategory(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
