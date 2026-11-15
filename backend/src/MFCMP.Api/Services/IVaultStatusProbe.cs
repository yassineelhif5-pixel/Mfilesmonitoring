using MFCMP.Api.Domain;
namespace MFCMP.Api.Services;

public interface IVaultStatusProbe
{
    Task<VaultHealthCheck> TestAsync(Vault vault, CancellationToken cancellationToken);
}
