using MFCMP.Api.Data;
using MFCMP.Api.Domain;
using MFCMP.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalPasswordChanged")]
[Route("api/vaults")]
public sealed class VaultsController(MonitoringDbContext db, IVaultStatusProbe statusProbe) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var vaults = await db.Vaults.AsNoTracking().OrderBy(v => v.Name)
            .Select(v => new
            {
                v.Id, v.Name, v.VaultGuid, v.CloudHost, v.Organization, v.Environment, v.MonitoringEnabled,
                LastHealthCheck = v.HealthChecks.OrderByDescending(h => h.CheckedAtUtc)
                    .Select(h => new
                    {
                        h.CheckedAtUtc, h.Status, h.DurationMs, h.AuthenticationStatus,
                        h.ServerVersion, h.CapabilitiesSummary, h.Message
                    }).FirstOrDefault()
            }).ToListAsync(ct);
        return Ok(vaults);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVaultRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.CloudHost, UriKind.Absolute, out var host) ||
            host.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(host.UserInfo))
            return BadRequest(new { message = "CloudHost must be a valid HTTPS URL without embedded credentials." });
        if (string.IsNullOrWhiteSpace(request.Name) || request.VaultGuid == Guid.Empty)
            return BadRequest(new { message = "Vault name and a valid Vault GUID are required." });
        if (await db.Vaults.AnyAsync(v => v.VaultGuid == request.VaultGuid, ct))
            return Conflict(new { message = "A vault with this GUID is already registered." });

        var vault = new Vault
        {
            Name = request.Name.Trim(),
            VaultGuid = request.VaultGuid,
            CloudHost = host.GetLeftPart(UriPartial.Authority),
            Organization = request.Organization?.Trim() ?? string.Empty,
            Environment = request.Environment.Trim().ToUpperInvariant(),
            MonitoringEnabled = request.MonitoringEnabled
        };
        db.Vaults.Add(vault);
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
            result.ServerVersion, result.CapabilitiesSummary, result.Message, result.CheckedAtUtc
        });
    }

    public sealed record CreateVaultRequest(
        string Name, Guid VaultGuid, string CloudHost, string? Organization,
        string Environment, bool MonitoringEnabled = true);
}
