using MFCMP.Api.Domain;
using MFiles.Grpc;
using PortalVault = MFCMP.Api.Domain.Vault;

namespace MFCMP.Api.Services;

public interface IVaultMetricsCollector
{
    Task<IReadOnlyList<VaultDailyObjectStatistic>> CollectAsync(PortalVault vault, CancellationToken cancellationToken);
}

public sealed class GrpcVaultMetricsCollector(MFilesConnectionFactory connectionFactory) : IVaultMetricsCollector
{
    public async Task<IReadOnlyList<VaultDailyObjectStatistic>> CollectAsync(PortalVault vault, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Open(vault);
        var authenticated = await connectionFactory.AuthenticateAsync(connection, vault, cancellationToken);
        if (!authenticated)
            throw new InvalidOperationException("Metrics collection requires a configured and authenticated read-only monitoring identity.");

        var search = new IRPCSearch.IRPCSearchClient(connection.CallInvoker);
        var total = await CountAsync(search, new[] { MFilesValues.IsNotDeleted() }, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = new List<VaultDailyObjectStatistic>();

        for (var offset = 6; offset >= 0; offset--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var day = today.AddDays(-offset);
            var start = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = day.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var conditions = new[]
            {
                MFilesValues.IsNotDeleted(),
                MFilesValues.PropertyCondition(20, ConditionType.GreaterThanOrEqual, MFilesValues.Timestamp(start)),
                MFilesValues.PropertyCondition(20, ConditionType.LessThan, MFilesValues.Timestamp(end))
            };
            var count = await CountAsync(search, conditions, cancellationToken);
            rows.Add(new VaultDailyObjectStatistic
            {
                VaultId = vault.Id, DateUtc = day,
                CreatedCount = count.Count, CreatedCountIsExact = count.Exact,
                TotalObjectCount = total.Count, TotalObjectCountIsExact = total.Exact,
                CollectedAtUtc = DateTimeOffset.UtcNow
            });
        }
        return rows;
    }

    private static async Task<(long Count, bool Exact)> CountAsync(
        IRPCSearch.IRPCSearchClient search, IEnumerable<SearchCondition> conditions, CancellationToken ct)
    {
        var request = new GetObjectCountInSearchRequest
        {
            Callimp = CallImportance.Normal,
            SearchConditionArrays = { new SearchConditionArray { Value = { conditions } } }
        };
        var response = await search.GetObjectCountInSearchAsync(request, cancellationToken: ct);
        var count = response.Psrcountarr.FirstOrDefault();
        return count is null ? (0, true) : (count.Count, count.Flags?.ExactMeasure == true);
    }
}
