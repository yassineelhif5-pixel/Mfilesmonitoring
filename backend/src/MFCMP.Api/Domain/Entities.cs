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
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<VaultHealthCheck> HealthChecks { get; set; } = new List<VaultHealthCheck>();
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
    public string Message { get; set; } = string.Empty;
}
