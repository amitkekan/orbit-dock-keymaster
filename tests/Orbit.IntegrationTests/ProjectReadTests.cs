using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orbit.Web.Data;
using Orbit.Web.Features;

namespace Orbit.IntegrationTests;

[TestClass]
public sealed class ProjectReadTests
{
    [TestMethod]
    public async Task ListProjects_NormalAuthentication_IgnoresTestHeaders()
    {
        using var factory = new OrbitFactory(useFixtureAuthentication: false);
        await factory.SeedAsync();
        using var client = CreateClient(factory, "alex");
        var response = await client.GetAsync("/api/organizations/aurora/projects");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsNull(response.Headers.Location);
    }

    [TestMethod]
    public async Task GetProject_CrossTenantId_ReturnsNotFound()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory, "blair");
        var response = await client.GetAsync("/api/organizations/aurora/projects/deep-space");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ListProjects_Anonymous_ReturnsUnauthorized()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory);
        var response = await client.GetAsync("/api/organizations/aurora/projects");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ListProjects_NonMemberWithAdminClaim_ReturnsNotFound()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory, "blair");
        var response = await client.GetAsync("/api/organizations/nebula/projects");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ListProjects_Member_ReturnsOnlyRequestedTenant()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory, "alex");
        foreach (var organization in new[] { "aurora", "nebula" })
        {
            var projects = await client.GetFromJsonAsync<ProjectResponse[]>($"/api/organizations/{organization}/projects");
            Assert.IsNotNull(projects);
            Assert.HasCount(1, projects);
            Assert.AreEqual(organization, projects[0].OrganizationId);
        }
    }

    [TestMethod]
    public async Task GetProject_Member_ReturnsProject()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory, "blair");
        var project = await client.GetFromJsonAsync<ProjectResponse>("/api/organizations/aurora/projects/lunar-relay");
        Assert.IsNotNull(project);
        Assert.AreEqual("lunar-relay", project.Id);
    }

    [TestMethod]
    public async Task ListProjects_SameSubjectDifferentIssuer_ReturnsNotFound()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory, "alex");
        client.DefaultRequestHeaders.Add("X-Test-Issuer", "untrusted");
        var response = await client.GetAsync("/api/organizations/aurora/projects");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ListProjects_MembershipRemoved_DeniesNextRequest()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        using var client = CreateClient(factory, "alex");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/organizations/nebula/projects")).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<OrbitDbContext>();
            var membership = await database.Memberships.SingleAsync(member =>
                member.UserId == "alex" && member.OrganizationId == "nebula");
            database.Memberships.Remove(membership);
            await database.SaveChangesAsync();
        }
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/organizations/nebula/projects")).StatusCode);
    }

    [TestMethod]
    public async Task SaveProject_OwnerOutsideOrganization_RejectsForeignKey()
    {
        using var factory = new OrbitFactory();
        await factory.SeedAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<OrbitDbContext>();
        database.Projects.Add(new Project
        {
            Id = "invalid", OrganizationId = "nebula", OwnerUserId = "blair", Name = "Invalid"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [TestMethod]
    public async Task LandingPage_Anonymous_ShowsPrelaunchStatus()
    {
        using var factory = new OrbitFactory();
        using var client = CreateClient(factory);
        var html = await client.GetStringAsync("/");
        StringAssert.Contains(html, "<title>Orbit Dock Keymaster | Mission Control</title>");
        StringAssert.Contains(html, "<h1>Orbit Dock Keymaster.</h1>");
        StringAssert.Contains(html, "Real sign-in");
    }

    private static HttpClient CreateClient(OrbitFactory factory, string? subject = null)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (subject is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        }
        return client;
    }
}
