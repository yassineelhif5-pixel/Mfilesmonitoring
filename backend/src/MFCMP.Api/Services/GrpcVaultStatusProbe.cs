using System.Diagnostics;
using Grpc.Core;
using MFCMP.Api.Domain;
using MFiles.Grpc;

namespace MFCMP.Api.Services;

/// <summary>
/// Performs anonymous, read-only gRPC diagnostics against an M-Files Cloud host.
/// It does not authenticate to a vault and must not be reported as a credential test.
/// </summary>
public sealed class GrpcVaultStatusProbe(ILogger<GrpcVaultStatusProbe> logger) : IVaultStatusProbe
{
    private static string DecodeVersion(ulong version) =>
        $"{version >> 48}.{(version >> 32) & 0xFFFF}.{(version >> 16) & 0xFFFF}.{version & 0xFFFF}";

    public async Task<VaultHealthCheck> TestAsync(Vault vault, CancellationToken cancellationToken)
    {
        var result = new VaultHealthCheck
        {
            VaultId = vault.Id,
            AuthenticationStatus = "NOT_TESTED"
        };

        if (!Uri.TryCreate(vault.CloudHost, UriKind.Absolute, out var host) ||
            host.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(host.UserInfo) ||
            string.IsNullOrWhiteSpace(host.Host))
        {
            result.Status = "INVALID_CONFIGURATION";
            result.Message = "CloudHost must be a valid HTTPS M-Files Cloud URL without embedded credentials.";
            return result;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // M-Files Cloud gRPC uses TLS/HTTP2 on port 443. The official MFiles.Grpc
            // connection helper also handles the mfilesmsm affinity cookie.
            var settings = new MFilesSettings
            {
                Address = $"{host.Host}:{host.Port}",
                UseTls = true
            };

            await using var connection = MFilesConnection.Open(settings);

            var versionClient = new IRPCQueryVersion.IRPCQueryVersionClient(connection.CallInvoker);
            var version = await versionClient.QueryVersionAsync(
                new QueryVersionRequest(),
                cancellationToken: timeout.Token);

            var loginClient = new IRPCLogin.IRPCLoginClient(connection.CallInvoker);
            var capabilities = await loginClient.GetServerCapabilitiesAsync(
                new GetServerCapabilitiesRequest(),
                cancellationToken: timeout.Token);

            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            result.ServerVersion = DecodeVersion(version.ServerVersion);
            result.CapabilitiesSummary = capabilities.ServerCapabilities.ToString();
            result.Status = "GRPC_REACHABLE_AUTH_NOT_TESTED";
            result.Message = "M-Files Cloud gRPC responded to anonymous version and capability queries. Vault authentication and vault-level permissions have not been tested.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            result.Status = "TIMEOUT";
            result.Message = "M-Files Cloud gRPC did not respond within the 12-second diagnostic timeout.";
        }
        catch (RpcException exception)
        {
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            result.Status = exception.StatusCode == StatusCode.Unavailable
                ? "CONNECTION_ERROR"
                : $"GRPC_{exception.StatusCode.ToString().ToUpperInvariant()}";
            result.Message = $"M-Files Cloud gRPC diagnostic failed with status {exception.StatusCode}.";
            logger.LogWarning("M-Files gRPC status probe failed for vault {VaultId} with status {StatusCode}.",
                vault.Id, exception.StatusCode);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            result.Status = "DIAGNOSTIC_ERROR";
            result.Message = "Unexpected gRPC diagnostic error. Consult sanitized application logs.";
            logger.LogError(exception, "Unexpected M-Files gRPC diagnostic error for vault {VaultId}.", vault.Id);
        }

        return result;
    }
}
