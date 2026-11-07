using System.Globalization;
using CsvHelper;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Services.Import.Handlers;

public class CapitalOneCheckingSavingsHandler
    : ITransactionImportHandler
{
    private const string InstitutionName = "Capital One";

    public bool CanHandle(string fileName)
    {
        return Path.GetFileName(fileName)
            .Contains(
                "360Checking",
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
            "Account Number",
            "Transaction Description",
            "Transaction Date",
            "Transaction Type",
            "Transaction Amount",
            "Balance"
        };

        Console.WriteLine("CSV Headers:");

        foreach (var header in csv.Headers)
        {
            Console.WriteLine($"[{header}] Length: {header.Length}");
        }

        if (csv.Headers == null ||
            requiredHeaders.Any(header =>
                !csv.Headers.Contains(
                    header,
                    StringComparer.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException(
                "Capital One CSV is missing one or more required columns.");
        }

        while (csv.Read())
        {
            var accountNumber =
                csv.GetString("Account Number") ?? string.Empty;

            var description =
                csv.GetString("Transaction Description") ?? string.Empty;

            var dateString =
                csv.GetString("Transaction Date") ?? string.Empty;

            var typeString =
                csv.GetString("Transaction Type") ?? string.Empty;

            var amountString =
                csv.GetString("Transaction Amount") ?? string.Empty;

            var balanceString =
                csv.GetString("Balance") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new InvalidDataException(
                    $"Missing account number on CSV row {csv.RowNumber}.");
            }

            if (!DateTime.TryParseExact(
                dateString,
                "M/d/yy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                throw new InvalidDataException(
                    $"Invalid transaction date on CSV row {csv.RowNumber}.");
            }

            if (!Enum.TryParse<CreditDebitIndicator>(
                typeString,
                true,
                out var indicator))
            {
                throw new InvalidDataException(
                    $"Invalid transaction type on CSV row {csv.RowNumber}.");
            }

            var amount = ParseCents(amountString);
            var balance = ParseCents(balanceString);

            transactions.Add(new ImportedTransaction
            {
                AccountNumber = accountNumber,
                Institution = InstitutionName,
                Description = description,
                Date = DateOnly.FromDateTime(parsedDate),
                CreditDebitIndicator = indicator,
                Amount = amount,
                AccountBalance = balance
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