using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Models.ViewModels;

public class TransactionDetailsViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Select an account.")]
    public int? AccountId { get; set; }

    public IReadOnlyList<SelectListItem> Accounts { get; set; } = Array.Empty<SelectListItem>();
    public string? Description { get; set; }

    [Required(ErrorMessage = "Enter a transaction date.")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Select credit or debit.")]
    public CreditDebitIndicator? CreditDebitIndicator { get; set; }

    [Required(ErrorMessage = "Enter an amount.")]
    [RegularExpression(@"^\d+\.\d{2}$",
        ErrorMessage = "Enter an amount with exactly two decimal places (for example, 12.34).")]
    public string? Amount { get; set; }

    [RegularExpression(@"^$|^\d+\.\d{2}$",
        ErrorMessage = "Enter an account balance with exactly two decimal places, or leave it blank.")]
    public string? AccountBalance { get; set; }

    public string? RewardCategory { get; set; }
}
