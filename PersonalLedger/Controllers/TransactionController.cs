using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalLedger.Models.ViewModels;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Controllers;

public class TransactionController : Controller
{
    private readonly ITransactionQueryService _transactionQueryService;
    private readonly IManualTransactionService _manualTransactionService;

    public TransactionController(
        ITransactionQueryService transactionQueryService,
        IManualTransactionService manualTransactionService)
    {
        _transactionQueryService = transactionQueryService;
        _manualTransactionService = manualTransactionService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new TransactionQueryViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> List(TransactionQueryViewModel query)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", query);
        }

        if (!query.StartDate.HasValue || !query.EndDate.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "Both a start date and an end date are required.");
            return View("Index", query);
        }

        if (query.StartDate.Value > query.EndDate.Value)
        {
            ModelState.AddModelError(
                nameof(query.EndDate),
                "The end date must be on or after the start date.");
            return View("Index", query);
        }

        var transactions =
            await _transactionQueryService.GetTransactionsByDateRangeAsync(
                query.StartDate.Value,
                query.EndDate.Value);

        var summary = _transactionQueryService.GetTransactionSummary(transactions);

        var viewModel = new TransactionListViewModel
        {
            StartDate = query.StartDate.Value,
            EndDate = query.EndDate.Value,
            Transactions = transactions,
            Summary = summary
        };

        return View("List", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Enter()
    {
        var viewModel = new ManualTransactionViewModel
        {
            Date = DateOnly.FromDateTime(DateTime.Today)
        };

        await PopulateAccountsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(ManualTransactionViewModel viewModel)
    {
        var result = await _manualTransactionService.CreateAsync(
            viewModel.AccountId,
            viewModel.Description,
            viewModel.Date,
            viewModel.CreditDebitIndicator?.ToString(),
            viewModel.Amount,
            viewModel.RewardCategory);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var modelField = error.Key switch
                {
                    "accountId" => nameof(viewModel.AccountId),
                    "date" => nameof(viewModel.Date),
                    "creditDebitIndicator" => nameof(viewModel.CreditDebitIndicator),
                    "amount" => nameof(viewModel.Amount),
                    _ => string.Empty
                };

                ModelState.AddModelError(modelField, error.Value);
            }

            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Transaction entered successfully.";
        return RedirectToAction(nameof(Enter));
    }

    private async Task PopulateAccountsAsync(ManualTransactionViewModel viewModel)
    {
        var accounts = await _manualTransactionService.GetAccountsAsync();

        viewModel.Accounts = accounts
            .Select(account => new SelectListItem
            {
                Value = account.Id.ToString(),
                Text = account.AccountName
            })
            .ToList();
    }
}
