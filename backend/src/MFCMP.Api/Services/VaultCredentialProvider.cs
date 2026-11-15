using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MFCMP.Api.Domain;

namespace MFCMP.Api.Services;

public sealed record VaultCredential(string? Username, string? Password, string? Token, string? PluginName, string? ConfigurationScope);

public interface IVaultCredentialProvider
{
    Task<VaultCredential?> GetAsync(Vault vault, CancellationToken cancellationToken);
}

public sealed class KeyVaultCredentialProvider(DefaultAzureCredential credential) : IVaultCredentialProvider
{
    public async Task<VaultCredential?> GetAsync(Vault vault, CancellationToken cancellationToken)
    {
        if (string.Equals(vault.AuthenticationMode, "None", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(vault.CredentialSecretUri))
            return null;

        if (!Uri.TryCreate(vault.CredentialSecretUri, UriKind.Absolute, out var secretUri) ||
            secretUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("CredentialSecretUri must be an HTTPS Azure Key Vault secret URI.");

        var segments = secretUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !string.Equals(segments[0], "secrets", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CredentialSecretUri must point to /secrets/{secret-name}[/{version}].");

        var name = Uri.UnescapeDataString(segments[1]);
        var version = segments.Length > 2 ? Uri.UnescapeDataString(segments[2]) : string.Empty;
        var client = new SecretClient(new Uri(secretUri.GetLeftPart(UriPartial.Authority)), credential);
        var response = await client.GetSecretAsync(name, version, cancellationToken);
        var json = response.Value.Value;
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        string? Read(string property) =>
            root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        var result = new VaultCredential(Read("username"), Read("password"), Read("token"),
            Read("pluginName") ?? vault.AuthenticationPlugin,
            Read("configurationScope") ?? vault.AuthenticationScope);

        if (string.Equals(vault.AuthenticationMode, "Credentials", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(result.Username) || string.IsNullOrEmpty(result.Password)))
            throw new InvalidOperationException("The Key Vault secret must contain username and password.");
        if (string.Equals(vault.AuthenticationMode, "Token", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(result.Token))
            throw new InvalidOperationException("The Key Vault secret must contain a token.");
        if (!string.Equals(vault.AuthenticationMode, "Credentials", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(vault.AuthenticationMode, "Token", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("AuthenticationMode must be None, Credentials or Token.");

        return result;
    }
}
