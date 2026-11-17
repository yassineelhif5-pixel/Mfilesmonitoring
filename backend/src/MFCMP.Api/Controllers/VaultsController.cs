using MFCMP.Api.Data;
using MFCMP.Api.Domain;
using MFCMP.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalPasswordChanged")]
[AutoValidateAntiforgeryToken]
[Route("api/vaults")]
public sealed class VaultsController(MonitoringDbContext db, IVaultStatusProbe statusProbe) : ControllerBase
{
    private static bool IsMFilesCloudHost(Uri host) =>
        host.Scheme == Uri.UriSchemeHttps && host.Port == 443 &&
        host.Host.EndsWith(".cloudvault.m-files.com", StringComparison.OrdinalIgnoreCase);

    private static bool IsAzureKeyVaultHost(string host) =>
        host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vault.usgovcloudapi.net", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vault.azure.cn", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var vaults = await db.Vaults.AsNoTracking().OrderBy(v => v.Name)
            .Select(v => new
            {
                v.Id, v.Name, v.VaultGuid, v.CloudHost, v.Organization, v.Environment,
                v.MonitoringEnabled, v.AuthenticationMode, v.HealthCheckIntervalSeconds,
                v.MetricsCollectionIntervalMinutes,
                LastHealthCheck = v.HealthChecks.OrderByDescending(h => h.CheckedAtUtc)
                    .Select(h => new
                    {
                        h.CheckedAtUtc, h.Status, h.DurationMs, h.AuthenticationStatus,
                        h.AuthenticatedAccountName, h.ServerVersion, h.CapabilitiesSummary, h.Message
                    }).FirstOrDefault()
            }).ToListAsync(ct);
        return Ok(vaults);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVaultRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.CloudHost, UriKind.Absolute, out var host) || !IsMFilesCloudHost(host) ||
            !string.IsNullOrEmpty(host.UserInfo))
            return BadRequest(new { message = "CloudHost must be an M-Files Cloud host under *.cloudvault.m-files.com using HTTPS port 443." });
        if (string.IsNullOrWhiteSpace(request.Name) || request.VaultGuid == Guid.Empty)
            return BadRequest(new { message = "Vault name and a valid Vault GUID are required." });
        if (!new[] { "DEV", "TEST", "UAT", "PREPROD", "PROD" }.Contains(request.Environment.ToUpperInvariant()))
            return BadRequest(new { message = "Environment must be DEV, TEST, UAT, PREPROD or PROD." });
        if (!new[] { "None", "Credentials", "Token" }.Contains(request.AuthenticationMode, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "AuthenticationMode must be None, Credentials or Token." });
        if (!string.Equals(request.AuthenticationMode, "None", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(request.CredentialSecretUri) ||
             !Uri.TryCreate(request.CredentialSecretUri, UriKind.Absolute, out var secretUri) ||
             secretUri.Scheme != Uri.UriSchemeHttps || !IsAzureKeyVaultHost(secretUri.Host) ||
             !string.IsNullOrEmpty(secretUri.UserInfo) ||
             !secretUri.AbsolutePath.Contains("/secrets/", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Configure a valid HTTPS Azure Key Vault secret URI." });
        if (await db.Vaults.AnyAsync(v => v.VaultGuid == request.VaultGuid, ct))
            return Conflict(new { message = "A vault with this GUID is already registered." });

        var vault = new Vault
        {
            Name = request.Name.Trim(), VaultGuid = request.VaultGuid,
            CloudHost = host.GetLeftPart(UriPartial.Authority),
            Organization = request.Organization?.Trim() ?? string.Empty,
            Environment = request.Environment.Trim().ToUpperInvariant(),
            MonitoringEnabled = request.MonitoringEnabled,
            AuthenticationMode = request.AuthenticationMode,
            CredentialSecretUri = request.CredentialSecretUri,
            AuthenticationPlugin = request.AuthenticationPlugin,
            AuthenticationScope = request.AuthenticationScope,
            HealthCheckIntervalSeconds = Math.Clamp(request.HealthCheckIntervalSeconds, 30, 3600),
            MetricsCollectionIntervalMinutes = Math.Clamp(request.MetricsCollectionIntervalMinutes, 5, 1440)
        };
        db.Vaults.Add(vault);
        db.Tenants.Add(new Tenant
        {
            Vault = vault, Name = vault.Name,
            Code = $"VAULT-{vault.Id:N}".ToUpperInvariant(), IsActive = true
        });
        await db.SaveChangesAsync(ct);
        return Ok(new { vault.Id, vault.Name, vault.VaultGuid });
    }

    [HttpPost("{id:guid}/connection-test")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct)
    {
        var vault = await db.Vaults.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vault is null) return NotFound();
        var result = await statusProbe.TestAsync(vault, ct);
        db.VaultHealthChecks.Add(result);
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            result.Status, result.DurationMs, result.AuthenticationStatus,
            result.AuthenticatedAccountName, result.ServerVersion,
            result.CapabilitiesSummary, result.Message, result.CheckedAtUtc
        });
    }

    public sealed record CreateVaultRequest(
        string Name, Guid VaultGuid, string CloudHost, string? Organization,
        string Environment, bool MonitoringEnabled = true,
        string AuthenticationMode = "None", string? CredentialSecretUri = null,
        string? AuthenticationPlugin = null, string? AuthenticationScope = null,
        int HealthCheckIntervalSeconds = 60, int MetricsCollectionIntervalMinutes = 15);
}
