using System.Security.Claims;
using Ekiphan.Application.Identity;
using Ekiphan.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ekiphan.Api.Authentication;

internal sealed record PermissionRequirement(string Permission):IAuthorizationRequirement;
internal sealed class PermissionAuthorizationHandler(IUserPermissionService service):AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,PermissionRequirement requirement)
    {
        if(Guid.TryParse(context.User.FindFirstValue("sub"),out var userId)&&await service.HasPermissionAsync(userId,requirement.Permission,CancellationToken.None))context.Succeed(requirement);
    }
}
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options):DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var configured=await base.GetPolicyAsync(policyName);if(configured is not null)return configured;
        return AdminPermissionCode.Granular.Contains(policyName)?new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(policyName)).Build():null;
    }
}
