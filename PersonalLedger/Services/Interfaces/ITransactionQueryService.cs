using PersonalLedger.Models;

namespace PersonalLedger.Services.Interfaces;

public interface ITransactionQueryService
{
    Task<IReadOnlyList<Transaction>> GetTransactionsByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate);
}