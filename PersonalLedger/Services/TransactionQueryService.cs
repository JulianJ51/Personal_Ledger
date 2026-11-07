using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Services.Interfaces;

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
}