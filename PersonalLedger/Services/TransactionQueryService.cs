using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Services.Interfaces;
using PersonalLedger.Models.Enums;
using PersonalLedger.Models.DTOs;

namespace PersonalLedger.Services;

public class TransactionQueryService : ITransactionQueryService
{
    private readonly LedgerDbContext _context;

    public TransactionQueryService(LedgerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public TransactionSummary GetTransactionSummary(
    IReadOnlyCollection<Transaction> transactions)
{
    ArgumentNullException.ThrowIfNull(transactions);

    var rewardCategoryBreakdown = transactions
        .Where(t =>
            t.Account.Type == AccountType.CreditCard &&
            t.CreditDebitIndicator == CreditDebitIndicator.Debit &&
            !string.IsNullOrWhiteSpace(t.RewardCategory))
        .GroupBy(t => t.RewardCategory!)
        .Select(g => new RewardCategorySummary
        {
            RewardCategory = g.Key,
            TotalSpend = g.Sum(t => t.Amount)
        })
        .OrderByDescending(g => g.TotalSpend)
        .ToList();

    return new TransactionSummary
    {
        TotalCredits = transactions
            .Where(t => t.CreditDebitIndicator == CreditDebitIndicator.Credit)
            .Sum(t => t.Amount),

        TotalDebits = transactions
            .Where(t => t.CreditDebitIndicator == CreditDebitIndicator.Debit)
            .Sum(t => t.Amount),

        RewardCategoryBreakdown = rewardCategoryBreakdown
    };
}
}