using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MFCMP.Api.Domain;
using MFCMP.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MFCMP.Api.Services;

public sealed record VaultCredential(string? Username, string? Password, string? Token, string? PluginName, string? ConfigurationScope);

public interface IVaultCredentialProvider
{
    Task<VaultCredential?> GetAsync(Vault vault, CancellationToken cancellationToken);
}

public sealed class KeyVaultCredentialProvider(DefaultAzureCredential credential, MonitoringDbContext db) : IVaultCredentialProvider
{
    private static bool IsAzureKeyVaultHost(string host) =>
        host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vault.usgovcloudapi.net", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".vault.azure.cn", StringComparison.OrdinalIgnoreCase);

    public async Task<VaultCredential?> GetAsync(Vault vault, CancellationToken cancellationToken)
    {
        var authMode = vault.AuthenticationMode;
        var secretUriValue = vault.CredentialSecretUri;
        var pluginName = vault.AuthenticationPlugin;
        var configurationScope = vault.AuthenticationScope;
        var tenant = await db.Tenants.Include(t => t.ServiceAccounts)
            .FirstOrDefaultAsync(t => t.VaultId == vault.Id && t.IsActive, cancellationToken);
        var primaryAccount = tenant?.ServiceAccounts.FirstOrDefault(a => a.IsEnabled && a.IsPrimary)
            ?? tenant?.ServiceAccounts.FirstOrDefault(a => a.IsEnabled);
        if (primaryAccount is not null)
        {
            authMode = primaryAccount.AuthenticationMode;
            secretUriValue = primaryAccount.CredentialSecretUri;
            primaryAccount.LastUsedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        if (string.Equals(authMode, "None", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(secretUriValue))
            return null;

        if (!Uri.TryCreate(secretUriValue, UriKind.Absolute, out var secretUri) ||
            secretUri.Scheme != Uri.UriSchemeHttps || !IsAzureKeyVaultHost(secretUri.Host) ||
            !string.IsNullOrEmpty(secretUri.UserInfo))
            throw new InvalidOperationException("CredentialSecretUri must be an HTTPS Azure Key Vault secret URI.");

        var segments = secretUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !string.Equals(segments[0], "secrets", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CredentialSecretUri must point to /secrets/{secret-name}[/{version}].");

        var name = Uri.UnescapeDataString(segments[1]);
        string? version = segments.Length > 2 ? Uri.UnescapeDataString(segments[2]) : null;
        var client = new SecretClient(new Uri(secretUri.GetLeftPart(UriPartial.Authority)), credential);
        var response = await client.GetSecretAsync(name, version, cancellationToken);
        using var document = JsonDocument.Parse(response.Value.Value);
        var root = document.RootElement;

        string? Read(string property) =>
            root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        var result = new VaultCredential(Read("username"), Read("password"), Read("token"),
            Read("pluginName") ?? pluginName,
            Read("configurationScope") ?? configurationScope);

        if (string.Equals(authMode, "Credentials", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(result.Username) || string.IsNullOrEmpty(result.Password)))
            throw new InvalidOperationException("The Key Vault secret must contain username and password.");
        if (string.Equals(authMode, "Token", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(result.Token))
            throw new InvalidOperationException("The Key Vault secret must contain a token.");
        if (!string.Equals(authMode, "Credentials", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(authMode, "Token", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("AuthenticationMode must be None, Credentials or Token.");

        return result;
    }
}
