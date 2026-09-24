using System.Security.Claims;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Content;

internal static class AdvancedCmsEndpoints
{
    public static void MapAdvancedCmsEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        // 1. References
        var refGroup = endpoints.MapGroup("/api/admin/references");
        if (authenticationConfigured) refGroup.RequireAuthorization("ReferencesRead");

        refGroup.MapGet("/", async (
            [FromQuery] ContentWorkflowStatus? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IReferenceProjectService service,
            CancellationToken cancellationToken) => Results.Ok(await service.GetReferencesAsync(status, page, pageSize, cancellationToken)));

        refGroup.MapGet("/{referenceId:guid}", async (Guid referenceId, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            var res = await service.GetReferenceByIdAsync(referenceId, cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        refGroup.MapPost("/", async ([FromBody] CreateReferenceProjectCommand command, ClaimsPrincipal user, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ActorUserId = GetUserId(user) };
            var res = await service.CreateReferenceAsync(full, cancellationToken);
            return Results.Created($"/api/admin/references/{res.Id}", res);
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapPut("/{referenceId:guid}", async (Guid referenceId, [FromBody] UpdateReferenceProjectCommand command, ClaimsPrincipal user, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            var full = command with { Id = referenceId, ActorUserId = GetUserId(user) };
            var res = await service.UpdateReferenceAsync(full, cancellationToken);
            return Results.Ok(res);
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapPost("/{referenceId:guid}/publish", async (Guid referenceId, [FromBody] WorkflowTransitionRequest req, ClaimsPrincipal user, IContentWorkflowService service, CancellationToken cancellationToken) =>
        {
            var res = await service.TransitionAsync("ReferenceProject", referenceId, ContentWorkflowStatus.Published, null, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("ReferencesPublish");

        refGroup.MapPost("/{referenceId:guid}/schedule", async (Guid referenceId, [FromBody] ScheduleTransitionRequest req, ClaimsPrincipal user, IContentWorkflowService service, CancellationToken cancellationToken) =>
        {
            var res = await service.TransitionAsync("ReferenceProject", referenceId, ContentWorkflowStatus.Scheduled, req.PublishAt, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("ContentSchedule");

        refGroup.MapPost("/{referenceId:guid}/unpublish", async (Guid referenceId, [FromBody] WorkflowTransitionRequest req, ClaimsPrincipal user, IContentWorkflowService service, CancellationToken cancellationToken) =>
        {
            var res = await service.TransitionAsync("ReferenceProject", referenceId, ContentWorkflowStatus.Unpublished, null, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapPost("/{referenceId:guid}/archive", async (Guid referenceId, [FromBody] WorkflowTransitionRequest req, ClaimsPrincipal user, IContentWorkflowService service, CancellationToken cancellationToken) =>
        {
            var res = await service.TransitionAsync("ReferenceProject", referenceId, ContentWorkflowStatus.Archived, null, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("ReferencesArchive");

        refGroup.MapPost("/{referenceId:guid}/media", async (Guid referenceId, [FromBody] AddReferenceProjectMediaCommand command, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ReferenceProjectId = referenceId };
            await service.AddMediaAsync(full, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapDelete("/{referenceId:guid}/media/{mediaId:guid}", async (Guid referenceId, Guid mediaId, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            await service.RemoveMediaAsync(referenceId, mediaId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapPut("/{referenceId:guid}/media/reorder", async (Guid referenceId, [FromBody] ReorderCmsItemsCommand command, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            await service.ReorderMediaAsync(referenceId, command, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapPost("/{referenceId:guid}/products", async (Guid referenceId, [FromBody] AddReferenceProjectProductCommand command, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ReferenceProjectId = referenceId };
            await service.AddProductAsync(full, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapDelete("/{referenceId:guid}/products/{productId:guid}", async (Guid referenceId, Guid productId, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            await service.RemoveProductAsync(referenceId, productId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        refGroup.MapPut("/{referenceId:guid}/products/reorder", async (Guid referenceId, [FromBody] ReorderCmsItemsCommand command, IReferenceProjectService service, CancellationToken cancellationToken) =>
        {
            await service.ReorderProductsAsync(referenceId, command, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        // 2. Customer Logos
        var logoGroup = endpoints.MapGroup("/api/admin/customer-logos");
        if (authenticationConfigured) logoGroup.RequireAuthorization("ReferencesRead");

        logoGroup.MapGet("/", async (ICustomerLogoService service, CancellationToken cancellationToken) => Results.Ok(await service.GetLogosAsync(false, cancellationToken)));
        logoGroup.MapGet("/{logoId:guid}", async (Guid logoId, ICustomerLogoService service, CancellationToken cancellationToken) =>
        {
            var res = await service.GetLogoByIdAsync(logoId, cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        logoGroup.MapPost("/", async ([FromBody] CreateCustomerLogoCommand command, ClaimsPrincipal user, ICustomerLogoService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ActorUserId = GetUserId(user) };
            var res = await service.CreateLogoAsync(full, cancellationToken);
            return Results.Created($"/api/admin/customer-logos/{res.Id}", res);
        }).RequireAuthorization("ReferencesManage");

        logoGroup.MapPut("/{logoId:guid}", async (Guid logoId, [FromBody] UpdateCustomerLogoCommand command, ClaimsPrincipal user, ICustomerLogoService service, CancellationToken cancellationToken) =>
        {
            var full = command with { Id = logoId, ActorUserId = GetUserId(user) };
            var res = await service.UpdateLogoAsync(full, cancellationToken);
            return Results.Ok(res);
        }).RequireAuthorization("ReferencesManage");

        logoGroup.MapPost("/{logoId:guid}/archive", async (Guid logoId, ClaimsPrincipal user, ICustomerLogoService service, CancellationToken cancellationToken) =>
        {
            await service.ArchiveLogoAsync(logoId, GetUserId(user), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        logoGroup.MapPut("/reorder", async ([FromBody] ReorderCmsItemsCommand command, ICustomerLogoService service, CancellationToken cancellationToken) =>
        {
            await service.ReorderLogosAsync(command, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ReferencesManage");

        // 3. Showrooms
        var showroomGroup = endpoints.MapGroup("/api/admin/showrooms");
        if (authenticationConfigured) showroomGroup.RequireAuthorization("ShowroomRead");

        showroomGroup.MapGet("/", async ([FromQuery] ContentWorkflowStatus? status, [FromQuery] int page, [FromQuery] int pageSize, IShowroomService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetShowroomsAsync(status, page, pageSize, cancellationToken)));

        showroomGroup.MapGet("/{showroomId:guid}", async (Guid showroomId, IShowroomService service, CancellationToken cancellationToken) =>
        {
            var res = await service.GetShowroomByIdAsync(showroomId, cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        showroomGroup.MapPost("/", async ([FromBody] CreateShowroomCommand command, ClaimsPrincipal user, IShowroomService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ActorUserId = GetUserId(user) };
            var res = await service.CreateShowroomAsync(full, cancellationToken);
            return Results.Created($"/api/admin/showrooms/{res.Id}", res);
        }).RequireAuthorization("ShowroomManage");

        showroomGroup.MapPut("/{showroomId:guid}", async (Guid showroomId, [FromBody] UpdateShowroomCommand command, ClaimsPrincipal user, IShowroomService service, CancellationToken cancellationToken) =>
        {
            var full = command with { Id = showroomId, ActorUserId = GetUserId(user) };
            var res = await service.UpdateShowroomAsync(full, cancellationToken);
            return Results.Ok(res);
        }).RequireAuthorization("ShowroomManage");

        showroomGroup.MapPost("/{showroomId:guid}/publish", async (Guid showroomId, [FromBody] WorkflowTransitionRequest req, ClaimsPrincipal user, IContentWorkflowService service, CancellationToken cancellationToken) =>
        {
            var res = await service.TransitionAsync("Showroom", showroomId, ContentWorkflowStatus.Published, null, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("ShowroomPublish");

        showroomGroup.MapPost("/{showroomId:guid}/hotspots", async (Guid showroomId, [FromBody] CreateShowroomHotspotCommand command, IShowroomService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ShowroomId = showroomId };
            var res = await service.CreateHotspotAsync(full, cancellationToken);
            return Results.Created($"/api/admin/showrooms/{showroomId}/hotspots/{res.Id}", res);
        }).RequireAuthorization("ShowroomManage");

        showroomGroup.MapPut("/{showroomId:guid}/hotspots/{hotspotId:guid}", async (Guid showroomId, Guid hotspotId, [FromBody] UpdateShowroomHotspotCommand command, IShowroomService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ShowroomId = showroomId, HotspotId = hotspotId };
            var res = await service.UpdateHotspotAsync(full, cancellationToken);
            return Results.Ok(res);
        }).RequireAuthorization("ShowroomManage");

        showroomGroup.MapDelete("/{showroomId:guid}/hotspots/{hotspotId:guid}", async (Guid showroomId, Guid hotspotId, IShowroomService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteHotspotAsync(showroomId, hotspotId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("ShowroomManage");

        // 4. Banners
        var bannerGroup = endpoints.MapGroup("/api/admin/banners");
        if (authenticationConfigured) bannerGroup.RequireAuthorization("BannersRead");

        bannerGroup.MapGet("/", async ([FromQuery] Guid? groupId, [FromQuery] BannerPlacement? placement, [FromQuery] ContentWorkflowStatus? status, IBannerManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetBannersAsync(groupId, placement, status, cancellationToken)));

        bannerGroup.MapGet("/{bannerId:guid}", async (Guid bannerId, IBannerManagementService service, CancellationToken cancellationToken) =>
        {
            var res = await service.GetBannerByIdAsync(bannerId, cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        bannerGroup.MapPost("/", async ([FromBody] CreateBannerCommand command, ClaimsPrincipal user, IBannerManagementService service, CancellationToken cancellationToken) =>
        {
            var full = command with { ActorUserId = GetUserId(user) };
            var res = await service.CreateBannerAsync(full, cancellationToken);
            return Results.Created($"/api/admin/banners/{res.Id}", res);
        }).RequireAuthorization("BannersManage");

        bannerGroup.MapPut("/{bannerId:guid}", async (Guid bannerId, [FromBody] UpdateBannerCommand command, ClaimsPrincipal user, IBannerManagementService service, CancellationToken cancellationToken) =>
        {
            var full = command with { Id = bannerId, ActorUserId = GetUserId(user) };
            var res = await service.UpdateBannerAsync(full, cancellationToken);
            return Results.Ok(res);
        }).RequireAuthorization("BannersManage");

        bannerGroup.MapPost("/{bannerId:guid}/publish", async (Guid bannerId, [FromBody] WorkflowTransitionRequest req, ClaimsPrincipal user, IContentWorkflowService service, CancellationToken cancellationToken) =>
        {
            var res = await service.TransitionAsync("Banner", bannerId, ContentWorkflowStatus.Published, null, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("BannersPublish");

        // 5. Site Settings
        var settingsGroup = endpoints.MapGroup("/api/admin/settings");
        if (authenticationConfigured) settingsGroup.RequireAuthorization("SettingsRead");

        settingsGroup.MapGet("/", async ([FromQuery] SiteSettingCategory? category, [FromQuery] bool? isPublic, ISiteSettingService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllSettingsAsync(category, isPublic, cancellationToken)));

        settingsGroup.MapGet("/{category}", async (SiteSettingCategory category, ISiteSettingService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllSettingsAsync(category, null, cancellationToken)));

        settingsGroup.MapPut("/", async ([FromBody] UpdateSiteSettingsRequest req, ClaimsPrincipal user, ISiteSettingService service, CancellationToken cancellationToken) =>
        {
            var command = new UpdateSiteSettingsCommand(req.Settings.Select(s => new UpdateSiteSettingItemDto(s.Key, s.Value, Convert.FromBase64String(s.RowVersion))).ToList(), req.Reason, GetUserId(user));
            await service.UpdateSettingsAsync(command, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("SettingsManage");

        // 6. Footer
        var footerGroup = endpoints.MapGroup("/api/admin/footer");
        if (authenticationConfigured) footerGroup.RequireAuthorization("SettingsManage");

        footerGroup.MapGet("/columns", async (IFooterManagementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetFooterColumnsAsync(false, cancellationToken)));

        footerGroup.MapPost("/columns", async ([FromBody] CreateFooterColumnCommand command, IFooterManagementService service, CancellationToken cancellationToken) =>
            Results.Created("/api/admin/footer/columns", await service.CreateColumnAsync(command, cancellationToken)));

        footerGroup.MapPut("/columns/{columnId:guid}", async (Guid columnId, [FromBody] UpdateFooterColumnCommand command, IFooterManagementService service, CancellationToken cancellationToken) =>
        {
            var full = command with { Id = columnId };
            return Results.Ok(await service.UpdateColumnAsync(full, cancellationToken));
        });

        footerGroup.MapPost("/columns/{columnId:guid}/links", async (Guid columnId, [FromBody] CreateFooterLinkCommand command, IFooterManagementService service, CancellationToken cancellationToken) =>
        {
            var full = command with { FooterColumnId = columnId };
            return Results.Created("/api/admin/footer/columns", await service.CreateLinkAsync(full, cancellationToken));
        });

        // 7. CMS Revisions & Preview
        var cmsGroup = endpoints.MapGroup("/api/admin/cms");
        if (authenticationConfigured) cmsGroup.RequireAuthorization("ContentHistoryRead");

        cmsGroup.MapGet("/{entityType}/{entityId:guid}/revisions", async (string entityType, Guid entityId, [FromQuery] int page, [FromQuery] int pageSize, ICmsRevisionService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetRevisionsAsync(entityType, entityId, page, pageSize, cancellationToken)));

        cmsGroup.MapGet("/{entityType}/{entityId:guid}/revisions/{revisionId:guid}", async (string entityType, Guid entityId, Guid revisionId, ICmsRevisionService service, CancellationToken cancellationToken) =>
        {
            var res = await service.GetRevisionAsync(entityType, entityId, revisionId, cancellationToken);
            return res is null ? Results.NotFound() : Results.Ok(res);
        });

        cmsGroup.MapPost("/{entityType}/{entityId:guid}/revisions/{revisionId:guid}/restore", async (string entityType, Guid entityId, Guid revisionId, [FromBody] RestoreRevisionRequest req, ClaimsPrincipal user, ICmsRevisionRestoreService service, CancellationToken cancellationToken) =>
        {
            var res = await service.RestoreRevisionAsync(entityType, entityId, revisionId, req.Reason, Convert.FromBase64String(req.RowVersion), GetUserId(user), cancellationToken);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        }).RequireAuthorization("ContentHistoryRestore");

        cmsGroup.MapPost("/{entityType}/{entityId:guid}/preview-token", async (string entityType, Guid entityId, [FromBody] CreatePreviewTokenApiRequest req, ICmsPreviewTokenService service, CancellationToken cancellationToken) =>
        {
            var command = new CreatePreviewTokenCommand(entityType, entityId, req.LanguageCode ?? "tr");
            var res = await service.CreatePreviewTokenAsync(command, cancellationToken);
            return Results.Ok(res);
        }).RequireAuthorization("ContentPreview");
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private sealed record WorkflowTransitionRequest(string RowVersion, string? Reason);
    private sealed record ScheduleTransitionRequest(DateTimeOffset PublishAt, string RowVersion, string? Reason);
    private sealed record UpdateSiteSettingApiItem(string Key, string? Value, string RowVersion);
    private sealed record UpdateSiteSettingsRequest(IReadOnlyList<UpdateSiteSettingApiItem> Settings, string? Reason);
    private sealed record RestoreRevisionRequest(string RowVersion, string Reason);
    private sealed record CreatePreviewTokenApiRequest(string? LanguageCode);
}
