
using Microsoft.AspNetCore.Mvc;
using PersonalLedger.Services.Interfaces;
using PersonalLedger.ViewModels;

namespace PersonalLedger.Controllers;

public class TransactionController : Controller
{
    private readonly ITransactionQueryService _transactionQueryService;

    public TransactionController(
        ITransactionQueryService transactionQueryService)
    {
        _transactionQueryService = transactionQueryService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new TransactionQueryViewModel());
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
}
