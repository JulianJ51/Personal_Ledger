namespace PersonalLedger.Models.DTOs;

public class TransactionSummary
{
    // Amounts are stored in cents.
    public long TotalCredits { get; set; }

    public long TotalDebits { get; set; }

    public IReadOnlyList<RewardCategorySummary> RewardCategoryBreakdown
        { get; set; } = new List<RewardCategorySummary>();
}