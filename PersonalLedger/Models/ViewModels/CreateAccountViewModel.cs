using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Models.ViewModels;

public class CreateAccountViewModel
{
    [Required(ErrorMessage = "Enter an account name.")]
    [StringLength(100)]
    public string? AccountName { get; set; }

    [Required(ErrorMessage = "Select an account type.")]
    public AccountType? Type { get; set; }

    [Required(ErrorMessage = "Enter an account number.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Account number must contain digits only.")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "Enter an institution.")]
    public string? Institution { get; set; }

    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Enter a valid non-negative amount with up to two decimal places.")]
    public string? Balance { get; set; }

    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Enter a valid non-negative amount with up to two decimal places.")]
    public string? AvailableCredit { get; set; }

    public IReadOnlyList<SelectListItem> AccountTypes { get; set; } = Array.Empty<SelectListItem>();
}
