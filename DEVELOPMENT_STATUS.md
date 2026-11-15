# Development status

Updated: 2026-11-15

## Repository
- Repository: https://github.com/yassineelhif5-pixel/Mfilesmonitoring
- Default branch: main
- Changes are committed directly to main.

## Confirmed decisions
- M-Files Cloud only.
- .NET 10 / ASP.NET Core.
- Angular 21 + TypeScript.
- SQL Server / Azure SQL.
- Official M-Files .NET gRPC client, version 26.11.0, as the primary connector.
- M-Files REST is a complementary connector for documented supported endpoints.
- Azure hosting.
- Local portal authentication: bootstrap admin/admin in Development, mandatory password change.
- Portal admin identity is separate from per-vault M-Files monitoring identities.
- Microsoft Graph is a monitoring source only, not the portal login provider.
- Strictly read-only access to M-Files vaults.

## Completed in repository
- [x] Repository access and main branch verified.
- [x] Functional and technical specification.
- [x] Architecture and security documentation.
- [x] Data-source coverage policy.
- [x] .NET 10 API project and solution.
- [x] SQL Server EF Core entities for portal users, vault configuration and health history.
- [x] Initial SQL Server migration for ASP.NET Identity, vaults and health history.
- [x] Local bootstrap administrator seeder; never resets an existing password.
- [x] First-login password-change endpoint with strong password validation.
- [x] Claims-based API authorization blocking monitoring endpoints until password has been changed.
- [x] CSRF token endpoint and antiforgery validation for unsafe authentication actions.
- [x] Vault registration API.
- [x] Official M-Files gRPC client dependency (MFiles.Grpc 26.11.0).
- [x] Anonymous gRPC diagnostics using QueryVersion and GetServerCapabilities.
- [x] Angular shell with login, mandatory password-change screen, vault list, vault registration and gRPC diagnostic action.
- [x] Angular development proxy configuration.
- [x] GitHub Actions backend and frontend build workflow.

## Validation
- [x] GitHub Actions backend restore and .NET build passed on commit 62e24d7426d782045b6ba7752c4137b526d904cd.
- [x] GitHub Actions Angular install and production build passed on the same commit.
- [ ] SQL migration application against a live SQL Server instance.
- [ ] Live gRPC connection against an authorized M-Files Cloud test vault.
- [ ] Authenticated vault access check using the configured read-only monitoring identity.

Local compilation has not been run in this environment; the successful compile evidence is GitHub Actions CI.

## Not yet implemented
- [ ] Per-vault monitoring identity configuration and secure Key Vault references.
- [ ] Authenticated gRPC vault diagnostic.
- [ ] KeepAlive lifecycle for long-running sessions.
- [ ] Scheduled health worker and historical collection.
- [ ] Dashboard aggregates and historical charts.
- [ ] Incidents and error deduplication.
- [ ] API audit/event ingestion and document analytics.
- [ ] Microsoft Graph SSO/SCIM connectors.
- [ ] License and Aino providers.
- [ ] Reporting, notifications and Azure deployment.

## External inputs required for live integration tests
- M-Files Cloud test vault host and Vault GUID.
- Dedicated M-Files monitoring account with minimum required read-only permissions.
- Authentication method configured on that vault (M-Files credentials or supported identity-provider plugin/token).
- SQL Server/Azure SQL connection for migration and runtime tests.
- Optional Microsoft Graph app registration and least-privilege permissions.

Never commit credentials or secret values.

## Known limitations
- The current gRPC diagnostic calls anonymous QueryVersion and GetServerCapabilities. It proves server endpoint reachability and capability discovery only; it does not authenticate the monitoring account or prove vault access.
- The next connector step must configure a distinct monitoring identity per vault, store secrets in Azure Key Vault, authenticate using the actual supported vault authentication plugin/flow, and validate read-only vault access.
- Internal M-Files Cloud server filesystem logs are not assumed accessible.
- Exhaustive direct API tracing is not assumed; every source needs capture coverage metadata.
- License and Aino metrics require an official accessible data source.
- Deletion analytics require an audit/event source.
