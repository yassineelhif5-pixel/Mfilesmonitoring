using MFCMP.Api.Domain;
using MFiles.Grpc;
using PortalVault = MFCMP.Api.Domain.Vault;

namespace MFCMP.Api.Services;

public sealed class MFilesConnectionFactory(IVaultCredentialProvider credentials)
{
    public MFilesConnection Open(PortalVault vault)
    {
        if (!Uri.TryCreate(vault.CloudHost, UriKind.Absolute, out var host) || host.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("CloudHost must be a valid HTTPS URL.");
        return MFilesConnection.Open(new MFilesSettings
        {
            Address = $"{host.Host}:{host.Port}",
            UseTls = true,
            Vault = vault.VaultGuid.ToString("B")
        });
    }

    public async Task<bool> AuthenticateAsync(MFilesConnection connection, PortalVault vault, CancellationToken cancellationToken)
    {
        var secret = await credentials.GetAsync(vault, cancellationToken);
        if (secret is null) return false;

        var vaultGuid = vault.VaultGuid.ToString("B");
        const string clientName = "MFCMP Monitoring";

        if (string.Equals(vault.AuthenticationMode, "Credentials", StringComparison.OrdinalIgnoreCase))
        {
            await connection.LogInAsync(vaultGuid, secret.Username!, secret.Password!, clientName, cancellationToken);
            return true;
        }

        if (string.Equals(vault.AuthenticationMode, "Token", StringComparison.OrdinalIgnoreCase))
        {
            var pluginName = secret.PluginName;
            var scope = secret.ConfigurationScope;
            var pluginVault = string.Empty;
            if (string.IsNullOrWhiteSpace(pluginName) || string.IsNullOrWhiteSpace(scope))
            {
                var discovered = await connection.FindAuthenticationPluginAsync(vaultGuid, pluginName, cancellationToken);
                pluginName ??= discovered.Name;
                scope ??= discovered.Scope;
                pluginVault = discovered.Vault;
            }
            await connection.LogInWithTokenAsync(vaultGuid, secret.Token!, pluginName!, scope!, clientName,
                cancellationToken, pluginVault);
            return true;
        }

        throw new InvalidOperationException("Unsupported M-Files authentication mode.");
    }
}
