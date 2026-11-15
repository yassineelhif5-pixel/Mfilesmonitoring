using MFCMP.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalPasswordChanged")]
[AutoValidateAntiforgeryToken]
[Route("api/monitoring")]
public sealed class MonitoringController(MonitoringDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var vaults = await db.Vaults.AsNoTracking().Where(v => v.MonitoringEnabled).ToListAsync(ct);
        var latest = await db.VaultHealthChecks.AsNoTracking()
            .GroupBy(h => h.VaultId)
            .Select(g => g.OrderByDescending(h => h.CheckedAtUtc).First())
            .ToListAsync(ct);
        var activeIncidents = await db.Incidents.AsNoTracking()
            .Where(i => i.Status != "RESOLVED" && i.Status != "CLOSED")
            .CountAsync(ct);
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var daily = await db.VaultDailyObjectStatistics.AsNoTracking()
            .Where(s => s.DateUtc >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)))
            .GroupBy(s => s.DateUtc)
            .Select(g => new
            {
                dateUtc = g.Key,
                createdCount = g.Sum(x => x.CreatedCount),
                createdCountExact = g.All(x => x.CreatedCountIsExact),
                totalObjectCount = g.Sum(x => x.TotalObjectCount),
                totalObjectCountExact = g.All(x => x.TotalObjectCountIsExact)
            }).OrderBy(x => x.dateUtc).ToListAsync(ct);

        return Ok(new
        {
            totalVaults = vaults.Count,
            healthyVaults = latest.Count(x => x.Status == "HEALTHY"),
            warningVaults = latest.Count(x => x.Status.StartsWith("WARNING", StringComparison.Ordinal)),
            failedVaults = latest.Count(x => x.Status is "TIMEOUT" or "CONNECTION_ERROR" or "AUTHENTICATION_FAILED" or "VAULT_ACCESS_FAILED" or "DIAGNOSTIC_ERROR"),
            unknownVaults = vaults.Count - latest.Count,
            activeIncidents,
            vaultsCheckedLast7Days = latest.Count(x => x.CheckedAtUtc >= since),
            dailyStatistics = daily
        });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics([FromQuery] Guid? vaultId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6));
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (end < start || end.DayNumber - start.DayNumber > 366)
            return BadRequest(new { message = "Date range must be chronological and no longer than 367 days." });

        var query = db.VaultDailyObjectStatistics.AsNoTracking().Where(x => x.DateUtc >= start && x.DateUtc <= end);
        if (vaultId.HasValue) query = query.Where(x => x.VaultId == vaultId.Value);
        var rows = await query.OrderBy(x => x.DateUtc).Select(x => new
        {
            x.VaultId, x.DateUtc, x.CreatedCount, x.CreatedCountIsExact,
            x.TotalObjectCount, x.TotalObjectCountIsExact, x.CollectedAtUtc
        }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> Incidents([FromQuery] string? status, CancellationToken ct)
    {
        var query = db.Incidents.AsNoTracking().Include(i => i.Vault).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.Status == status);
        var incidents = await query.OrderByDescending(i => i.LastSeenUtc).Take(250)
            .Select(i => new
            {
                i.Id, i.VaultId, vaultName = i.Vault!.Name, i.Category, i.Severity, i.Status,
                i.Title, i.Description, i.FirstSeenUtc, i.LastSeenUtc, i.Occurrences, i.LastErrorCode
            }).ToListAsync(ct);
        return Ok(incidents);
    }

    [HttpPatch("incidents/{id:guid}")]
    public async Task<IActionResult> UpdateIncident(Guid id, UpdateIncidentRequest request, CancellationToken ct)
    {
        if (!new[] { "ACKNOWLEDGED", "IN_PROGRESS", "RESOLVED", "CLOSED", "NEW" }.Contains(request.Status))
            return BadRequest(new { message = "Invalid incident status." });
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (incident is null) return NotFound();
        incident.Status = request.Status;
        incident.AcknowledgedAtUtc = request.Status == "ACKNOWLEDGED" ? DateTimeOffset.UtcNow : incident.AcknowledgedAtUtc;
        incident.ResolvedAtUtc = request.Status is "RESOLVED" or "CLOSED" ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return Ok(new { incident.Id, incident.Status, incident.AcknowledgedAtUtc, incident.ResolvedAtUtc });
    }

    public sealed record UpdateIncidentRequest(string Status);
}
