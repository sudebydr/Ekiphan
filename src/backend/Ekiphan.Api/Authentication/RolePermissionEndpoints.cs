using System.Security.Claims;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Authentication;

internal static class RolePermissionEndpoints
{
    public static void MapRolePermissionEndpoints(this IEndpointRouteBuilder endpoints,bool configured)
    {
        var roles=endpoints.MapGroup("/api/admin/roles");var users=endpoints.MapGroup("/api/admin/users");
        if(!configured){roles.MapGet("",()=>Results.Problem(statusCode:503));return;}
        roles.MapGet("",(IRolePermissionManagementService s,CancellationToken ct)=>s.GetRolesAsync(ct)).RequireAuthorization(AdminPermissionCode.Roles.Read).WithSummary("List roles");
        roles.MapGet("/{id:guid}",async(Guid id,IRolePermissionManagementService s,CancellationToken ct)=>(await s.GetRoleAsync(id,ct)) is{} x?Results.Ok(x):Results.NotFound()).RequireAuthorization(AdminPermissionCode.Roles.Read);
        roles.MapPost("",Create).RequireAuthorization(AdminPermissionCode.Roles.Create).WithSummary("Create role");
        roles.MapPut("/{id:guid}",Update).RequireAuthorization(AdminPermissionCode.Roles.Update).WithSummary("Update role and revoke affected sessions");
        roles.MapDelete("/{id:guid}",Archive).RequireAuthorization(AdminPermissionCode.Roles.Delete).WithSummary("Archive custom role");
        users.MapGet("/{id:guid}/roles",(Guid id,IRolePermissionManagementService s,CancellationToken ct)=>s.GetUserRolesAsync(id,ct)).RequireAuthorization(AdminPermissionCode.Users.Read);
        users.MapPost("/{id:guid}/roles",Assign).RequireAuthorization(AdminPermissionCode.Roles.Assign);
        users.MapDelete("/{userId:guid}/roles/{roleId:guid}",RemoveRole).RequireAuthorization(AdminPermissionCode.Roles.Assign);
        users.MapGet("/{id:guid}/permissions",(Guid id,IUserPermissionService s,CancellationToken ct)=>s.GetPermissionSummaryAsync(id,ct)).RequireAuthorization(AdminPermissionCode.Permissions.Read);
        users.MapPost("/{id:guid}/permission-overrides",AddOverride).RequireAuthorization(AdminPermissionCode.Permissions.Assign);
        users.MapDelete("/{id:guid}/permission-overrides/{overrideId:guid}",RemoveOverride).RequireAuthorization(AdminPermissionCode.Permissions.Assign);
        endpoints.MapGet("/api/admin/permissions",(IRolePermissionManagementService s,CancellationToken ct)=>s.GetPermissionCatalogAsync(ct)).RequireAuthorization(AdminPermissionCode.Permissions.Read).WithSummary("Get grouped permission catalog");
    }
    private static async Task<IResult>Create(CreateRoleRequest r,ClaimsPrincipal p,IRolePermissionManagementService s,IValidator<CreateRoleCommand> v,CancellationToken ct){var c=new CreateRoleCommand(r.Name,r.Description,r.PermissionCodes,Actor(p));await v.ValidateAndThrowAsync(c,ct);try{return Results.Created("/api/admin/roles",await s.CreateRoleAsync(c,ct));}catch(Exception e){return Problem(e);}}
    private static async Task<IResult>Update(Guid id,UpdateRoleRequest r,ClaimsPrincipal p,IRolePermissionManagementService s,IValidator<UpdateRoleCommand> v,CancellationToken ct){var c=new UpdateRoleCommand(id,r.Name,r.Description,r.IsActive,r.PermissionCodes,r.RowVersion,Actor(p));await v.ValidateAndThrowAsync(c,ct);try{return Results.Ok(await s.UpdateRoleAsync(c,ct));}catch(Exception e){return Problem(e);}}
    private static async Task<IResult>Archive(Guid id,[FromBody] ArchiveRoleRequest r,ClaimsPrincipal p,IRolePermissionManagementService s,CancellationToken ct){try{return await s.ArchiveRoleAsync(id,Actor(p),r.Reason,ct)?Results.NoContent():Results.NotFound();}catch(Exception e){return Problem(e);}}
    private static async Task<IResult>Assign(Guid id,AssignRolesRequest r,ClaimsPrincipal p,IRolePermissionManagementService s,IValidator<AssignUserRolesCommand> v,CancellationToken ct){var c=new AssignUserRolesCommand(id,r.RoleIds,r.Reason,Actor(p));await v.ValidateAndThrowAsync(c,ct);try{await s.AssignRolesAsync(c,ct);return Results.NoContent();}catch(Exception e){return Problem(e);}}
    private static async Task<IResult>RemoveRole(Guid userId,Guid roleId,string reason,ClaimsPrincipal p,IRolePermissionManagementService s,CancellationToken ct){try{await s.RemoveRoleAsync(userId,roleId,Actor(p),reason,ct);return Results.NoContent();}catch(Exception e){return Problem(e);}}
    private static async Task<IResult>AddOverride(Guid id,AddOverrideRequest r,ClaimsPrincipal p,IRolePermissionManagementService s,IValidator<AddUserPermissionOverrideCommand> v,CancellationToken ct){var c=new AddUserPermissionOverrideCommand(id,r.PermissionCode,r.OverrideType,r.ExpiresAt,r.Reason,Actor(p));await v.ValidateAndThrowAsync(c,ct);try{return Results.Ok(await s.AddOverrideAsync(c,ct));}catch(Exception e){return Problem(e);}}
    private static async Task<IResult>RemoveOverride(Guid id,Guid overrideId,ClaimsPrincipal p,IRolePermissionManagementService s,CancellationToken ct){try{await s.RemoveOverrideAsync(id,overrideId,Actor(p),ct);return Results.NoContent();}catch(Exception e){return Problem(e);}}
    private static Guid Actor(ClaimsPrincipal p)=>Guid.TryParse(p.FindFirstValue("sub"),out var id)?id:throw new UnauthorizedAccessException();
    private static IResult Problem(Exception e)=>e switch{KeyNotFoundException=>Results.Problem(statusCode:404,title:e.Message),InvalidOperationException=>Results.Problem(statusCode:409,title:e.Message),_=>Results.Problem(statusCode:400,title:e.Message)};
    private sealed record CreateRoleRequest(string Name,string? Description,IReadOnlyList<string> PermissionCodes);
    private sealed record UpdateRoleRequest(string Name,string? Description,bool IsActive,IReadOnlyList<string> PermissionCodes,string RowVersion);
    private sealed record ArchiveRoleRequest(string Reason);
    private sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds,string Reason);
    private sealed record AddOverrideRequest(string PermissionCode,PermissionOverrideType OverrideType,DateTimeOffset? ExpiresAt,string Reason);
}
