using FluentValidation;

namespace Ekiphan.Application.DataImport;

public sealed class ProductImportPreviewRequestValidator : AbstractValidator<ProductImportPreviewRequest>
{
    public ProductImportPreviewRequestValidator()
    {
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260)
            .Must(x => Path.GetFileName(x) == x).WithMessage("File name cannot contain a path.")
            .Must(x => Path.GetExtension(x).Equals(".csv", StringComparison.OrdinalIgnoreCase) ||
                       Path.GetExtension(x).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .csv and .xlsx files are supported.");
        RuleFor(x => x.Length).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class ProductImportValidateCommandValidator : AbstractValidator<ProductImportValidateCommand>
{
    public ProductImportValidateCommandValidator()
    {
        RuleFor(x => x.UploadToken).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Mappings).NotNull().NotEmpty();
        RuleForEach(x => x.Mappings).ChildRules(mapping =>
        {
            mapping.RuleFor(x => x.SourceHeader).NotEmpty().MaximumLength(250);
            mapping.RuleFor(x => x.TargetField).Must(ProductImportField.All.Contains)
                .WithMessage("Unknown target field.");
        });
        RuleFor(x => x.Mappings).Must(x => x.Select(m => m.SourceHeader).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Count)
            .WithMessage("A source column can only be mapped once.");
        RuleFor(x => x.Mappings).Must(x => x.Select(m => m.TargetField).Distinct(StringComparer.Ordinal).Count() == x.Count)
            .WithMessage("A target field can only be mapped once.");
        RuleFor(x => x.Mappings).Must(x => x.Any(m => m.TargetField == ProductImportField.Sku) &&
                                           x.Any(m => m.TargetField == ProductImportField.ProductNameTr))
            .WithMessage("Sku and ProductNameTr mappings are required.");
    }
}

public sealed class ProductImportExecuteCommandValidator : AbstractValidator<ProductImportExecuteCommand>
{
    public ProductImportExecuteCommandValidator() => RuleFor(x => x.ValidationToken).NotEmpty().MaximumLength(200);
}

public sealed class ProductImportRollbackCommandValidator : AbstractValidator<ProductImportRollbackCommand>
{
    public ProductImportRollbackCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
