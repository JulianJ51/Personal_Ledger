using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalLedger.Models.Enums;
using PersonalLedger.Models.ViewModels;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Create()
    {
        var viewModel = new CreateAccountViewModel();
        PopulateAccountTypes(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountViewModel viewModel)
    {
        var result = await _accountService.CreateAsync(
            viewModel.AccountName,
            viewModel.Type?.ToString(),
            viewModel.AccountNumber,
            viewModel.Institution,
            viewModel.Balance,
            viewModel.AvailableCredit);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var field = error.Key switch
                {
                    "accountName" => nameof(viewModel.AccountName),
                    "type" => nameof(viewModel.Type),
                    "accountNumber" => nameof(viewModel.AccountNumber),
                    "institution" => nameof(viewModel.Institution),
                    "balance" => nameof(viewModel.Balance),
                    "availableCredit" => nameof(viewModel.AvailableCredit),
                    _ => string.Empty
                };
                ModelState.AddModelError(field, error.Value);
            }

            PopulateAccountTypes(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Account created successfully.";
        return RedirectToAction(nameof(Create));
    }

    private static void PopulateAccountTypes(CreateAccountViewModel viewModel)
    {
        viewModel.AccountTypes = Enum.GetValues<AccountType>()
            .Select(type => new SelectListItem
            {
                Value = type.ToString(),
                Text = type.ToString()
            }).ToList();
    }
}
