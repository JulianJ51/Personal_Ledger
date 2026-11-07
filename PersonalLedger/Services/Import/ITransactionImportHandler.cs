namespace PersonalLedger.Services.Import;

public interface ITransactionImportHandler
{
    bool CanHandle(string fileName);

    Task<IReadOnlyList<ImportedTransaction>> ImportAsync(
        string filePath);
}