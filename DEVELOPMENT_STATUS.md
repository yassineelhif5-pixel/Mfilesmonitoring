# Development status

Updated: 2026-11-15

## Repository
- Repository: https://github.com/yassineelhif5-pixel/Mfilesmonitoring
- Branch: main
- Stack: .NET 10, Angular 21, SQL Server, official M-Files gRPC client 26.11.0.

## Implemented
- [x] Local portal authentication, mandatory first-login password change and CSRF protection.
- [x] Multi-vault configuration with Cloud host, GUID, environment and configurable health/metrics intervals.
- [x] Per-vault authentication modes: None, M-Files credentials and token/plugin.
- [x] Azure Key Vault references; secret values are never stored in SQL. Key Vault URI hostnames are allowlisted to Azure Key Vault DNS suffixes.
- [x] Official M-Files gRPC channel over TLS/HTTP2.
- [x] Anonymous server version/capability diagnostics.
- [x] M-Files credentials login and plugin token login using the official .NET client.
- [x] Authenticated vault session validation with GetVaultSessionInfo.
- [x] Correct handling of the official client's MFilesException wrapper and gRPC transport exceptions.
- [x] Background health worker with independent per-vault cadence.
- [x] Daily object creation counts for the rolling seven UTC days using GetObjectCountInSearch and built-in Created property ID 20.
- [x] Total non-deleted object count; exact/approximate flags persisted.
- [x] Incident generation, deduplication, occurrence counts, status management and recovery resolution.
- [x] Metrics collection failure incidents with interval-based retry throttling.
- [x] Dashboard, statistics and incident APIs.
- [x] Angular dashboard, vault configuration, manual connection test and incident management UI.
- [x] SQL migrations for portal Identity, vaults, health history, auth settings, daily metrics and incidents.
- [x] GitHub Actions backend and frontend build.

## Validation
- [x] GitHub Actions .NET 10 backend build passed on commit 714d4f9e391c61cdc58b189f197e3776ed6a9d4c.
- [x] GitHub Actions Angular production build passed on the same commit.
- [ ] Validate the latest follow-up commit in CI.
- [ ] Apply both migrations against a live SQL Server instance.
- [ ] Live integration test with a real M-Files Cloud vault and Azure Key Vault secret.
- [ ] Verify that the monitoring account has the required minimum read-only rights.

## Metrics semantics and limitations
- Built-in property 20 is Created (timestamp).
- Created counts include only objects visible to the configured account; deleted objects require the M-Files see-deleted-objects right to be included.
- Total count uses IsNotDeleted.
- Counts may be approximate when M-Files does not provide an exact measure.
- Search counts respect the monitoring identity's permissions and do not guarantee an all-vault privileged count.
- Daily Created counts are rolling historical searches, not a live event/audit stream.
- Internal M-Files Cloud filesystem logs are not assumed accessible.
- Exhaustive tracing of direct API calls from unrelated clients is not implemented.

## Not yet implemented / external integrations
- [ ] Microsoft Graph SSO/SCIM provisioning and sign-in logs.
- [ ] Aino usage and subscription/license providers.
- [ ] API request tracing for instrumented apps, approved proxy or official audit source.
- [ ] Per-class and per-object-type statistics.
- [ ] Notification delivery, alert rules and maintenance windows.
- [ ] Historical retention/archival policy and automated tests.
- [ ] Production Azure infrastructure, managed identity, SQL deployment and release pipeline.

Never commit real credentials, tokens, cookies or secret values.
