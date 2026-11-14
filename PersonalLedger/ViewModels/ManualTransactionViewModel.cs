using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.ViewModels;

public class ManualTransactionViewModel
{
    [Required(ErrorMessage = "Select an account.")]
    [Display(Name = "Account")]
    public int? AccountId { get; set; }

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a transaction date.")]
    [Display(Name = "Date")]
    public DateOnly? Date { get; set; }

    [Display(Name = "Credit/Debit")]
    public CreditDebitIndicator CreditDebitIndicator { get; set; }

    [Required(ErrorMessage = "Enter an amount.")]
    [RegularExpression(
        @"^\d+\.\d{2}$",
        ErrorMessage = "Enter an amount using two decimal places (for example, 125.50).")]
    [Display(Name = "Amount")]
    public string Amount { get; set; } = string.Empty;

    [Display(Name = "Reward Category")]
    public string? RewardCategory { get; set; }

    public List<SelectListItem> AccountOptions { get; set; } = [];
}
