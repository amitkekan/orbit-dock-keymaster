using Microsoft.EntityFrameworkCore;

namespace Orbit.Web.Data;

public static class DemoData
{
    public static async Task InitializeAsync(OrbitDbContext database, CancellationToken cancellationToken = default)
    {
        await database.Database.EnsureCreatedAsync(cancellationToken);
        if (await database.Organizations.AnyAsync(cancellationToken))
        {
            return;
        }

        database.Users.AddRange(
            new AppUser { Id = "alex", Issuer = "orbit-fixture", Subject = "alex" },
            new AppUser { Id = "blair", Issuer = "orbit-fixture", Subject = "blair" },
            new AppUser { Id = "casey", Issuer = "orbit-fixture", Subject = "casey" });
        database.Organizations.AddRange(
            new Organization { Id = "aurora", Name = "Aurora Fleet" },
            new Organization { Id = "nebula", Name = "Nebula Fleet" });
        database.Memberships.AddRange(
            new Membership { OrganizationId = "aurora", UserId = "alex", Role = OrganizationRole.Admin },
            new Membership { OrganizationId = "nebula", UserId = "alex", Role = OrganizationRole.Viewer },
            new Membership { OrganizationId = "aurora", UserId = "blair", Role = OrganizationRole.Editor },
            new Membership { OrganizationId = "aurora", UserId = "casey", Role = OrganizationRole.Viewer },
            new Membership { OrganizationId = "nebula", UserId = "casey", Role = OrganizationRole.Admin });
        database.Projects.AddRange(
            new Project { Id = "lunar-relay", OrganizationId = "aurora", OwnerUserId = "blair", Name = "Lunar Relay" },
            new Project { Id = "deep-space", OrganizationId = "nebula", OwnerUserId = "casey", Name = "Deep Space Survey" });
        await database.SaveChangesAsync(cancellationToken);
    }
}
