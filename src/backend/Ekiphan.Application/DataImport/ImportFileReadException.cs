namespace Ekiphan.Application.DataImport;

public sealed class ImportFileReadException : Exception
{
    public ImportFileReadException(string message)
        : base(message)
    {
    }

    public ImportFileReadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
