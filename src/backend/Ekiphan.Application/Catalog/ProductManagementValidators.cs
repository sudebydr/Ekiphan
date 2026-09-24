using FluentValidation;

namespace Ekiphan.Application.Catalog;

public sealed class DuplicateProductCommandValidator : AbstractValidator<DuplicateProductCommand>
{
    public DuplicateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NewSku).NotEmpty().MaximumLength(100);
    }
}

public sealed class ProductBulkPreviewCommandValidator : AbstractValidator<ProductBulkPreviewCommand>
{
    public ProductBulkPreviewCommandValidator()
    {
        RuleFor(x => x.OperationType).IsInEnum();
        RuleFor(x => x.ProductIds).NotEmpty().Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("ProductIds must be unique and non-empty.");
        RuleFor(x => x.ProductIds.Count).LessThanOrEqualTo(5000)
            .WithMessage("Maximum item limit for bulk operation is 5000.");
    }
}

public sealed class ProductBulkExecuteCommandValidator : AbstractValidator<ProductBulkExecuteCommand>
{
    public ProductBulkExecuteCommandValidator()
    {
        RuleFor(x => x.OperationType).IsInEnum();
        RuleFor(x => x.ProductIds).NotEmpty().Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("ProductIds must be unique and non-empty.");
        RuleFor(x => x.ProductIds.Count).LessThanOrEqualTo(5000)
            .WithMessage("Maximum item limit for bulk operation is 5000.");
    }
}

public sealed class ReorderCategoryProductsCommandValidator : AbstractValidator<ReorderCategoryProductsCommand>
{
    public ReorderCategoryProductsCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Items).Must(x => x.Select(i => i.ProductId).Distinct().Count() == x.Count)
            .WithMessage("Product IDs must be unique.");
        RuleFor(x => x.Items).Must(x => x.All(i => i.SortOrder >= 0))
            .WithMessage("SortOrder must be non-negative.");
    }
}
