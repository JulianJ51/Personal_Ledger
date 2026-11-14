namespace PersonalLedger.Models.DTOs;

public class RewardCategorySummary
{
    public string RewardCategory { get; set; } = string.Empty;

    // Amount stored in cents.
    public long TotalSpend { get; set; }
}