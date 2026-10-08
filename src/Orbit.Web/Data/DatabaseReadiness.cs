using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orbit.Web.Data;

public sealed class DatabaseReadiness(OrbitDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await database.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database unavailable.");
}

public static class DatabaseReadinessRegistration
{
    public static IHealthChecksBuilder AddDbReadiness(this IHealthChecksBuilder builder) =>
        builder.AddCheck<DatabaseReadiness>("database");
}
