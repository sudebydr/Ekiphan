using System.Security.Claims;
using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Catalog;

internal static class AdvancedProductEndpoints
{
    public static void MapAdvancedProductEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/products")
            .RequireRateLimiting("catalog-admin-read");

        if (!authenticationConfigured)
        {
            group.MapPost("/{productId:guid}/submit-review", Unavailable);
            group.MapPost("/{productId:guid}/publish", Unavailable);
            group.MapPost("/{productId:guid}/unpublish", Unavailable);
            group.MapPost("/{productId:guid}/archive", Unavailable);
            group.MapPost("/{productId:guid}/restore-draft", Unavailable);
            group.MapPost("/{productId:guid}/duplicate", Unavailable);
            group.MapGet("/{productId:guid}/revisions", Unavailable);
            group.MapGet("/{productId:guid}/revisions/{revisionId:guid}", Unavailable);
            group.MapPost("/{productId:guid}/revisions/{revisionId:guid}/restore", Unavailable);
            group.MapPost("/bulk/preview", Unavailable);
            group.MapPost("/bulk", Unavailable);
            group.MapGet("/bulk-operations/{operationId:guid}", Unavailable);
            group.MapGet("/bulk-operations/{operationId:guid}/errors", Unavailable);
            group.MapGet("/{productId:guid}/quality", Unavailable);
            group.MapPost("/{productId:guid}/quality/recalculate", Unavailable);
            group.MapGet("/quality", Unavailable);
            group.MapPost("/quality/recalculate", Unavailable);

            endpoints.MapPut("/api/admin/categories/{categoryId:guid}/products/reorder", Unavailable);
            return;
        }

        group.RequireAuthorization("CatalogManage");

        group.MapPost("/{productId:guid}/submit-review", SubmitReviewAsync)
            .RequireAuthorization("CatalogManage")
            .WithSummary("Submit product for review");

        group.MapPost("/{productId:guid}/publish", PublishAsync)
            .RequireAuthorization("ProductsPublish")
            .WithSummary("Publish product");

        group.MapPost("/{productId:guid}/unpublish", UnpublishAsync)
            .RequireAuthorization("ProductsPublish")
            .WithSummary("Unpublish product");

        group.MapPost("/{productId:guid}/archive", ArchiveAsync)
            .RequireAuthorization("ProductsArchive")
            .WithSummary("Archive product");

        group.MapPost("/{productId:guid}/restore-draft", RestoreDraftAsync)
            .RequireAuthorization("CatalogManage")
            .WithSummary("Restore product to draft status");

        group.MapPost("/{productId:guid}/duplicate", DuplicateAsync)
            .RequireAuthorization("CatalogManage")
            .WithSummary("Duplicate product");

        group.MapGet("/{productId:guid}/revisions", GetRevisionsAsync)
            .RequireAuthorization("ProductsHistoryRead")
            .WithSummary("Get product revisions list");

        group.MapGet("/{productId:guid}/revisions/{revisionId:guid}", GetRevisionDetailAsync)
            .RequireAuthorization("ProductsHistoryRead")
            .WithSummary("Get product revision detail");

        group.MapPost("/{productId:guid}/revisions/{revisionId:guid}/restore", RestoreRevisionAsync)
            .RequireAuthorization("ProductsHistoryRestore")
            .WithSummary("Restore product revision");

        group.MapPost("/bulk/preview", BulkPreviewAsync)
            .RequireAuthorization("ProductsBulkUpdate")
            .WithSummary("Preview bulk product operation");

        group.MapPost("/bulk", BulkExecuteAsync)
            .RequireAuthorization("ProductsBulkUpdate")
            .WithSummary("Execute bulk product operation");

        group.MapGet("/bulk-operations/{operationId:guid}", GetBulkOperationStatusAsync)
            .RequireAuthorization("ProductsBulkUpdate")
            .WithSummary("Get bulk operation status");

        group.MapGet("/bulk-operations/{operationId:guid}/errors", DownloadBulkErrorsCsvAsync)
            .RequireAuthorization("ProductsBulkUpdate")
            .WithSummary("Download bulk operation error report as CSV");

        group.MapGet("/{productId:guid}/quality", GetProductQualityAsync)
            .RequireAuthorization("ProductsQualityRead")
            .WithSummary("Get single product quality report");

