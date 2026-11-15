using System.Diagnostics;
using MFCMP.Api.Domain;
namespace MFCMP.Api.Services;

/// <summary>Unauthenticated MFWS status probe. Reachability does not prove vault authentication.</summary>
public sealed class RestVaultStatusProbe(HttpClient client) : IVaultStatusProbe
{
    public async Task<VaultHealthCheck> TestAsync(Vault vault, CancellationToken cancellationToken)
    {
        var result = new VaultHealthCheck { VaultId = vault.Id, AuthenticationStatus = "NOT_TESTED" };
        if (!Uri.TryCreate(vault.CloudHost, UriKind.Absolute, out var host) ||
            host.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(host.UserInfo))
        {
            result.Status = "INVALID_CONFIGURATION";
            result.Message = "CloudHost must be a valid HTTPS URL without embedded credentials.";
            return result;
        }

        var uri = new Uri(host.AbsoluteUri.TrimEnd('/') + "/REST/server/status");
        var timer = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            timer.Stop();
            result.HttpStatusCode = (int)response.StatusCode;
            result.DurationMs = timer.ElapsedMilliseconds;
            result.Status = response.IsSuccessStatusCode ? "REACHABLE_AUTH_NOT_TESTED" : "HTTP_ERROR";
            result.Message = response.IsSuccessStatusCode
                ? "The M-Files server status endpoint responded. Vault authentication has not been tested."
                : $"M-Files status endpoint returned HTTP {(int)response.StatusCode}.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timer.Stop();
            result.DurationMs = timer.ElapsedMilliseconds;
            result.Status = "TIMEOUT";
            result.Message = "The status endpoint did not respond within the configured timeout.";
        }
        catch (HttpRequestException)
        {
            timer.Stop();
            result.DurationMs = timer.ElapsedMilliseconds;
            result.Status = "CONNECTION_ERROR";
            result.Message = "Unable to reach the configured status endpoint.";
        }
        return result;
    }
}
