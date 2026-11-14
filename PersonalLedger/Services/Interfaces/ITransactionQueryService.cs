using PersonalLedger.Models;
using PersonalLedger.Models.DTOs;

namespace PersonalLedger.Services.Interfaces;

public interface ITransactionQueryService
{
    Task<IReadOnlyList<Transaction>> GetTransactionsByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate);

    TransactionSummary GetTransactionSummary(
        IReadOnlyCollection<Transaction> transactions);
}