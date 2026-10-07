namespace Ekiphan.Application.DataImport;

public sealed class DuplicateImportSourceException : Exception
{
    public DuplicateImportSourceException()
        : base("Aynı import kaynağı daha önce doğrulama için işlenmiş.")
    {
    }
}
