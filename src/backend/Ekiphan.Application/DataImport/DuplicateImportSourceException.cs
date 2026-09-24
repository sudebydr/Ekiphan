namespace Ekiphan.Application.DataImport;

public sealed class DuplicateImportSourceException : Exception
{
    public DuplicateImportSourceException()
        : base("The same import source has already been staged.")
    {
    }
}
