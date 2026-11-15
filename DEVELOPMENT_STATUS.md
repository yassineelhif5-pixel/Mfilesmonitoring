# Development status

Updated: 2026-11-15

## Repository
- Repository: https://github.com/yassineelhif5-pixel/Mfilesmonitoring
- Branch: main
- Stack: .NET 10, Angular 21, SQL Server, official M-Files gRPC client 26.11.0.

## Implemented in this iteration
- [x] Local portal authentication, first-login password change and CSRF protection.
- [x] Multi-vault configuration with Cloud host, GUID, environment and health/metrics intervals.
- [x] Per-vault M-Files authentication modes: None, Credentials, Token/plugin.
- [x] Azure Key Vault secret URI references; secret values are never persisted in SQL.
- [x] Official M-Files gRPC channel, TLS and anonymous version/capability diagnostics.
- [x] M-Files credentials login or plugin token login using official client methods.
- [x] Authenticated vault session validation using GetVaultSessionInfo.
- [x] Background health worker with per-vault intervals and isolation of failures.
- [x] Daily document/object count collector for the previous seven UTC days.
- [x] Total non-deleted object count and created-by-day count using GetObjectCountInSearch.
- [x] Exact/approximate count flag persisted and exposed.
- [x] Incident creation, fingerprint deduplication, occurrence count and lifecycle status.
- [x] Dashboard, statistics and incident APIs.
- [x] Angular dashboard, vault configuration, manual connection test and incident status management.
- [x] SQL migration for authentication settings, daily statistics and incidents.
- [x] GitHub Actions backend and frontend build workflow.

## M-Files API implementation notes
- Credentials use official MFilesConnection.LogInAsync(vaultGuid, username, password, clientName).
- Plugin tokens use FindAuthenticationPluginAsync and LogInWithTokenAsync.
- Session validation uses IRPCLogin.GetVaultSessionInfo.
- Counts use IRPCSearch.GetObjectCountInSearch and MFilesValues.
- Built-in property 20 is Created (timestamp); this is used for UTC-day counts.
- M-Files Cloud host is connected over TLS/HTTP2 on port 443.
- Sessions are disposed/logged out after each probe/collection.

## Data and security boundaries
- Portal admin and M-Files monitoring identities are separate.
- Azure Key Vault secret JSON for Credentials: { username, password }.
- Azure Key Vault secret JSON for Token: { token, pluginName?, configurationScope? }.
- API managed identity or local DefaultAzureCredential identity must have Key Vault secrets/get.
- M-Files monitoring accounts should have minimum read-only vault rights.
- Only session lifecycle, read diagnostics and search/count calls are used.
- No document, metadata, workflow, permission, user or vault configuration writes are performed by the monitoring connector.
- Search counts respect the monitoring user's visibility; they are not necessarily a privileged all-vault count.
- Daily Created counts are rolling historical search counts, not a live event stream.
- Direct M-Files Cloud internal server filesystem logs and exhaustive tracing of all external client calls are not assumed available.

## Not yet implemented / external integration
- [ ] Live validation against a real M-Files Cloud vault and real Key Vault secret.
- [ ] Microsoft Graph SSO/SCIM logs and provisioning diagnostics.
- [ ] Aino usage and subscription/license provider.
- [ ] API request tracing for instrumented applications / approved proxy / audit source.
- [ ] Per-class and per-object-type breakdown (requires additional grouping/aggregation work).
- [ ] Notification delivery and alert rules.
- [ ] Historical data retention/archival policy.
- [ ] Production Azure infrastructure, managed identity, SQL deployment and CI/CD release workflow.
- [ ] Automated tests and runtime migration validation against SQL Server.

## Validation
The next GitHub Actions run validates the latest source changes. A successful build does not prove access to the user's real M-Files Cloud vault, Azure Key Vault or SQL Server. Live integration requires the tenant's host, vault GUID, read-only service account and Key Vault permissions.

Never commit real credentials, tokens, cookies or secret values.
