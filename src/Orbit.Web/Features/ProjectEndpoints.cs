using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Orbit.Web.Features;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId}/projects").RequireAuthorization();
        group.MapGet("/", async Task<Results<Ok<IReadOnlyList<ProjectResponse>>, NotFound>> (
            string organizationId, ClaimsPrincipal user, IProjectService service, CancellationToken cancellationToken) =>
        {
            var projects = await service.ListAsync(user, organizationId, cancellationToken);
            return projects is null ? TypedResults.NotFound() : TypedResults.Ok(projects);
        }).WithName("ListProjects").WithSummary("List up to 100 missions in an authorized fleet.");

        group.MapGet("/{projectId}", async Task<Results<Ok<ProjectResponse>, NotFound>> (
            string organizationId, string projectId, ClaimsPrincipal user, IProjectService service,
            CancellationToken cancellationToken) =>
        {
            var project = await service.GetAsync(user, organizationId, projectId, cancellationToken);
            return project is null ? TypedResults.NotFound() : TypedResults.Ok(project);
        }).WithName("GetProject").WithSummary("Read a mission without crossing fleet boundaries.");
    }
}
