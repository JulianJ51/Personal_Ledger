using PersonalLedger.Models;
using PersonalLedger.Models.DTOs;


namespace PersonalLedger.Models.ViewModels;
public class TransactionListViewModel
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public IReadOnlyList<Transaction> Transactions { get; set; }
        = new List<Transaction>();

    public TransactionSummary Summary { get; set; } = new();
}