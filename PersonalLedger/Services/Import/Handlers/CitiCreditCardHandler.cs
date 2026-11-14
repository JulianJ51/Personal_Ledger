using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using PersonalLedger.Models.DTOs;
using PersonalLedger.Models.Enums;

namespace PersonalLedger.Services.Import.Handlers;

public class CitiCreditCardHandler : ITransactionImportHandler
{
    private const string InstitutionName = "Citi";

    public bool CanHandle(string fileName)
    {
        return Regex.IsMatch(
            Path.GetFileName(fileName),
            @"^\d+.*\.csv$",
            RegexOptions.IgnoreCase);
    }

    public Task<IReadOnlyList<ImportedTransaction>> ImportAsync(
        string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var accountMatch = Regex.Match(
            Path.GetFileNameWithoutExtension(fileName),
            @"^(?<accountNumber>\d+)");

        if (!accountMatch.Success)
        {
            throw new InvalidDataException(
                $"Could not extract an account number from filename: {fileName}");
        }

        var accountNumber = accountMatch.Groups["accountNumber"].Value;
        var transactions = new List<ImportedTransaction>();

        using var reader = new StreamReader(filePath);
        using var csv = CsvReader.Create(reader);

        var requiredHeaders = new[]
        {
            "Date",
            "Description",
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
                "Citi credit card CSV is missing one or more required columns.");
        }

        while (csv.Read())
        {
            var dateString = csv.GetString("Date")?.Trim() ?? string.Empty;
            var description = csv.GetString("Description")?.Trim() ?? string.Empty;
            var debitString = csv.GetString("Debit")?.Trim() ?? string.Empty;
            var creditString = csv.GetString("Credit")?.Trim() ?? string.Empty;

            if (!DateTime.TryParseExact(
                dateString,
                "M/d/yy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
            {
                throw new InvalidDataException(
                    $"Invalid transaction date on CSV row {csv.RowNumber}: {dateString}");
            }

            var hasDebit = !string.IsNullOrWhiteSpace(debitString);
            var hasCredit = !string.IsNullOrWhiteSpace(creditString);

            if (hasDebit == hasCredit)
            {
                throw new InvalidDataException(
                    $"CSV row {csv.RowNumber} must contain exactly one of Debit or Credit.");
            }

            var indicator = hasCredit
                ? CreditDebitIndicator.Credit
                : CreditDebitIndicator.Debit;

            var amount = ParseCents(
                hasCredit ? creditString : debitString,
                csv.RowNumber);

            transactions.Add(new ImportedTransaction
            {
                AccountNumber = accountNumber,
                Institution = InstitutionName,
                Description = description,
                Date = DateOnly.FromDateTime(parsedDate),
                CreditDebitIndicator = indicator,
                Amount = amount,
                AccountBalance = null,
                RewardCategory = null
            });
        }

        return Task.FromResult<IReadOnlyList<ImportedTransaction>>(
            transactions);
    }

    private static long ParseCents(string value, long rowNumber)
    {
        // Parentheses are formatting only; direction comes from the populated column.
        var normalizedValue = value
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (!decimal.TryParse(
            normalizedValue,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var amount))
        {
            throw new InvalidDataException(
                $"Invalid transaction amount on CSV row {rowNumber}: {value}");
        }

        if (amount <= 0)
        {
            throw new InvalidDataException(
                $"Transaction amount must be positive on CSV row {rowNumber}.");
        }

        var cents = amount * 100m;

        if (cents != decimal.Truncate(cents))
        {
            throw new InvalidDataException(
                $"Transaction amount contains fractions of a cent on CSV row {rowNumber}: {value}");
        }

        return checked((long)cents);
    }
}
