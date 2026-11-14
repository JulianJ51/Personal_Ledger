using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Models.Enums;
using PersonalLedger.Models.ViewModels;
using PersonalLedger.Services.Interfaces;

namespace PersonalLedger.Controllers;

public class TransactionController : Controller
{
    private readonly ITransactionQueryService _transactionQueryService;
    private readonly LedgerDbContext _context;

    public TransactionController(
        ITransactionQueryService transactionQueryService,
        LedgerDbContext context)
    {
        _transactionQueryService = transactionQueryService;
        _context = context;
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
        if (!ModelState.IsValid)
        {
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        if (!Enum.IsDefined(viewModel.CreditDebitIndicator!.Value))
        {
            ModelState.AddModelError(
                nameof(viewModel.CreditDebitIndicator),
                "Select a valid credit or debit indicator.");
        }

        if (!decimal.TryParse(
                viewModel.Amount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount) || amount <= 0)
        {
            ModelState.AddModelError(
                nameof(viewModel.Amount),
                "Enter an amount greater than zero with exactly two decimal places.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        var amountInCents = checked((long)(amount * 100m));
        var account = await _context.Accounts
            .SingleOrDefaultAsync(a => a.Id == viewModel.AccountId);

        if (account is null)
        {
            ModelState.AddModelError(
                nameof(viewModel.AccountId),
                "The selected account could not be found.");
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        var isCreditCard = account.Type == AccountType.CreditCard;
        var increasesBalance =
            isCreditCard
                ? viewModel.CreditDebitIndicator == CreditDebitIndicator.Debit
                : viewModel.CreditDebitIndicator == CreditDebitIndicator.Credit;

        account.Balance = checked(
            increasesBalance
                ? account.Balance + amountInCents
                : account.Balance - amountInCents);

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Description = viewModel.Description ?? string.Empty,
            Date = viewModel.Date!.Value,
            CreditDebitIndicator = viewModel.CreditDebitIndicator.Value,
            Amount = amountInCents,
            AccountBalance = account.Balance,
            RewardCategory = string.IsNullOrWhiteSpace(viewModel.RewardCategory)
                ? null
                : viewModel.RewardCategory.Trim()
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Transaction entered successfully.";
        return RedirectToAction(nameof(Enter));
    }

    private async Task PopulateAccountsAsync(ManualTransactionViewModel viewModel)
    {
        viewModel.Accounts = await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.AccountName)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.AccountName
            })
            .ToListAsync();
    }
}
