using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Orbit.Web.Data;

namespace Orbit.Web.Authorization;

public sealed record OrganizationScope(string OrganizationId, CancellationToken CancellationToken);
public sealed record OrganizationReadRequirement : IAuthorizationRequirement;

public static class OrganizationPermissions
{
    public static bool CanRead(OrganizationRole role) =>
        role is OrganizationRole.Viewer or OrganizationRole.Editor or OrganizationRole.Admin;
}

public sealed class OrganizationReadHandler(OrbitDbContext database)
    : AuthorizationHandler<OrganizationReadRequirement, OrganizationScope>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OrganizationReadRequirement requirement, OrganizationScope resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var identifier = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (identifier is null)
        {
            return;
        }

        var membership = await (
            from member in database.Memberships.AsNoTracking()
            join user in database.Users.AsNoTracking() on member.UserId equals user.Id
            where member.OrganizationId == resource.OrganizationId
                && user.Issuer == identifier.Issuer && user.Subject == identifier.Value
            select member).SingleOrDefaultAsync(resource.CancellationToken);
        if (membership is not null && OrganizationPermissions.CanRead(membership.Role))
        {
            context.Succeed(requirement);
        }
    }
}
