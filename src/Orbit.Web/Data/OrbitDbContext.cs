using Microsoft.EntityFrameworkCore;

namespace Orbit.Web.Data;

public enum OrganizationRole { Viewer, Editor, Admin }

public sealed class AppUser
{
    public required string Id { get; set; }
    public required string Issuer { get; set; }
    public required string Subject { get; set; }
}

public sealed class Organization
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

public sealed class Membership
{
    public required string OrganizationId { get; set; }
    public required string UserId { get; set; }
    public OrganizationRole Role { get; set; }
}

public sealed class Project
{
    public required string Id { get; set; }
    public required string OrganizationId { get; set; }
    public required string OwnerUserId { get; set; }
    public required string Name { get; set; }
}

public sealed class OrbitDbContext(DbContextOptions<OrbitDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(user => new { user.Issuer, user.Subject }).IsUnique();
        modelBuilder.Entity<Membership>().HasKey(member => new { member.OrganizationId, member.UserId });
        modelBuilder.Entity<Membership>().Property(member => member.Role).HasConversion<string>();
        modelBuilder.Entity<Membership>().HasOne<Organization>().WithMany()
            .HasForeignKey(member => member.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Membership>().HasOne<AppUser>().WithMany()
            .HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Project>().HasOne<Membership>().WithMany()
            .HasForeignKey(project => new { project.OrganizationId, UserId = project.OwnerUserId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Project>().HasIndex(project => new { project.OrganizationId, project.Id });
    }
}
