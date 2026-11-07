using Microsoft.EntityFrameworkCore;
using PersonalLedger.Data;
using PersonalLedger.Models;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Services.Import;

public class TransactionImportService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TransactionImportFactory _factory;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransactionImportService> _logger;

    public TransactionImportService(
        IServiceScopeFactory scopeFactory,
        TransactionImportFactory factory,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<TransactionImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _factory = factory;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configuredDirectory =
            _configuration["Import:StagingDirectory"]
            ?? "ImportFiles";

        var stagingDirectory = Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.Combine(
                _environment.ContentRootPath,
                configuredDirectory);

        Directory.CreateDirectory(stagingDirectory);

        var files = Directory.GetFiles(
            stagingDirectory,
            "*.csv",
            SearchOption.TopDirectoryOnly);

        _logger.LogInformation(
            "Found {FileCount} CSV files to import.",
            files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ImportTransactionFile(file, cancellationToken);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to import transaction file {FileName}.",
                    Path.GetFileName(file));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ImportTransactionFile(
        string filePath,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);

        _logger.LogInformation(
            "Beginning import of {FileName}.",
            fileName);

        var handler = _factory.GetHandler(fileName);

        var importedTransactions =
            await handler.ImportAsync(filePath);

        if (importedTransactions.Count == 0)
        {
            _logger.LogInformation(
                "No transactions found in {FileName}.",
                fileName);

            return;
        }

        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<LedgerDbContext>();

        var accountNumbers = importedTransactions
            .Select(t => t.AccountNumber)
            .Distinct()
            .ToList();

        var institutions = importedTransactions
            .Select(t => t.Institution)
            .Distinct()
            .ToList();

        var accounts = await dbContext.Accounts
            .Where(a =>
                accountNumbers.Contains(a.AccountNumber) &&
                institutions.Contains(a.Institution))
            .ToListAsync(cancellationToken);

        var accountLookup = accounts.ToDictionary(
            a => (a.Institution, a.AccountNumber));

        var accountIds = accounts
            .Select(a => a.Id)
            .ToList();

        foreach (var imported in importedTransactions)
        {
            if (!accountLookup.TryGetValue(
                (imported.Institution, imported.AccountNumber),
                out var account))
            {
                throw new InvalidOperationException(
                    $"Account {imported.AccountNumber} " +
                    $"at {imported.Institution} was not found.");
            }
        }

        var existingTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId))
            .ToListAsync(cancellationToken);

        var existingKeys = existingTransactions
            .Select(CreateTransactionKey)
            .ToHashSet();

        var importedCount = 0;
        var duplicateCount = 0;

        foreach (var imported in importedTransactions)
        {
            var account = accountLookup[
                (imported.Institution, imported.AccountNumber)];

            var transaction = new Transaction
            {
                AccountId = account.Id,
                Description = imported.Description,
                Date = imported.Date,
                CreditDebitIndicator = imported.CreditDebitIndicator,
                Amount = imported.Amount,
                RewardCategory = imported.RewardCategory,
                AccountBalance = imported.AccountBalance
            };

            var key = CreateTransactionKey(transaction);

            if (!existingKeys.Add(key))
            {
                duplicateCount++;
                continue;
            }

            dbContext.Transactions.Add(transaction);
            importedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed import of {FileName}. " +
            "Imported: {ImportedCount}. Duplicates skipped: {DuplicateCount}.",
            fileName,
            importedCount,
            duplicateCount);
    }

    private static TransactionImportKey CreateTransactionKey(
        Transaction transaction)
    {
        return new TransactionImportKey(
            transaction.AccountId,
            transaction.Date,
            transaction.Description,
            transaction.CreditDebitIndicator,
            transaction.Amount,
            transaction.RewardCategory,
            transaction.AccountBalance);
    }

    private readonly record struct TransactionImportKey(
        int AccountId,
        DateOnly Date,
        string Description,
        CreditDebitIndicator Indicator,
        long Amount,
        string? RewardCategory,
        long? AccountBalance);
}