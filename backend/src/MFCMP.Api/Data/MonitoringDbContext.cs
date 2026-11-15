using MFCMP.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Data;

public sealed class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
    : IdentityDbContext<PortalUser>(options)
{
    public DbSet<Vault> Vaults => Set<Vault>();
    public DbSet<VaultHealthCheck> VaultHealthChecks => Set<VaultHealthCheck>();
    public DbSet<VaultDailyObjectStatistic> VaultDailyObjectStatistics => Set<VaultDailyObjectStatistic>();
    public DbSet<Incident> Incidents => Set<Incident>();

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
            e.Property(x => x.AuthenticationMode).HasMaxLength(24).IsRequired();
            e.Property(x => x.CredentialSecretUri).HasMaxLength(2048);
            e.Property(x => x.AuthenticationPlugin).HasMaxLength(160);
            e.Property(x => x.AuthenticationScope).HasMaxLength(512);
            e.HasIndex(x => x.VaultGuid).IsUnique();
            e.HasIndex(x => new { x.Environment, x.MonitoringEnabled });
        });
        builder.Entity<VaultHealthCheck>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.Property(x => x.AuthenticationStatus).HasMaxLength(40).IsRequired();
            e.Property(x => x.AuthenticatedAccountName).HasMaxLength(256);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.HasIndex(x => new { x.VaultId, x.CheckedAtUtc });
            e.HasOne(x => x.Vault).WithMany(x => x.HealthChecks).HasForeignKey(x => x.VaultId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<VaultDailyObjectStatistic>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.VaultId, x.DateUtc }).IsUnique();
            e.HasOne(x => x.Vault).WithMany(x => x.DailyStatistics).HasForeignKey(x => x.VaultId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Incident>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(40).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(8).IsRequired();
            e.Property(x => x.Status).HasMaxLength(24).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Fingerprint).HasMaxLength(128).IsRequired();
            e.Property(x => x.LastErrorCode).HasMaxLength(100);
            e.HasIndex(x => new { x.Status, x.LastSeenUtc });
            e.HasIndex(x => new { x.VaultId, x.FirstSeenUtc });
            e.HasOne(x => x.Vault).WithMany(x => x.Incidents).HasForeignKey(x => x.VaultId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
