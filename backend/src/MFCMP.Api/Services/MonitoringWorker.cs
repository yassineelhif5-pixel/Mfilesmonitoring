using System.Security.Cryptography;
using System.Text;
using MFCMP.Api.Data;
using MFCMP.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Services;

public sealed class MonitoringWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Monitoring:WorkerTickSeconds", 15), 5, 300));
        using var timer = new PeriodicTimer(interval);
        await RunCycleAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCycleAsync(stoppingToken);
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
            var probe = scope.ServiceProvider.GetRequiredService<IVaultStatusProbe>();
            var metricCollector = scope.ServiceProvider.GetRequiredService<IVaultMetricsCollector>();
            var vaults = await db.Vaults.AsNoTracking().Where(v => v.MonitoringEnabled).ToListAsync(ct);
            var now = DateTimeOffset.UtcNow;

            foreach (var vault in vaults)
            {
                try
                {
                    var lastHealth = await db.VaultHealthChecks.Where(h => h.VaultId == vault.Id)
                        .OrderByDescending(h => h.CheckedAtUtc).Select(h => (DateTimeOffset?)h.CheckedAtUtc).FirstOrDefaultAsync(ct);
                    if (lastHealth is null || now - lastHealth.Value >= TimeSpan.FromSeconds(Math.Clamp(vault.HealthCheckIntervalSeconds, 30, 3600)))
                    {
                        var result = await probe.TestAsync(vault, ct);
                        db.VaultHealthChecks.Add(result);
                        await UpdateIncidentAsync(db, vault, result, ct);
                        await db.SaveChangesAsync(ct);
                    }

                    if (!string.Equals(vault.AuthenticationMode, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        var lastMetrics = await db.VaultDailyObjectStatistics.Where(x => x.VaultId == vault.Id)
                            .OrderByDescending(x => x.CollectedAtUtc).Select(x => (DateTimeOffset?)x.CollectedAtUtc).FirstOrDefaultAsync(ct);
                        if (lastMetrics is null || now - lastMetrics.Value >= TimeSpan.FromMinutes(Math.Clamp(vault.MetricsCollectionIntervalMinutes, 5, 1440)))
                        {
                            try
                            {
                                var rows = await metricCollector.CollectAsync(vault, ct);
                                foreach (var row in rows)
                                {
                                    var existing = await db.VaultDailyObjectStatistics
                                        .FirstOrDefaultAsync(x => x.VaultId == vault.Id && x.DateUtc == row.DateUtc, ct);
                                    if (existing is null) db.VaultDailyObjectStatistics.Add(row);
                                    else
                                    {
                                        existing.CreatedCount = row.CreatedCount;
                                        existing.CreatedCountIsExact = row.CreatedCountIsExact;
                                        existing.TotalObjectCount = row.TotalObjectCount;
                                        existing.TotalObjectCountIsExact = row.TotalObjectCountIsExact;
                                        existing.CollectedAtUtc = row.CollectedAtUtc;
                                    }
                                }
                                await db.SaveChangesAsync(ct);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                logger.LogWarning("Metrics collection failed for vault {VaultId}: {ExceptionType}.", vault.Id, ex.GetType().Name);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError("Monitoring cycle failed for vault {VaultId}: {ExceptionType}.", vault.Id, ex.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError("Monitoring worker cycle failed: {ExceptionType}.", ex.GetType().Name);
        }
    }

    private static async Task UpdateIncidentAsync(MonitoringDbContext db, Vault vault, VaultHealthCheck check, CancellationToken ct)
    {
        var isFailure = check.Status is "TIMEOUT" or "CONNECTION_ERROR" or "AUTHENTICATION_FAILED" or
            "VAULT_ACCESS_FAILED" or "AUTHENTICATION_CONFIGURATION_ERROR" or "DIAGNOSTIC_ERROR" or "INVALID_CONFIGURATION";
        var category = check.Status.Contains("AUTHENTICATION", StringComparison.Ordinal) ? "AUTHENTICATION" :
            check.Status == "VAULT_ACCESS_FAILED" ? "PERMISSIONS" : "CONNECTIVITY";
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{vault.Id:N}|{category}|{check.Status}")));

        if (isFailure)
        {
            var active = await db.Incidents.FirstOrDefaultAsync(i => i.VaultId == vault.Id &&
                i.Fingerprint == fingerprint && i.Status != "RESOLVED" && i.Status != "CLOSED", ct);
            if (active is null)
            {
                db.Incidents.Add(new Incident
                {
                    VaultId = vault.Id, Category = category,
                    Severity = category == "AUTHENTICATION" ? "P1" : "P2",
                    Status = "NEW", Title = $"{category}: {vault.Name}",
                    Description = check.Message, Fingerprint = fingerprint,
                    FirstSeenUtc = check.CheckedAtUtc, LastSeenUtc = check.CheckedAtUtc,
                    LastErrorCode = check.Status
                });
            }
            else
            {
                active.LastSeenUtc = check.CheckedAtUtc;
                active.Occurrences++;
                active.Description = check.Message;
                active.LastErrorCode = check.Status;
            }
        }
        else if (check.Status == "HEALTHY")
        {
            var open = await db.Incidents.Where(i => i.VaultId == vault.Id &&
                (i.Category == "CONNECTIVITY" || i.Category == "AUTHENTICATION" || i.Category == "PERMISSIONS") &&
                i.Status != "RESOLVED" && i.Status != "CLOSED").ToListAsync(ct);
            foreach (var incident in open)
            {
                incident.Status = "RESOLVED";
                incident.ResolvedAtUtc = check.CheckedAtUtc;
            }
        }
    }
}
