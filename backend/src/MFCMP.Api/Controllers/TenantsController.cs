using MFCMP.Api.Data;
using MFCMP.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalPasswordChanged")]
[AutoValidateAntiforgeryToken]
[Route("api/tenants")]
public sealed class TenantsController(MonitoringDbContext db) : ControllerBase
{
    [HttpGet("by-vault/{vaultId:guid}")]
    public async Task<IActionResult> GetByVault(Guid vaultId, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .Where(t => t.VaultId == vaultId)
            .Select(t => new
            {
                t.Id, t.VaultId, t.Name, t.Code, t.IsActive, t.CreatedAtUtc,
                ServiceAccounts = t.ServiceAccounts.OrderByDescending(a => a.IsPrimary)
                    .Select(a => new { a.Id, a.Name, a.AccountName, a.MFilesAccountId, a.AuthenticationMode, a.IsPrimary, a.IsEnabled, a.LastUsedAtUtc })
            }).FirstOrDefaultAsync(ct);
        return tenant is null ? NotFound(new { message = "No tenant is associated with this vault." }) : Ok(tenant);
    }

    [HttpPost("{tenantId:guid}/service-accounts")]
    public async Task<IActionResult> AddServiceAccount(Guid tenantId, CreateServiceAccountRequest request, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return NotFound(new { message = "Tenant not found." });
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.AccountName))
            return BadRequest(new { message = "Service account name and account identifier are required." });
        if (!Uri.TryCreate(request.CredentialSecretUri, UriKind.Absolute, out var secretUri) ||
            secretUri.Scheme != Uri.UriSchemeHttps || !secretUri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase) ||
            !secretUri.AbsolutePath.Contains("/secrets/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Provide an HTTPS Azure Key Vault secret URI. Never send the password or token to this API." });
        if (await db.ServiceAccounts.AnyAsync(a => a.TenantId == tenantId && a.AccountName == request.AccountName, ct))
            return Conflict(new { message = "This service account is already registered for this tenant." });

        var account = new ServiceAccount
        {
            TenantId = tenantId, Name = request.Name.Trim(), AccountName = request.AccountName.Trim(),
            MFilesAccountId = request.MFilesAccountId?.Trim(), AuthenticationMode = request.AuthenticationMode,
            CredentialSecretUri = request.CredentialSecretUri, IsPrimary = request.IsPrimary,
            IsEnabled = true
        };
        if (account.IsPrimary)
        {
            var previous = await db.ServiceAccounts.Where(a => a.TenantId == tenantId && a.IsPrimary).ToListAsync(ct);
            foreach (var item in previous) item.IsPrimary = false;
        }
        db.ServiceAccounts.Add(account);
        db.MonitoringLogs.Add(new MonitoringLog
        {
            TenantId = tenantId, VaultId = tenant.VaultId, ServiceAccountId = account.Id,
            Source = "MFCMP", Category = "CONFIGURATION", Level = "INFO",
            EventName = "SERVICE_ACCOUNT_REGISTERED",
            Message = $"Service account '{account.AccountName}' was registered for tenant '{tenant.Code}'."
        });
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetByVault), new { vaultId = tenant.VaultId }, new
        {
            account.Id, account.Name, account.AccountName, account.MFilesAccountId,
            account.AuthenticationMode, account.IsPrimary, account.IsEnabled
        });
    }

    [HttpPatch("{tenantId:guid}/service-accounts/{accountId:guid}")]
    public async Task<IActionResult> UpdateServiceAccount(Guid tenantId, Guid accountId, UpdateServiceAccountRequest request, CancellationToken ct)
    {
        var account = await db.ServiceAccounts.FirstOrDefaultAsync(a => a.Id == accountId && a.TenantId == tenantId, ct);
        if (account is null) return NotFound();
        account.IsEnabled = request.IsEnabled;
        if (request.IsPrimary)
        {
            var others = await db.ServiceAccounts.Where(a => a.TenantId == tenantId && a.Id != accountId && a.IsPrimary).ToListAsync(ct);
            foreach (var other in others) other.IsPrimary = false;
            account.IsPrimary = true;
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { account.Id, account.IsEnabled, account.IsPrimary });
    }

    [HttpGet("{tenantId:guid}/logs")]
    public async Task<IActionResult> SearchLogs(
        Guid tenantId, [FromQuery] string? category, [FromQuery] string? level,
        [FromQuery] Guid? serviceAccountId, [FromQuery] string? source,
        [FromQuery] DateTimeOffset? fromUtc, [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            return NotFound(new { message = "Tenant not found." });
        if (fromUtc.HasValue && toUtc.HasValue && toUtc < fromUtc)
            return BadRequest(new { message = "toUtc must be greater than or equal to fromUtc." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.MonitoringLogs.AsNoTracking().Where(l => l.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(l => l.Category == category);
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(l => l.Level == level);
        if (serviceAccountId.HasValue) query = query.Where(l => l.ServiceAccountId == serviceAccountId);
        if (!string.IsNullOrWhiteSpace(source)) query = query.Where(l => l.Source == source);
        if (fromUtc.HasValue) query = query.Where(l => l.OccurredAtUtc >= fromUtc);
        if (toUtc.HasValue) query = query.Where(l => l.OccurredAtUtc <= toUtc);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Message.Contains(search) || l.EventName.Contains(search) ||
                (l.ErrorCode != null && l.ErrorCode.Contains(search)) ||
                (l.CorrelationId != null && l.CorrelationId.Contains(search)));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new
            {
                l.Id, l.OccurredAtUtc, l.Source, l.Category, l.Level, l.EventName,
                l.Message, l.Details, l.ErrorCode, l.CorrelationId,
                l.ServiceAccountId, ServiceAccount = l.ServiceAccount == null ? null : l.ServiceAccount.AccountName
            }).ToListAsync(ct);
        return Ok(new { tenantId, page, pageSize, total, items });
    }

    public sealed record CreateServiceAccountRequest(
        string Name, string AccountName, string? MFilesAccountId,
        string AuthenticationMode, string CredentialSecretUri, bool IsPrimary = false);
    public sealed record UpdateServiceAccountRequest(bool IsEnabled, bool IsPrimary = false);
}
