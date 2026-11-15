using MFCMP.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Data;

public sealed class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
    : IdentityDbContext<PortalUser>(options)
{
    public DbSet<Vault> Vaults => Set<Vault>();
    public DbSet<VaultHealthCheck> VaultHealthChecks => Set<VaultHealthCheck>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Vault>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.CloudHost).HasMaxLength(512).IsRequired();
            e.Property(x => x.Organization).HasMaxLength(160);
            e.Property(x => x.Environment).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.VaultGuid).IsUnique();
            e.HasIndex(x => new { x.Environment, x.MonitoringEnabled });
        });
        builder.Entity<VaultHealthCheck>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.Property(x => x.AuthenticationStatus).HasMaxLength(40).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1000);
            e.HasIndex(x => new { x.VaultId, x.CheckedAtUtc });
            e.HasOne(x => x.Vault).WithMany(x => x.HealthChecks).HasForeignKey(x => x.VaultId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
