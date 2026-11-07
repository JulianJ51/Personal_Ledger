using PersonalLedger.Models;

namespace PersonalLedger.ViewModels;
public class TransactionListViewModel
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public IReadOnlyList<Transaction> Transactions { get; set; }
        = new List<Transaction>();
}