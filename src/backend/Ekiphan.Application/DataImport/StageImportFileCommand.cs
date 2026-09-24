namespace Ekiphan.Application.DataImport;

public sealed record StageImportFileCommand(
    Stream Content,
    string FileName,
    bool IsDryRun,
    Guid? CreatedByUserId = null,
    ImportFileReadOptions? ReadOptions = null);
