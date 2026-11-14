
using Microsoft.AspNetCore.Mvc;
using PersonalLedger.Services.Interfaces;
using PersonalLedger.Models.ViewModels;

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
                

        var summary = _transactionQueryService.GetTransactionSummary(
            transactions);

        var viewModel = new TransactionListViewModel
        {
            StartDate = query.StartDate.Value,
            EndDate = query.EndDate.Value,
            Transactions = transactions,
            Summary = summary
        };

        return View("List", viewModel);
    }
}
