using System.Collections.Concurrent;
using System.Data;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Identity;

internal sealed class UserPermissionService(EkiphanDbContext db,TimeProvider clock):IUserPermissionService
{
    private static readonly ConcurrentDictionary<Guid,(long Version,UserPermissionSummaryDto Summary)> Cache=[];
    public async Task<bool> HasPermissionAsync(Guid userId,string permission,CancellationToken cancellationToken=default)=>(await GetEffectivePermissionsAsync(userId,cancellationToken)).Contains(permission,StringComparer.Ordinal);
    public async Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(Guid userId,CancellationToken cancellationToken=default)=>(await GetPermissionSummaryAsync(userId,cancellationToken)).EffectivePermissions;
    public async Task<UserPermissionSummaryDto> GetPermissionSummaryAsync(Guid userId,CancellationToken cancellationToken=default)
    {
        var user=await db.AdminUsers.AsNoTracking().Where(x=>x.Id==userId&&x.IsActive).Select(x=>new{x.AuthorizationVersion,Legacy=x.Permissions.Select(p=>p.Permission).ToArray()}).SingleOrDefaultAsync(cancellationToken);
        if(user is null)return new([],[],[],[],[]);
        if(Cache.TryGetValue(userId,out var cached)&&cached.Version==user.AuthorizationVersion)return cached.Summary;
        var roleRows=await(from ur in db.AdminUserRoles join r in db.AdminRoles on ur.RoleId equals r.Id where ur.AdminUserId==userId&&r.IsActive&&!r.IsArchived join rp in db.AdminRolePermissions on r.Id equals rp.RoleId join p in db.AdminPermissions on rp.PermissionId equals p.Id where p.IsActive select new{r.Name,r.IsSystemRole,p.Code}).AsNoTracking().ToListAsync(cancellationToken);
        var overrides=await(from o in db.AdminUserPermissionOverrides join p in db.AdminPermissions on o.PermissionId equals p.Id where o.AdminUserId==userId&&p.IsActive select new{o.OverrideType,o.ExpiresAt,p.Code}).AsNoTracking().ToListAsync(cancellationToken);
        var roles=roleRows.Select(x=>x.Name).Distinct().Order().ToArray();
        var isSuperAdmin=roleRows.Any(x=>x.IsSystemRole&&x.Name=="SuperAdmin");
        var rolePermissions=isSuperAdmin
            ?(await db.AdminPermissions.AsNoTracking().Where(x=>x.IsActive).Select(x=>x.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal)
            :roleRows.Select(x=>x.Code).Concat(AdminPermissionCode.ExpandLegacy(user.Legacy)).ToHashSet(StringComparer.Ordinal);
        var now=clock.GetUtcNow();var active=overrides.Where(x=>!x.ExpiresAt.HasValue||x.ExpiresAt>now).ToArray();
        var allows=active.Where(x=>x.OverrideType==PermissionOverrideType.Allow).Select(x=>x.Code).Distinct().ToArray();var denies=active.Where(x=>x.OverrideType==PermissionOverrideType.Deny).Select(x=>x.Code).Distinct().ToArray();
        rolePermissions.UnionWith(allows);rolePermissions.ExceptWith(denies);var effective=(IReadOnlyCollection<string>)rolePermissions.Order().ToArray();
        var summary=new UserPermissionSummaryDto(roleRows.Select(x=>x.Code).Distinct().ToArray(),allows,denies,effective,roles);Cache[userId]=(user.AuthorizationVersion,summary);
        return summary;
    }
    public void Invalidate(Guid userId)=>Cache.TryRemove(userId,out _);
}

internal sealed class RolePermissionManagementService(EkiphanDbContext db,IUserPermissionService permissions,TimeProvider clock):IRolePermissionManagementService
{
    public async Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken ct)
    {
        var rows=await db.AdminRoles.AsNoTracking().Where(x=>!x.IsArchived).OrderBy(x=>x.Name)
            .Select(x=>new{x.Id,x.Name,x.Description,x.IsSystemRole,x.IsActive,Users=db.AdminUserRoles.Count(u=>u.RoleId==x.Id),Permissions=x.Permissions.Count,x.CreatedAt,x.UpdatedAt,x.RowVersion})
            .ToListAsync(ct);
        return rows.Select(x=>new RoleSummaryDto(x.Id,x.Name,x.Description,x.IsSystemRole,x.IsActive,x.Users,x.Permissions,x.CreatedAt,x.UpdatedAt,Convert.ToBase64String(x.RowVersion))).ToArray();
    }
    public async Task<RoleDetailDto?> GetRoleAsync(Guid id,CancellationToken ct){var role=await db.AdminRoles.AsNoTracking().Include(x=>x.Permissions).SingleOrDefaultAsync(x=>x.Id==id&&!x.IsArchived,ct);return role is null?null:new(Map(role,await db.AdminUserRoles.CountAsync(x=>x.RoleId==id,ct),role.Permissions.Count),await Codes(role.Permissions.Select(x=>x.PermissionId),ct));}
    public async Task<RoleDetailDto> CreateRoleAsync(CreateRoleCommand c,CancellationToken ct){var ids=await PermissionIds(c.PermissionCodes,ct);var role=new AdminRole(Guid.NewGuid(),c.Name,c.Description);role.SetPermissions(ids,c.ActorUserId);if(await db.AdminRoles.AnyAsync(x=>x.NormalizedName==role.NormalizedName,ct))throw new InvalidOperationException("ROLE_NAME_ALREADY_EXISTS");db.AdminRoles.Add(role);await db.SaveChangesAsync(ct);return (await GetRoleAsync(role.Id,ct))!;}
    public async Task<RoleDetailDto> UpdateRoleAsync(UpdateRoleCommand c,CancellationToken ct){var role=await db.AdminRoles.Include(x=>x.Permissions).SingleOrDefaultAsync(x=>x.Id==c.RoleId&&!x.IsArchived,ct)??throw new KeyNotFoundException("ROLE_NOT_FOUND");db.Entry(role).Property(x=>x.RowVersion).OriginalValue=Convert.FromBase64String(c.RowVersion);var ids=await PermissionIds(c.PermissionCodes,ct);if(role.Name=="SuperAdmin"&&(!c.IsActive||!AdminPermissionCode.Granular.All(code=>c.PermissionCodes.Contains(code,StringComparer.Ordinal))))throw new InvalidOperationException("ROLE_IS_SYSTEM_PROTECTED");role.Update(c.Name,c.Description,c.IsActive,c.ActorUserId);role.SetPermissions(ids,c.ActorUserId);await InvalidateRoleUsers(role.Id,ct);await Save(ct);return (await GetRoleAsync(role.Id,ct))!;}
    public async Task<bool> ArchiveRoleAsync(Guid roleId,Guid actor,string reason,CancellationToken ct){if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Reason is required.");var role=await db.AdminRoles.SingleOrDefaultAsync(x=>x.Id==roleId,ct);if(role is null)return false;if(await db.AdminUserRoles.AnyAsync(x=>x.RoleId==roleId,ct))throw new InvalidOperationException("ROLE_HAS_ASSIGNED_USERS");role.Archive(actor);await Save(ct);return true;}
    public async Task<IReadOnlyList<RoleSummaryDto>> GetUserRolesAsync(Guid userId,CancellationToken ct)
    {
        if(!await db.AdminUsers.AnyAsync(x=>x.Id==userId,ct))throw new KeyNotFoundException("USER_NOT_FOUND");
        var ids=db.AdminUserRoles.Where(x=>x.AdminUserId==userId).Select(x=>x.RoleId);
        var rows=await db.AdminRoles.AsNoTracking().Where(x=>ids.Contains(x.Id)).OrderBy(x=>x.Name)
            .Select(x=>new{x.Id,x.Name,x.Description,x.IsSystemRole,x.IsActive,Users=db.AdminUserRoles.Count(u=>u.RoleId==x.Id),Permissions=x.Permissions.Count,x.CreatedAt,x.UpdatedAt,x.RowVersion})
            .ToListAsync(ct);
        return rows.Select(x=>new RoleSummaryDto(x.Id,x.Name,x.Description,x.IsSystemRole,x.IsActive,x.Users,x.Permissions,x.CreatedAt,x.UpdatedAt,Convert.ToBase64String(x.RowVersion))).ToArray();
    }
    public async Task AssignRolesAsync(AssignUserRolesCommand c,CancellationToken ct){await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);var user=await db.AdminUsers.SingleOrDefaultAsync(x=>x.Id==c.UserId&&x.IsActive,ct)??throw new KeyNotFoundException("USER_NOT_FOUND");var roles=await db.AdminRoles.Where(x=>c.RoleIds.Contains(x.Id)&&x.IsActive&&!x.IsArchived).ToListAsync(ct);if(roles.Count!=c.RoleIds.Count)throw new InvalidOperationException("ROLE_NOT_FOUND");var existing=await db.AdminUserRoles.Where(x=>x.AdminUserId==c.UserId&&c.RoleIds.Contains(x.RoleId)).Select(x=>x.RoleId).ToListAsync(ct);if(existing.Count>0)throw new InvalidOperationException("USER_ROLE_ALREADY_ASSIGNED");foreach(var id in c.RoleIds)db.AdminUserRoles.Add(new(c.UserId,id,c.ActorUserId,clock.GetUtcNow()));user.PermissionsChanged(clock.GetUtcNow());Revoke(user.Id);await Save(ct);await tx.CommitAsync(ct);permissions.Invalidate(user.Id);}
    public async Task RemoveRoleAsync(Guid userId,Guid roleId,Guid actor,string reason,CancellationToken ct){if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Reason is required.");await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);var link=await db.AdminUserRoles.SingleOrDefaultAsync(x=>x.AdminUserId==userId&&x.RoleId==roleId,ct)??throw new KeyNotFoundException("USER_ROLE_NOT_ASSIGNED");var role=await db.AdminRoles.SingleAsync(x=>x.Id==roleId,ct);if(role.Name=="SuperAdmin"&&actor==userId)throw new InvalidOperationException("CANNOT_MODIFY_OWN_CRITICAL_ACCESS");if(role.Name=="SuperAdmin"&&!await HasAnotherSuperAdmin(userId,roleId,ct))throw new InvalidOperationException("LAST_SUPER_ADMIN_PROTECTED");db.AdminUserRoles.Remove(link);var user=await db.AdminUsers.SingleAsync(x=>x.Id==userId,ct);user.PermissionsChanged(clock.GetUtcNow());Revoke(userId);await Save(ct);await tx.CommitAsync(ct);permissions.Invalidate(userId);}
    public async Task<UserPermissionSummaryDto> AddOverrideAsync(AddUserPermissionOverrideCommand c,CancellationToken ct){var user=await db.AdminUsers.SingleOrDefaultAsync(x=>x.Id==c.UserId&&x.IsActive,ct)??throw new KeyNotFoundException("USER_NOT_FOUND");var permission=await db.AdminPermissions.SingleOrDefaultAsync(x=>x.Code==c.PermissionCode&&x.IsActive,ct)??throw new KeyNotFoundException("PERMISSION_NOT_FOUND");if(c.OverrideType==PermissionOverrideType.Deny&&await IsSuperAdmin(c.UserId,ct))throw new InvalidOperationException("ROLE_IS_SYSTEM_PROTECTED");if(await db.AdminUserPermissionOverrides.AnyAsync(x=>x.AdminUserId==c.UserId&&x.PermissionId==permission.Id&&x.OverrideType==c.OverrideType,ct))throw new InvalidOperationException("PERMISSION_OVERRIDE_ALREADY_EXISTS");db.AdminUserPermissionOverrides.Add(new(Guid.NewGuid(),c.UserId,permission.Id,c.OverrideType,c.ActorUserId,c.Reason,c.ExpiresAt));user.PermissionsChanged(clock.GetUtcNow());Revoke(user.Id);await Save(ct);permissions.Invalidate(user.Id);return await permissions.GetPermissionSummaryAsync(user.Id,ct);}
    public async Task RemoveOverrideAsync(Guid userId,Guid overrideId,Guid actor,CancellationToken ct){var item=await db.AdminUserPermissionOverrides.SingleOrDefaultAsync(x=>x.Id==overrideId&&x.AdminUserId==userId,ct)??throw new KeyNotFoundException("PERMISSION_OVERRIDE_NOT_FOUND");db.Remove(item);var user=await db.AdminUsers.SingleAsync(x=>x.Id==userId,ct);user.PermissionsChanged(clock.GetUtcNow());Revoke(userId);await Save(ct);permissions.Invalidate(userId);}
    public async Task<IReadOnlyList<PermissionGroupDto>> GetPermissionCatalogAsync(CancellationToken ct)=>(await db.AdminPermissions.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Group).ThenBy(x=>x.Code).Select(x=>new PermissionDto(x.Id,x.Code,x.Name,x.Description,x.Group)).ToListAsync(ct)).GroupBy(x=>x.Group).Select(x=>new PermissionGroupDto(x.Key,x.ToList())).ToArray();
    private async Task<Guid[]> PermissionIds(IEnumerable<string> codes,CancellationToken ct){var list=codes.Distinct(StringComparer.Ordinal).ToArray();if(list.Any(x=>!AdminPermissionCode.Granular.Contains(x)))throw new InvalidOperationException("ROLE_PERMISSION_INVALID");var rows=await db.AdminPermissions.Where(x=>list.Contains(x.Code)&&x.IsActive).Select(x=>x.Id).ToArrayAsync(ct);if(rows.Length!=list.Length)throw new InvalidOperationException("ROLE_PERMISSION_INVALID");return rows;}
    private async Task<string[]> Codes(IEnumerable<Guid> ids,CancellationToken ct){var values=ids.ToArray();return await db.AdminPermissions.Where(x=>values.Contains(x.Id)).Select(x=>x.Code).Order().ToArrayAsync(ct);}
    private async Task InvalidateRoleUsers(Guid roleId,CancellationToken ct){var users=await db.AdminUsers.Where(x=>db.AdminUserRoles.Any(r=>r.RoleId==roleId&&r.AdminUserId==x.Id)).ToListAsync(ct);foreach(var user in users){user.PermissionsChanged(clock.GetUtcNow());Revoke(user.Id);permissions.Invalidate(user.Id);}}
    private void Revoke(Guid userId){foreach(var s in db.AdminSessions.Where(x=>x.AdminUserId==userId&&x.RevokedAt==null))s.Revoke(clock.GetUtcNow());}
    private Task<bool> HasAnotherSuperAdmin(Guid userId,Guid roleId,CancellationToken ct)=>db.AdminUserRoles.AnyAsync(x=>x.RoleId==roleId&&x.AdminUserId!=userId&&db.AdminUsers.Any(u=>u.Id==x.AdminUserId&&u.IsActive),ct);
    private Task<bool> IsSuperAdmin(Guid userId,CancellationToken ct)=>db.AdminUserRoles.AnyAsync(x=>x.AdminUserId==userId&&db.AdminRoles.Any(r=>r.Id==x.RoleId&&r.IsActive&&r.NormalizedName=="SUPERADMIN"),ct);
    private async Task Save(CancellationToken ct){try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){throw new InvalidOperationException("ROLE_CONCURRENCY_CONFLICT");}}
    private static RoleSummaryDto Map(AdminRole x,int users,int permissions)=>new(x.Id,x.Name,x.Description,x.IsSystemRole,x.IsActive,users,permissions,x.CreatedAt,x.UpdatedAt,Convert.ToBase64String(x.RowVersion));
}
