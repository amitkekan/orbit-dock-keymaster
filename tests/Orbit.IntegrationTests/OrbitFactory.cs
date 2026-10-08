using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orbit.Web.Data;

namespace Orbit.IntegrationTests;

public sealed class OrbitFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly bool useFixtureAuthentication;

    public OrbitFactory(bool useFixtureAuthentication = true)
    {
        this.useFixtureAuthentication = useFixtureAuthentication;
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<OrbitDbContext>)
                || descriptor.ServiceType == typeof(OrbitDbContext)
                || descriptor.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<OrbitDbContext>))
                .ToArray();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
            services.AddDbContext<OrbitDbContext>(options => options.UseSqlite(connection));
            if (useFixtureAuthentication)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Fixture";
                    options.DefaultChallengeScheme = "Fixture";
                }).AddScheme<AuthenticationSchemeOptions, FixtureAuthentication>("Fixture", _ => { });
            }
        });
    }

    public async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await DemoData.InitializeAsync(scope.ServiceProvider.GetRequiredService<OrbitDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
        }
    }
}

public sealed class FixtureAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Subject", out var subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
        var issuer = Request.Headers["X-Test-Issuer"].FirstOrDefault() ?? "orbit-fixture";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject.ToString(), ClaimValueTypes.String, issuer),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
