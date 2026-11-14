using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalLedger.Models.DTOs;
using PersonalLedger.Models.ViewModels;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Controllers;

public class TransactionController : Controller
{
    private readonly ITransactionQueryService _transactionQueryService;
    private readonly IManualTransactionService _manualTransactionService;

    public TransactionController(ITransactionQueryService transactionQueryService,
        IManualTransactionService manualTransactionService)
    {
        _transactionQueryService = transactionQueryService;
        _manualTransactionService = manualTransactionService;
    }

    [HttpGet]
    public IActionResult Index() => View(new TransactionQueryViewModel());

    [HttpGet]
    public async Task<IActionResult> List(TransactionQueryViewModel query)
    {
        if (!ModelState.IsValid) return View("Index", query);
        if (!query.StartDate.HasValue || !query.EndDate.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Both a start date and an end date are required.");
            return View("Index", query);
        }
        if (query.StartDate.Value > query.EndDate.Value)
        {
            ModelState.AddModelError(nameof(query.EndDate), "The end date must be on or after the start date.");
            return View("Index", query);
        }

        var transactions = await _transactionQueryService.GetTransactionsByDateRangeAsync(
            query.StartDate.Value, query.EndDate.Value);
        var summary = _transactionQueryService.GetTransactionSummary(transactions);
        return View("List", new TransactionListViewModel
        {
            StartDate = query.StartDate.Value,
            EndDate = query.EndDate.Value,
            Transactions = transactions,
            Summary = summary
        });
    }

    [HttpGet]
    public async Task<IActionResult> Enter()
    {
        var viewModel = new ManualTransactionViewModel { Date = DateOnly.FromDateTime(DateTime.Today) };
        await PopulateAccountsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(ManualTransactionViewModel viewModel)
    {
        var request = new CreateManualTransactionRequest
        {
            AccountId = viewModel.AccountId,
            Description = viewModel.Description,
            Date = viewModel.Date,
            CreditDebitIndicator = viewModel.CreditDebitIndicator?.ToString(),
            Amount = viewModel.Amount,
            RewardCategory = viewModel.RewardCategory
        };

        var result = await _manualTransactionService.CreateAsync(request);

        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Transaction entered successfully.";
        return RedirectToAction(nameof(Enter));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var transaction = await _manualTransactionService.GetTransactionAsync(id);
        if (transaction is null) return NotFound();

        var viewModel = new TransactionDetailsViewModel
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account.AccountName,
            Institution = transaction.Account.Institution,
            Description = transaction.Description,
            Date = transaction.Date,
            CreditDebitIndicator = transaction.CreditDebitIndicator,
            Amount = (transaction.Amount / 100m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            AccountBalance = transaction.AccountBalance.HasValue
                ? (transaction.AccountBalance.Value / 100m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                : null,
            RewardCategory = transaction.RewardCategory
        };

        await PopulateAccountsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(int id, TransactionDetailsViewModel viewModel)
    {
        viewModel.Id = id;

        var request = new UpdateManualTransactionRequest
        {
            AccountId = viewModel.AccountId,
            Description = viewModel.Description,
            Date = viewModel.Date,
            CreditDebitIndicator = viewModel.CreditDebitIndicator?.ToString(),
            Amount = viewModel.Amount,
            AccountBalance = viewModel.AccountBalance,
            RewardCategory = viewModel.RewardCategory
        };

        var result = await _manualTransactionService.UpdateAsync(id, request);

        if (!result.Succeeded)
        {
            if (result.Errors.ContainsKey(string.Empty)) return NotFound();
            AddServiceErrors(result);
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Transaction updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _manualTransactionService.DeleteAsync(id);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Transaction deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private void AddServiceErrors(ManualTransactionResult result)
    {
        foreach (var error in result.Errors)
        {
            var field = error.Key switch
            {
                "accountId" => nameof(TransactionDetailsViewModel.AccountId),
                "date" => nameof(TransactionDetailsViewModel.Date),
                "creditDebitIndicator" => nameof(TransactionDetailsViewModel.CreditDebitIndicator),
                "amount" => nameof(TransactionDetailsViewModel.Amount),
                "accountBalance" => nameof(TransactionDetailsViewModel.AccountBalance),
                _ => string.Empty
            };
            ModelState.AddModelError(field, error.Value);
        }
    }

    private async Task PopulateAccountsAsync(ManualTransactionViewModel viewModel)
    {
        var accounts = await _manualTransactionService.GetAccountsAsync();
        viewModel.Accounts = accounts.Select(account => new SelectListItem
        {
            Value = account.Id.ToString(), Text = account.AccountName
        }).ToList();
    }

    private async Task PopulateAccountsAsync(TransactionDetailsViewModel viewModel)
    {
        var accounts = await _manualTransactionService.GetAccountsAsync();
        viewModel.Accounts = accounts.Select(account => new SelectListItem
        {
            Value = account.Id.ToString(), Text = account.AccountName
        }).ToList();
    }
}
