using Microsoft.AspNetCore.Identity;

namespace MFCMP.Api.Domain;

public sealed class PortalUser : IdentityUser
{
    public bool MustChangePassword { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Vault
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid VaultGuid { get; set; }
    public string CloudHost { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Environment { get; set; } = "DEV";
    public bool MonitoringEnabled { get; set; } = true;
    public string AuthenticationMode { get; set; } = "None";
    public string? CredentialSecretUri { get; set; }
    public string? AuthenticationPlugin { get; set; }
    public string? AuthenticationScope { get; set; }
    public int HealthCheckIntervalSeconds { get; set; } = 60;
    public int MetricsCollectionIntervalMinutes { get; set; } = 15;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<VaultHealthCheck> HealthChecks { get; set; } = new List<VaultHealthCheck>();
    public ICollection<VaultDailyObjectStatistic> DailyStatistics { get; set; } = new List<VaultDailyObjectStatistic>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public Tenant? Tenant { get; set; }
}

public sealed class VaultHealthCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VaultId { get; set; }
    public Vault? Vault { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "UNKNOWN";
    public int? HttpStatusCode { get; set; }
    public long DurationMs { get; set; }
    public string AuthenticationStatus { get; set; } = "NOT_TESTED";
    public string? AuthenticatedAccountName { get; set; }
    public string? ServerVersion { get; set; }
    public string? CapabilitiesSummary { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class VaultDailyObjectStatistic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VaultId { get; set; }
    public Vault? Vault { get; set; }
    public DateOnly DateUtc { get; set; }
    public long CreatedCount { get; set; }
    public bool CreatedCountIsExact { get; set; }
    public long TotalObjectCount { get; set; }
    public bool TotalObjectCountIsExact { get; set; }
    public DateTimeOffset CollectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VaultId { get; set; }
    public Vault? Vault { get; set; }
    public string Category { get; set; } = "CONNECTIVITY";
    public string Severity { get; set; } = "P2";
    public string Status { get; set; } = "NEW";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public DateTimeOffset FirstSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public int Occurrences { get; set; } = 1;
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
}

public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VaultId { get; set; }
    public Vault? Vault { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ServiceAccount> ServiceAccounts { get; set; } = new List<ServiceAccount>();
    public ICollection<MonitoringLog> Logs { get; set; } = new List<MonitoringLog>();
}

public sealed class ServiceAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? MFilesAccountId { get; set; }
    public string AuthenticationMode { get; set; } = "Credentials";
    public string CredentialSecretUri { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAtUtc { get; set; }
    public ICollection<MonitoringLog> Logs { get; set; } = new List<MonitoringLog>();
}

public sealed class MonitoringLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid VaultId { get; set; }
    public Vault? Vault { get; set; }
    public Guid? ServiceAccountId { get; set; }
    public ServiceAccount? ServiceAccount { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Source { get; set; } = "MFCMP";
    public string Category { get; set; } = "SYSTEM";
    public string Level { get; set; } = "INFO";
    public string EventName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? ErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}
