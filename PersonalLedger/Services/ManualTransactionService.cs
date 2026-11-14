using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Models.Enums;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Services;

public class ManualTransactionService : IManualTransactionService
{
    private static readonly Regex AmountPattern = new(
        @"^\d+\.\d{2}$",
        RegexOptions.Compiled);

    private readonly LedgerDbContext _context;

    public ManualTransactionService(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Account>> GetAccountsAsync()
    {
        return await _context.Accounts
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .ToListAsync();
    }

    public async Task<ManualTransactionResult> CreateAsync(
        int? accountId,
        string? description,
        DateOnly? date,
        string? creditDebitIndicator,
        string? amount,
        string? rewardCategory)
    {
        if (!accountId.HasValue)
        {
            return ManualTransactionResult.Failure(
                nameof(accountId), "Select an account.");
        }

        if (!date.HasValue)
        {
            return ManualTransactionResult.Failure(
                nameof(date), "Enter a valid transaction date.");
        }

        if (!Enum.TryParse<CreditDebitIndicator>(
                creditDebitIndicator,
                ignoreCase: true,
                out var indicator) ||
            !Enum.IsDefined(indicator))
        {
            return ManualTransactionResult.Failure(
                nameof(creditDebitIndicator),
                "Select a valid credit or debit indicator.");
        }

        if (string.IsNullOrWhiteSpace(amount) ||
            !AmountPattern.IsMatch(amount) ||
            !decimal.TryParse(
                amount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var parsedAmount) ||
            parsedAmount <= 0)
        {
            return ManualTransactionResult.Failure(
                nameof(amount),
                "Enter an amount greater than zero with exactly two decimal places.");
        }

        long amountInCents;
        try
        {
            amountInCents = checked((long)(parsedAmount * 100m));
        }
        catch (OverflowException)
        {
            return ManualTransactionResult.Failure(
                nameof(amount), "The entered amount is too large.");
        }

        var account = await _context.Accounts
            .SingleOrDefaultAsync(a => a.Id == accountId.Value);

        if (account is null)
        {
            return ManualTransactionResult.Failure(
                nameof(accountId), "The selected account could not be found.");
        }

        var increasesBalance = account.Type == AccountType.CreditCard
            ? indicator == CreditDebitIndicator.Debit
            : indicator == CreditDebitIndicator.Credit;

        try
        {
            account.Balance = checked(
                increasesBalance
                    ? account.Balance + amountInCents
                    : account.Balance - amountInCents);
        }
        catch (OverflowException)
        {
            return ManualTransactionResult.Failure(
                nameof(amount), "This transaction would exceed the supported account balance range.");
        }

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Description = description ?? string.Empty,
            Date = date.Value,
            CreditDebitIndicator = indicator,
            Amount = amountInCents,
            AccountBalance = account.Balance,
            RewardCategory = string.IsNullOrWhiteSpace(rewardCategory)
                ? null
                : rewardCategory.Trim()
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return ManualTransactionResult.Success();
    }
}