        group.MapPost("/{productId:guid}/quality/recalculate", RecalculateProductQualityAsync)
            .RequireAuthorization("ProductsQualityRecalculate")
            .WithSummary("Recalculate product quality report");

        group.MapGet("/quality", GetQualityDashboardAsync)
            .RequireAuthorization("ProductsQualityRead")
            .WithSummary("Get quality dashboard");

        group.MapPost("/quality/recalculate", RecalculateBatchQualityAsync)
            .RequireAuthorization("ProductsQualityRecalculate")
            .WithSummary("Recalculate quality for product batch");

        endpoints.MapPut("/api/admin/categories/{categoryId:guid}/products/reorder", ReorderCategoryProductsAsync)
            .RequireAuthorization("CategoriesReorder")
            .WithSummary("Reorder products within category");
    }

    private static async Task<IResult> SubmitReviewAsync(
        Guid productId,
        [FromBody] WorkflowRequest request,
        ClaimsPrincipal user,
        IProductWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var res = await workflowService.TransitionAsync(productId, ProductWorkflowStatus.InReview, request.Reason, versionBytes, actorId, cancellationToken);
        return res.Success ? Results.Ok(res) : Problem(400, res.ErrorMessage ?? "Transition failed.");
    }

    private static async Task<IResult> PublishAsync(
        Guid productId,
        [FromBody] WorkflowRequest request,
        ClaimsPrincipal user,
        IProductWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var res = await workflowService.TransitionAsync(productId, ProductWorkflowStatus.Published, request.Reason, versionBytes, actorId, cancellationToken);
        return res.Success ? Results.Ok(res) : Problem(400, res.ErrorMessage ?? "Transition failed.");
    }

    private static async Task<IResult> UnpublishAsync(
        Guid productId,
        [FromBody] WorkflowRequest request,
        ClaimsPrincipal user,
        IProductWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var res = await workflowService.TransitionAsync(productId, ProductWorkflowStatus.Unpublished, request.Reason, versionBytes, actorId, cancellationToken);
        return res.Success ? Results.Ok(res) : Problem(400, res.ErrorMessage ?? "Transition failed.");
    }

    private static async Task<IResult> ArchiveAsync(
        Guid productId,
        [FromBody] WorkflowRequest request,
        ClaimsPrincipal user,
        IProductWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var res = await workflowService.TransitionAsync(productId, ProductWorkflowStatus.Archived, request.Reason, versionBytes, actorId, cancellationToken);
        return res.Success ? Results.Ok(res) : Problem(400, res.ErrorMessage ?? "Transition failed.");
    }

    private static async Task<IResult> RestoreDraftAsync(
        Guid productId,
        [FromBody] WorkflowRequest request,
        ClaimsPrincipal user,
        IProductWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var res = await workflowService.TransitionAsync(productId, ProductWorkflowStatus.Draft, request.Reason, versionBytes, actorId, cancellationToken);
        return res.Success ? Results.Ok(res) : Problem(400, res.ErrorMessage ?? "Transition failed.");
    }

    private static async Task<IResult> DuplicateAsync(
        Guid productId,
        [FromBody] DuplicateApiRequest request,
        ClaimsPrincipal user,
        IProductDuplicationService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var command = new DuplicateProductCommand(
            productId, request.NewSku, request.CopyTranslations, request.CopyCategories,
            request.CopyAttributes, request.CopyTags, request.CopyVariants, request.CopyMediaRelations,
            request.CopySeoFields, request.CopyRelatedProducts, actorId);

        var result = await service.DuplicateAsync(command, cancellationToken);
        return Results.Created($"/api/admin/products/{result.NewProductId}", result);
    }

    private static async Task<IResult> GetRevisionsAsync(
        Guid productId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IProductRevisionService service,
        CancellationToken cancellationToken)
    {
        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;
        var list = await service.GetRevisionsAsync(productId, page: p, pageSize: ps, cancellationToken: cancellationToken);
        return Results.Ok(list);
    }

    private static async Task<IResult> GetRevisionDetailAsync(
        Guid productId,
        Guid revisionId,
        IProductRevisionService service,
        CancellationToken cancellationToken)
    {
        var rev = await service.GetRevisionAsync(productId, revisionId, cancellationToken);
        return rev is null ? Results.NotFound() : Results.Ok(rev);
    }

    private static async Task<IResult> RestoreRevisionAsync(
        Guid productId,
        Guid revisionId,
        [FromBody] RestoreRevisionApiRequest request,
        ClaimsPrincipal user,
        IProductRevisionRestoreService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var res = await service.RestoreRevisionAsync(productId, revisionId, request.Reason, versionBytes, actorId, cancellationToken);
        return res.Success ? Results.Ok(res) : Problem(400, res.ErrorMessage ?? "Restore failed.");
    }

    private static async Task<IResult> BulkPreviewAsync(
        [FromBody] ProductBulkPreviewCommand command,
        IProductBulkOperationService service,
        CancellationToken cancellationToken)
    {
        var preview = await service.PreviewAsync(command, cancellationToken);
        return Results.Ok(preview);
    }

    private static async Task<IResult> BulkExecuteAsync(
        [FromBody] ProductBulkExecuteCommand command,
        ClaimsPrincipal user,
        IProductBulkOperationService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var fullCommand = command with { ActorUserId = actorId };
        var res = await service.ExecuteAsync(fullCommand, cancellationToken);
        return Results.Accepted($"/api/admin/products/bulk-operations/{res.BulkOperationId}", res);
    }

    private static async Task<IResult> GetBulkOperationStatusAsync(
        Guid operationId,
        IProductBulkOperationService service,
        CancellationToken cancellationToken)
    {
        var detail = await service.GetOperationAsync(operationId, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> DownloadBulkErrorsCsvAsync(
        Guid operationId,
        IProductBulkOperationService service,
        CancellationToken cancellationToken)
    {
        var bytes = await service.ExportErrorsCsvAsync(operationId, cancellationToken);
        return Results.File(bytes, "text/csv; charset=utf-8", $"BulkErrors_{operationId}.csv");
    }

    private static async Task<IResult> GetProductQualityAsync(
        Guid productId,
        IProductQualityService service,
        CancellationToken cancellationToken)
    {
        var res = await service.EvaluateAsync(productId, cancellationToken);
        return Results.Ok(res);
    }

    private static async Task<IResult> RecalculateProductQualityAsync(
        Guid productId,
        IProductQualityService service,
        CancellationToken cancellationToken)
    {
        var res = await service.EvaluateAsync(productId, cancellationToken);
        return Results.Ok(res);
    }

    private static async Task<IResult> GetQualityDashboardAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IProductQualityService service,
        CancellationToken cancellationToken)
    {
        var filter = new ProductQualityFilter(Page: page <= 0 ? 1 : page, PageSize: pageSize <= 0 ? 20 : pageSize);
        var dash = await service.GetDashboardAsync(filter, cancellationToken);
        return Results.Ok(dash);
    }

    private static async Task<IResult> RecalculateBatchQualityAsync(
        [FromBody] IReadOnlyList<Guid> productIds,
        IProductQualityService service,
        CancellationToken cancellationToken)
    {
        var summary = await service.EvaluateBatchAsync(productIds, cancellationToken);
        return Results.Ok(summary);
    }

    private static async Task<IResult> ReorderCategoryProductsAsync(
        Guid categoryId,
        [FromBody] ReorderApiRequest request,
        ClaimsPrincipal user,
        ICategoryProductOrderingService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actorId)) return Problem(403, "Authenticated user required.");
        var versionBytes = Convert.FromBase64String(request.RowVersion);
        var command = new ReorderCategoryProductsCommand(categoryId, request.Items, versionBytes, actorId);
        await service.ReorderAsync(command, cancellationToken);
        return Results.NoContent();
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    private static IResult Problem(int statusCode, string detail) => Results.Problem(statusCode: statusCode, detail: detail);
    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "Service unavailable");

    private sealed record WorkflowRequest(string RowVersion, string? Reason);
    private sealed record DuplicateApiRequest(string NewSku, bool CopyTranslations = true, bool CopyCategories = true, bool CopyAttributes = true, bool CopyTags = true, bool CopyVariants = false, bool CopyMediaRelations = true, bool CopySeoFields = false, bool CopyRelatedProducts = false);
    private sealed record RestoreRevisionApiRequest(string RowVersion, string Reason);
    private sealed record ReorderApiRequest(IReadOnlyList<ReorderCategoryProductItemDto> Items, string RowVersion);
}
