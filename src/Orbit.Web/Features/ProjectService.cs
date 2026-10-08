using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Orbit.Web.Authorization;
using Orbit.Web.Data;

namespace Orbit.Web.Features;

/// <summary>A project visible within an authorized organization.</summary>
public sealed record ProjectResponse(string Id, string OrganizationId, string Name);

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>?> ListAsync(ClaimsPrincipal user, string organizationId, CancellationToken cancellationToken);
    Task<ProjectResponse?> GetAsync(ClaimsPrincipal user, string organizationId, string projectId, CancellationToken cancellationToken);
}

public sealed class ProjectService(OrbitDbContext database, IAuthorizationService authorization) : IProjectService
{
    private async Task<bool> CanReadAsync(ClaimsPrincipal user, string organizationId, CancellationToken cancellationToken)
    {
        var result = await authorization.AuthorizeAsync(user,
            new OrganizationScope(organizationId, cancellationToken), new OrganizationReadRequirement());
        return result.Succeeded;
    }

    public async Task<IReadOnlyList<ProjectResponse>?> ListAsync(
        ClaimsPrincipal user, string organizationId, CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(user, organizationId, cancellationToken))
        {
            return null;
        }

        return await database.Projects.AsNoTracking().Where(project => project.OrganizationId == organizationId)
            .OrderBy(project => project.Id).Take(100)
            .Select(project => new ProjectResponse(project.Id, project.OrganizationId, project.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectResponse?> GetAsync(
        ClaimsPrincipal user, string organizationId, string projectId, CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(user, organizationId, cancellationToken))
        {
            return null;
        }

        return await database.Projects.AsNoTracking()
            .Where(project => project.OrganizationId == organizationId && project.Id == projectId)
            .Select(project => new ProjectResponse(project.Id, project.OrganizationId, project.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
