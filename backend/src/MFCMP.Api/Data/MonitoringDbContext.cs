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
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();
    public DbSet<MonitoringLog> MonitoringLogs => Set<MonitoringLog>();

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

        builder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Code).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.VaultId).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.Vault).WithOne(x => x.Tenant).HasForeignKey<Tenant>(x => x.VaultId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ServiceAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.AccountName).HasMaxLength(256).IsRequired();
            e.Property(x => x.MFilesAccountId).HasMaxLength(128);
            e.Property(x => x.AuthenticationMode).HasMaxLength(24).IsRequired();
            e.Property(x => x.CredentialSecretUri).HasMaxLength(2048).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.AccountName }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(x => x.ServiceAccounts).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<MonitoringLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Source).HasMaxLength(40).IsRequired();
            e.Property(x => x.Category).HasMaxLength(40).IsRequired();
            e.Property(x => x.Level).HasMaxLength(16).IsRequired();
            e.Property(x => x.EventName).HasMaxLength(160).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ErrorCode).HasMaxLength(128);
            e.Property(x => x.CorrelationId).HasMaxLength(128);
            e.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
            e.HasIndex(x => new { x.TenantId, x.Category, x.Level, x.OccurredAtUtc });
            e.HasIndex(x => new { x.ServiceAccountId, x.OccurredAtUtc });
            e.HasOne(x => x.Tenant).WithMany(x => x.Logs).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vault).WithMany().HasForeignKey(x => x.VaultId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ServiceAccount).WithMany(x => x.Logs).HasForeignKey(x => x.ServiceAccountId).OnDelete(DeleteBehavior.SetNull);
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
