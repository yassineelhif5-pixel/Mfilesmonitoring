using System.Diagnostics;
using Grpc.Core;
using MFCMP.Api.Domain;
using MFiles.Grpc;
using PortalVault = MFCMP.Api.Domain.Vault;

namespace MFCMP.Api.Services;

public sealed class GrpcVaultStatusProbe(
    MFilesConnectionFactory connectionFactory,
    ILogger<GrpcVaultStatusProbe> logger) : IVaultStatusProbe
{
    private static string DecodeVersion(ulong version) =>
        $"{version >> 48}.{(version >> 32) & 0xFFFF}.{(version >> 16) & 0xFFFF}.{version & 0xFFFF}";

    public async Task<VaultHealthCheck> TestAsync(PortalVault vault, CancellationToken cancellationToken)
    {
        var result = new VaultHealthCheck { VaultId = vault.Id, AuthenticationStatus = "NOT_CONFIGURED" };
        if (!Uri.TryCreate(vault.CloudHost, UriKind.Absolute, out var host) ||
            host.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(host.UserInfo))
        {
            result.Status = "INVALID_CONFIGURATION";
            result.Message = "CloudHost must be a valid HTTPS URL without embedded credentials.";
            return result;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var watch = Stopwatch.StartNew();
        string stage = "CONNECT";
        try
        {
            await using var connection = connectionFactory.Open(vault);
            var versionClient = new IRPCQueryVersion.IRPCQueryVersionClient(connection.CallInvoker);
            var version = await versionClient.QueryVersionAsync(new QueryVersionRequest(), cancellationToken: timeout.Token);
            var loginClient = new IRPCLogin.IRPCLoginClient(connection.CallInvoker);
            var capabilities = await loginClient.GetServerCapabilitiesAsync(new GetServerCapabilitiesRequest(), cancellationToken: timeout.Token);

            result.ServerVersion = DecodeVersion(version.ServerVersion);
            result.CapabilitiesSummary = capabilities.ServerCapabilities.ToString();

            stage = "AUTHENTICATION";
            var authenticated = await connectionFactory.AuthenticateAsync(connection, vault, timeout.Token);
            if (!authenticated)
            {
                result.Status = "WARNING_AUTH_NOT_CONFIGURED";
                result.AuthenticationStatus = "NOT_CONFIGURED";
                result.Message = "M-Files Cloud gRPC is reachable. Configure a per-vault monitoring identity in Azure Key Vault to validate authentication and vault permissions.";
            }
            else
            {
                stage = "VAULT_ACCESS";
                _ = await loginClient.GetVaultSessionInfoAsync(new GetVaultSessionInfoRequest(), cancellationToken: timeout.Token);
                result.Status = "HEALTHY";
                result.AuthenticationStatus = "AUTHENTICATED";
                result.AuthenticatedAccountName = connection.Session?.AccountName;
                result.Message = "gRPC endpoint reachable; monitoring identity authenticated and vault session validated.";
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            result.Status = "TIMEOUT";
            result.Message = "M-Files Cloud diagnostic exceeded the 20-second timeout.";
        }
        catch (RpcException exception)
        {
            result.Status = stage == "AUTHENTICATION" ? "AUTHENTICATION_FAILED" :
                stage == "VAULT_ACCESS" ? "VAULT_ACCESS_FAILED" :
                exception.StatusCode == StatusCode.Unavailable ? "CONNECTION_ERROR" : $"GRPC_{exception.StatusCode.ToString().ToUpperInvariant()}";
            result.AuthenticationStatus = stage == "AUTHENTICATION" ? "FAILED" : result.AuthenticationStatus;
            result.Message = stage switch
            {
                "AUTHENTICATION" => "The configured monitoring identity could not authenticate. Check the Key Vault secret and the vault authentication method.",
                "VAULT_ACCESS" => "Authentication succeeded, but the vault session validation failed. Check the monitoring account permissions.",
                _ => $"M-Files Cloud gRPC diagnostic failed ({exception.StatusCode})."
            };
            logger.LogWarning("M-Files gRPC probe failed for vault {VaultId} at stage {Stage} with status {StatusCode}.",
                vault.Id, stage, exception.StatusCode);
        }
        catch (Exception exception)
        {
            result.Status = stage == "AUTHENTICATION" ? "AUTHENTICATION_CONFIGURATION_ERROR" : "DIAGNOSTIC_ERROR";
            result.Message = "The diagnostic could not complete. Check the sanitized application logs and connector configuration.";
            logger.LogError("M-Files probe failed for vault {VaultId} at stage {Stage}: {ExceptionType}.",
                vault.Id, stage, exception.GetType().Name);
        }
        finally
        {
            watch.Stop();
            result.DurationMs = watch.ElapsedMilliseconds;
            result.CheckedAtUtc = DateTimeOffset.UtcNow;
        }
        return result;
    }
}
