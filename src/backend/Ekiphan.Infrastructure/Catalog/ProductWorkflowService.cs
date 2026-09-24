using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductWorkflowService(
    EkiphanDbContext dbContext,
    IProductRevisionService revisionService,
    IProductQualityService qualityService)
    : IProductWorkflowService
{
    public bool CanTransition(
        ProductWorkflowStatus currentStatus,
        ProductWorkflowStatus targetStatus,
        IReadOnlyCollection<string> permissions)
    {
        return targetStatus switch
        {
            ProductWorkflowStatus.Draft => currentStatus is ProductWorkflowStatus.InReview or ProductWorkflowStatus.Unpublished or ProductWorkflowStatus.Archived,
            ProductWorkflowStatus.InReview => currentStatus is ProductWorkflowStatus.Draft or ProductWorkflowStatus.Unpublished,
            ProductWorkflowStatus.Published => (currentStatus is ProductWorkflowStatus.InReview or ProductWorkflowStatus.Unpublished) ||
                                               (currentStatus == ProductWorkflowStatus.Draft && permissions.Contains(AdminPermissionCode.Products.PublishDirect)),
            ProductWorkflowStatus.Unpublished => currentStatus == ProductWorkflowStatus.Published,
            ProductWorkflowStatus.Archived => currentStatus is ProductWorkflowStatus.Draft or ProductWorkflowStatus.InReview or ProductWorkflowStatus.Published or ProductWorkflowStatus.Unpublished,
            _ => false
        };
    }

    public IReadOnlyCollection<ProductWorkflowStatus> GetAllowedTransitions(
        ProductWorkflowStatus currentStatus,
        IReadOnlyCollection<string> permissions)
    {
        var allowed = new List<ProductWorkflowStatus>();
        foreach (ProductWorkflowStatus status in Enum.GetValues<ProductWorkflowStatus>())
        {
            if (CanTransition(currentStatus, status, permissions))
            {
                allowed.Add(status);
            }
        }
        return allowed;
    }

    public async Task<ProductWorkflowTransitionResult> TransitionAsync(
        Guid productId,
        ProductWorkflowStatus targetStatus,
        string? reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(x => x.Categories)
            .Include(x => x.Translations)
            .Include(x => x.Tags)
            .Include(x => x.Variants)
            .SingleOrDefaultAsync(x => x.Id == productId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException(ProductManagementErrorCodes.ProductNotFound);

        // Check concurrency
        if (rowVersion.Length > 0 && product.RowVersion.Length > 0 && !product.RowVersion.SequenceEqual(rowVersion))
        {
            throw new DbUpdateConcurrencyException(ProductManagementErrorCodes.ProductConcurrencyConflict);
        }

        var permissions = new HashSet<string> { AdminPermissionCode.Products.Publish, AdminPermissionCode.Products.PublishDirect };
        if (!CanTransition(product.WorkflowStatus, targetStatus, permissions))
        {
            return new ProductWorkflowTransitionResult(false, product.WorkflowStatus, "Invalid workflow status transition.");
        }

        // Evaluate quality if transitioning to Published
        if (targetStatus == ProductWorkflowStatus.Published)
        {
            var quality = await qualityService.EvaluateAsync(productId, cancellationToken);
            if (quality.BlocksPublishing)
            {
                return new ProductWorkflowTransitionResult(false, product.WorkflowStatus, ProductManagementErrorCodes.ProductQualityBlocksPublishing);
            }
        }

        var oldStatus = product.WorkflowStatus;
        product.SetWorkflowStatus(targetStatus, actorUserId, reason);
        product.IncrementVersion();

        var changeType = targetStatus switch
        {
            ProductWorkflowStatus.InReview => ProductRevisionChangeType.SubmittedForReview,
            ProductWorkflowStatus.Published => ProductRevisionChangeType.Published,
            ProductWorkflowStatus.Unpublished => ProductRevisionChangeType.Unpublished,
            ProductWorkflowStatus.Archived => ProductRevisionChangeType.Archived,
            _ => ProductRevisionChangeType.Updated
        };

        await revisionService.CreateRevisionAsync(
            product,
            changeType,
            actorUserId,
            reason,
            changedFieldsJson: $"{{\"WorkflowStatus\":{{\"Old\":\"{oldStatus}\",\"New\":\"{targetStatus}\"}}}}",
            source: ProductRevisionSource.Manual,
            cancellationToken: cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProductWorkflowTransitionResult(true, product.WorkflowStatus);
    }
}
