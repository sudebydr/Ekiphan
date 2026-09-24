namespace Ekiphan.Application.DataImport;

public sealed class ImportJobNotFoundException(Guid jobId)
    : Exception($"Import job '{jobId}' was not found.");
