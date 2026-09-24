using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Identity;

public sealed class AdminRole : Entity
{
    private readonly List<RolePermissionGrant> _permissions=[];
    private AdminRole() { }
    public AdminRole(Guid id,string name,string? description,bool isSystemRole=false):base(id)
    { SetName(name); Description=Normalize(description,500); IsSystemRole=isSystemRole; }
    public string Name { get; private set; }=string.Empty;
    public string NormalizedName { get; private set; }=string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; }=true;
    public bool IsArchived { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public IReadOnlyCollection<RolePermissionGrant> Permissions=>_permissions;
    public void Update(string name,string? description,bool active,Guid actor)
    { if(IsSystemRole&&!string.Equals(Name,name.Trim(),StringComparison.Ordinal)) throw new InvalidOperationException("System role name cannot be changed."); SetName(name); Description=Normalize(description,500); IsActive=active; UpdatedByUserId=actor; }
    public void SetPermissions(IEnumerable<Guid> permissionIds,Guid actor)
    { _permissions.Clear(); _permissions.AddRange(permissionIds.Distinct().Select(id=>new RolePermissionGrant(Id,id,actor))); UpdatedByUserId=actor; }
    public void Archive(Guid actor){if(IsSystemRole)throw new InvalidOperationException("System roles cannot be archived.");IsArchived=true;IsActive=false;UpdatedByUserId=actor;}
    private void SetName(string value){ArgumentException.ThrowIfNullOrWhiteSpace(value);Name=value.Trim();if(Name.Length>100)throw new ArgumentException("Role name is too long.");NormalizedName=Name.ToUpperInvariant();}
    private static string? Normalize(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value.Trim().Length<=max?value.Trim():throw new ArgumentException("Value is too long.");
}

public sealed class PermissionDefinition : Entity
{
    private PermissionDefinition() { }
    public PermissionDefinition(Guid id,string code,string name,string description,string group):base(id)
    { Code=code;Name=name;Description=description;Group=group; }
    public string Code { get; private set; }=string.Empty;
    public string Name { get; private set; }=string.Empty;
    public string Description { get; private set; }=string.Empty;
    public string Group { get; private set; }=string.Empty;
    public bool IsActive { get; private set; }=true;
    public void Refresh(string name,string description,string group){Name=name;Description=description;Group=group;IsActive=true;}
}

public sealed class AdminUserRole
{
    private AdminUserRole() { }
    public AdminUserRole(Guid userId,Guid roleId,Guid actor,DateTimeOffset at){AdminUserId=userId;RoleId=roleId;AssignedByUserId=actor;AssignedAt=at;}
    public Guid AdminUserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid AssignedByUserId { get; private set; }
}

public sealed class RolePermissionGrant
{
    private RolePermissionGrant() { }
    internal RolePermissionGrant(Guid roleId,Guid permissionId,Guid actor){RoleId=roleId;PermissionId=permissionId;GrantedByUserId=actor;GrantedAt=DateTimeOffset.UtcNow;}
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public Guid GrantedByUserId { get; private set; }
}

public enum PermissionOverrideType { Allow=1,Deny=2 }
public sealed class AdminUserPermissionOverride : Entity
{
    private AdminUserPermissionOverride() { }
    public AdminUserPermissionOverride(Guid id,Guid userId,Guid permissionId,PermissionOverrideType type,Guid actor,string reason,DateTimeOffset? expiresAt):base(id)
    { AdminUserId=userId;PermissionId=permissionId;OverrideType=type;CreatedByUserId=actor;Reason=string.IsNullOrWhiteSpace(reason)?throw new ArgumentException("Reason is required."):reason.Trim();ExpiresAt=expiresAt; }
    public Guid AdminUserId { get; private set; }
    public Guid PermissionId { get; private set; }
    public PermissionOverrideType OverrideType { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string Reason { get; private set; }=string.Empty;
    public bool IsEffective(DateTimeOffset now)=>!ExpiresAt.HasValue||ExpiresAt>now;
}
