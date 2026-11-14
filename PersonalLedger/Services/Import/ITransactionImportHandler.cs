namespace PersonalLedger.Services.Import;
using PersonalLedger.Models.DTOs;

public interface ITransactionImportHandler
{
    bool CanHandle(string fileName);

    Task<IReadOnlyList<ImportedTransaction>> ImportAsync(
        string filePath);
}