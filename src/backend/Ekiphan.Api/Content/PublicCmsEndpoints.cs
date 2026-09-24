using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Content;

internal static class PublicCmsEndpoints
{
    public static void MapPublicCmsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/public");

        publicGroup.MapGet("/references", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IReferenceProjectService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetReferencesAsync(ContentWorkflowStatus.Published, page, pageSize, cancellationToken)));

        publicGroup.MapGet("/references/{slug}", async (
            string slug,
            [FromQuery] string? lang,
            IReferenceProjectService service,
            CancellationToken cancellationToken) =>
        {
            var res = await service.GetReferenceBySlugAsync(slug, lang ?? "tr", cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        publicGroup.MapGet("/showrooms", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IShowroomService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetShowroomsAsync(ContentWorkflowStatus.Published, page, pageSize, cancellationToken)));

        publicGroup.MapGet("/showrooms/{slug}", async (
            string slug,
            [FromQuery] string? lang,
            IShowroomService service,
            CancellationToken cancellationToken) =>
        {
            var res = await service.GetShowroomBySlugAsync(slug, lang ?? "tr", cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        publicGroup.MapGet("/customer-logos", async (
            ICustomerLogoService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetLogosAsync(true, cancellationToken)));

        publicGroup.MapGet("/banners/{placement}", async (
            BannerPlacement placement,
            IBannerManagementService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetBannersAsync(null, placement, ContentWorkflowStatus.Published, cancellationToken)));

        publicGroup.MapGet("/settings", async (
            IPublicSiteSettingService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetPublicSettingsAsync(cancellationToken)));

        publicGroup.MapGet("/footer", async (
            IFooterManagementService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetFooterColumnsAsync(true, cancellationToken)));

        // Preview Endpoint
        endpoints.MapGet("/api/preview/{entityType}/{entityId:guid}", async (
            string entityType,
            Guid entityId,
            [FromQuery] string token,
            ICmsPreviewTokenService tokenService,
            IReferenceProjectService refService,
            IShowroomService showroomService,
            CancellationToken cancellationToken) =>
        {
            var valid = await tokenService.ValidatePreviewTokenAsync(token, entityType, entityId, cancellationToken);
            if (!valid) return Results.Problem(statusCode: 403, detail: "Invalid or expired preview token.");

            var type = entityType.Trim().ToLowerInvariant();
            if (type is "reference" or "referenceproject")
            {
                var res = await refService.GetReferenceByIdAsync(entityId, cancellationToken);
                return res is null ? Results.NotFound() : Results.Ok(res);
            }
            else if (type is "showroom")
            {
                var res = await showroomService.GetShowroomByIdAsync(entityId, cancellationToken);
                return res is null ? Results.NotFound() : Results.Ok(res);
            }

            return Results.NotFound();
        });
    }
}
