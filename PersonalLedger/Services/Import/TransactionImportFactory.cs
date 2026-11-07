namespace PersonalLedger.Services.Import;

public class TransactionImportFactory
{
    private readonly IEnumerable<ITransactionImportHandler> _handlers;

    public TransactionImportFactory(
        IEnumerable<ITransactionImportHandler> handlers)
    {
        _handlers = handlers;
    }

    public ITransactionImportHandler GetHandler(string fileName)
    {
        return _handlers.FirstOrDefault(
            handler => handler.CanHandle(fileName))
            ?? throw new NotSupportedException(
                $"No import handler found for file: {fileName}");
    }
}