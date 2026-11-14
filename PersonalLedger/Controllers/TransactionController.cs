using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Models.Enums;
using PersonalLedger.Services.Interfaces;
using PersonalLedger.ViewModels;

namespace PersonalLedger.Controllers;

public class TransactionController : Controller
{
    private readonly LedgerDbContext _context;
    private readonly ITransactionQueryService _transactionQueryService;

    public TransactionController(
        LedgerDbContext context,
        ITransactionQueryService transactionQueryService)
    {
        _context = context;
        _transactionQueryService = transactionQueryService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new TransactionQueryViewModel());
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
        if (!Enum.IsDefined(viewModel.CreditDebitIndicator))
        {
            ModelState.AddModelError(
                nameof(viewModel.CreditDebitIndicator),
                "Select a valid credit/debit indicator.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        if (!decimal.TryParse(
            viewModel.Amount,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var amount) || amount <= 0)
        {
            ModelState.AddModelError(
                nameof(viewModel.Amount),
                "Enter a positive amount with exactly two decimal places.");
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        long amountInCents;

        try
        {
            amountInCents = checked((long)(amount * 100m));
        }
        catch (OverflowException)
        {
            ModelState.AddModelError(
                nameof(viewModel.Amount),
                "The amount is too large.");
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        var account = await _context.Accounts
            .SingleOrDefaultAsync(a => a.Id == viewModel.AccountId!.Value);

        if (account is null)
        {
            ModelState.AddModelError(
                nameof(viewModel.AccountId),
                "Select an account that exists.");
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        // Credit card balances represent amounts owed, so purchases (debits)
        // increase the balance and payments/credits reduce it.
        var increasesBalance =
            account.Type == AccountType.CreditCard
                ? viewModel.CreditDebitIndicator == CreditDebitIndicator.Debit
                : viewModel.CreditDebitIndicator == CreditDebitIndicator.Credit;

        long updatedBalance;

        try
        {
            updatedBalance = increasesBalance
                ? checked(account.Balance + amountInCents)
                : checked(account.Balance - amountInCents);
        }
        catch (OverflowException)
        {
            ModelState.AddModelError(
                nameof(viewModel.Amount),
                "This transaction would cause the account balance to exceed the supported range.");
            await PopulateAccountsAsync(viewModel);
            return View(viewModel);
        }

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Description = viewModel.Description ?? string.Empty,
            Date = viewModel.Date!.Value,
            CreditDebitIndicator = viewModel.CreditDebitIndicator,
            Amount = amountInCents,
            AccountBalance = updatedBalance,
            RewardCategory = string.IsNullOrWhiteSpace(viewModel.RewardCategory)
                ? null
                : viewModel.RewardCategory.Trim()
        };

        account.Balance = updatedBalance;
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Transaction entered successfully.";
        return RedirectToAction(nameof(Enter));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        TransactionQueryViewModel query)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", query);
        }

        if (!query.StartDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(query.StartDate),
                "A start date is required.");
        }

        if (!query.EndDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(query.EndDate),
                "An end date is required.");
        }

        if (!query.StartDate.HasValue || !query.EndDate.HasValue)
        {
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

        var viewModel = new TransactionListViewModel
        {
            StartDate = query.StartDate.Value,
            EndDate = query.EndDate.Value,
            Transactions = transactions
        };

        return View("List", viewModel);
    }

    private async Task PopulateAccountsAsync(
        ManualTransactionViewModel viewModel)
    {
        viewModel.AccountOptions = await _context.Accounts
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
