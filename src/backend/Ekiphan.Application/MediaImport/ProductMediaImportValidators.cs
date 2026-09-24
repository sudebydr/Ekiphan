using FluentValidation;

namespace Ekiphan.Application.MediaImport;

public sealed class ProductMediaImportPreviewRequestValidator : AbstractValidator<ProductMediaImportPreviewRequest>
{
    public ProductMediaImportPreviewRequestValidator()
    {
        RuleFor(x => x.Content).NotNull(); RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.Length).GreaterThan(0); RuleFor(x => x.UserId).NotEmpty();
    }
}
public sealed class ProductMediaManualMappingValidator : AbstractValidator<ProductMediaManualMappingDto>
{
    public ProductMediaManualMappingValidator()
    {
        RuleFor(x => x.TemporaryFileId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.ProductId is { } id && id != Guid.Empty || !string.IsNullOrWhiteSpace(x.Sku))
            .WithMessage("A product id or SKU is required.");
    }
}
public sealed class ProductMediaImportValidateCommandValidator : AbstractValidator<ProductMediaImportValidateCommand>
{
    public ProductMediaImportValidateCommandValidator()
    {
        RuleFor(x => x.UploadToken).NotEmpty().Matches("^[A-Za-z0-9_-]{32,200}$");
        RuleForEach(x => x.ManualMappings).SetValidator(new ProductMediaManualMappingValidator());
        RuleFor(x => x.ManualMappings).Must(items => items is null || items.Select(x => x.TemporaryFileId).Distinct().Count() == items.Count)
            .WithMessage("A file can only be mapped once.");
        RuleFor(x => x.ManualMappings).Must(items => items is null || items.Where(x => x.IsPrimary)
            .GroupBy(x => x.ProductId?.ToString() ?? x.Sku, StringComparer.OrdinalIgnoreCase).All(g => g.Count() == 1))
            .WithMessage("A product can only have one manually selected primary image.");
    }
}
public sealed class ProductMediaImportExecuteCommandValidator : AbstractValidator<ProductMediaImportExecuteCommand>
{ public ProductMediaImportExecuteCommandValidator() => RuleFor(x => x.ValidationToken).NotEmpty().Matches("^[A-Za-z0-9_-]{32,200}$"); }
public sealed class ProductMediaImportRollbackCommandValidator : AbstractValidator<ProductMediaImportRollbackCommand>
{ public ProductMediaImportRollbackCommandValidator() { RuleFor(x => x.BatchId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty(); } }
