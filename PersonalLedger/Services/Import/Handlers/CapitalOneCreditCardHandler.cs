
using System.Globalization;
using CsvHelper;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Services.Import.Handlers;

public class CapitalOneCreditCardHandler
    : ITransactionImportHandler
{
    private const string InstitutionName = "Capital One";

    public bool CanHandle(string fileName)
    {
        return Path.GetFileName(fileName)
            .Contains(
                "transaction_download",
                StringComparison.OrdinalIgnoreCase)
            && Path.GetExtension(fileName)
                .Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<ImportedTransaction>> ImportAsync(
        string filePath)
    {
        var transactions = new List<ImportedTransaction>();

        using var reader = new StreamReader(filePath);
        using var csv = CsvReader.Create(reader);

        var requiredHeaders = new[]
        {
            "Transaction Date",
            "Card No.",
            "Description",
            "Category",
            "Debit",
            "Credit"
        };

        if (csv.Headers == null ||
            requiredHeaders.Any(header =>
                !csv.Headers.Contains(
                    header,
                    StringComparer.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException(
                "Capital One credit card CSV is missing one or more required columns.");
        }

        while (csv.Read())
        {
            var accountNumber =
                csv.GetString("Card No.") ?? string.Empty;

            var description =
                csv.GetString("Description") ?? string.Empty;

            var category =
                csv.GetString("Category");

            var dateString =
                csv.GetString("Transaction Date") ?? string.Empty;

            var debitString =
                csv.GetString("Debit") ?? string.Empty;

            var creditString =
                csv.GetString("Credit") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new InvalidDataException(
                    $"Missing card number on CSV row {csv.RowNumber}.");
            }

            if (!DateTime.TryParseExact(
                dateString,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                throw new InvalidDataException(
                    $"Invalid transaction date on CSV row {csv.RowNumber}.");
            }

            // Exactly one of Debit or Credit must contain an amount.
            var hasDebit = !string.IsNullOrWhiteSpace(debitString);
            var hasCredit = !string.IsNullOrWhiteSpace(creditString);

            if (hasDebit == hasCredit)
            {
                throw new InvalidDataException(
                    $"CSV row {csv.RowNumber} must contain exactly one of Debit or Credit.");
            }

            var indicator = hasDebit
                ? CreditDebitIndicator.Debit
                : CreditDebitIndicator.Credit;

            var amount = ParseCents(
                hasDebit ? debitString : creditString);

            if (amount <= 0)
            {
                throw new InvalidDataException(
                    $"Transaction amount must be positive on CSV row {csv.RowNumber}.");
            }

            transactions.Add(new ImportedTransaction
            {
                AccountNumber = accountNumber,
                Institution = InstitutionName,
                Description = description,
                Date = DateOnly.FromDateTime(parsedDate),
                CreditDebitIndicator = indicator,
                Amount = amount,
                AccountBalance = null,
                RewardCategory = string.IsNullOrWhiteSpace(category)
                    ? null
                    : category.Trim()
            });
        }

        return Task.FromResult<IReadOnlyList<ImportedTransaction>>(
            transactions);
    }

    private static long ParseCents(string value)
    {
        if (!decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var amount))
        {
            throw new InvalidDataException(
                $"Invalid monetary value: {value}");
        }

        var cents = amount * 100m;

        if (cents != decimal.Truncate(cents))
        {
            throw new InvalidDataException(
                $"Monetary value contains fractions of a cent: {value}");
        }

        return checked((long)cents);
    }
}
