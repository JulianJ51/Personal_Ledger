namespace PersonalLedger.Models.ViewModels;

public class TransactionQueryViewModel
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}