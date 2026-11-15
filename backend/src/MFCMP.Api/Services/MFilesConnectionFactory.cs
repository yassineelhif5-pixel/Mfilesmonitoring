using MFCMP.Api.Domain;
using MFiles.Grpc;
using PortalVault = MFCMP.Api.Domain.Vault;

namespace MFCMP.Api.Services;

public sealed class MFilesConnectionFactory(IVaultCredentialProvider credentials)
{
    private static bool IsMFilesCloudHost(Uri host) =>
        host.Scheme == Uri.UriSchemeHttps &&
        host.Port == 443 &&
        host.Host.EndsWith(".cloudvault.m-files.com", StringComparison.OrdinalIgnoreCase);

    public MFilesConnection Open(PortalVault vault)
    {
        if (!Uri.TryCreate(vault.CloudHost, UriKind.Absolute, out var host) || !IsMFilesCloudHost(host))
            throw new InvalidOperationException("CloudHost must be an M-Files Cloud host under *.cloudvault.m-files.com using HTTPS port 443.");
        return MFilesConnection.Open(new MFilesSettings
        {
            Address = $"{host.Host}:443",
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
            var discovered = await connection.FindAuthenticationPluginAsync(vaultGuid, secret.PluginName, cancellationToken);
            var pluginName = secret.PluginName ?? discovered.Name;
            var scope = secret.ConfigurationScope ?? discovered.Scope;
            await connection.LogInWithTokenAsync(vaultGuid, secret.Token!, pluginName, scope, clientName,
                cancellationToken, discovered.Vault);
            return true;
        }

        throw new InvalidOperationException("Unsupported M-Files authentication mode.");
    }
}
