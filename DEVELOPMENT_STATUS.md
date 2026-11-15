# Development status

Updated: 2026-11-15

## Repository
- Repository: https://github.com/yassineelhif5-pixel/Mfilesmonitoring
- Default branch: main
- Initial repository state: empty; project initialized on main.

## Confirmed decisions
- M-Files Cloud only.
- .NET 10 / ASP.NET Core.
- Angular 21.2 LTS + TypeScript (selected to remain compatible with the available Node 22.16 toolchain).
- SQL Server / Azure SQL.
- M-Files gRPC primary + REST complementary (gRPC connector is not yet implemented).
- Azure hosting.
- Local portal authentication: bootstrap admin/admin in Development, mandatory password change.
- Portal admin identity is separate from per-vault M-Files monitoring identities.
- Microsoft Graph is a monitoring source only, not the portal login provider.
- Strictly read-only access to M-Files vaults.

## Completed in repository
- [x] Repository access and main branch verified.
- [x] Functional and technical specification.
- [x] Architecture and security documentation.
- [x] Source coverage policy.
- [x] .NET 10 API project and solution.
- [x] EF Core SQL Server entities for portal users, vault configuration and health-check history.
- [x] Local bootstrap administrator seeder; never resets an existing password.
- [x] First-login password-change endpoint with a strong password rule.
- [x] Claims-based API authorization blocking vault endpoints until the password is changed.
- [x] CSRF token endpoint and antiforgery validation on unsafe auth actions.
- [x] Vault registration API.
- [x] Read-only REST server-status probe with honest status REACHABLE_AUTH_NOT_TESTED.
- [x] Angular shell with login, mandatory password-change screen, vault list, vault registration and status test action.
- [x] Angular development proxy configuration.
- [x] GitHub Actions backend/frontend build workflow.

## Not yet implemented
- [ ] EF Core initial migration (must be generated and tested with .NET 10 SDK).
- [ ] Actual M-Files gRPC connector and authenticated vault-access check.
- [ ] Scheduled health worker.
- [ ] Key Vault credential references and per-vault monitoring-account configuration.
- [ ] Dashboard aggregates and historical charts.
- [ ] Incidents, API audit, event ingestion and document analytics.
- [ ] Microsoft Graph SSO/SCIM connectors.
- [ ] License and Aino providers.
- [ ] Reporting, notifications and Azure deployment.

## Validation status
- Repository files committed to main.
- Local compilation/tests have not been run in this environment: .NET SDK is not installed.
- Angular dependencies/build have not yet been installed or executed.
- GitHub Actions should provide the first remote compile check after the commit is pushed.

## External inputs required for live integration testing
- M-Files Cloud test vault host and Vault GUID.
- Dedicated M-Files monitoring account with minimum required read-only permissions.
- Approved authentication mechanism for that test vault.
- SQL Server/Azure SQL connection for integration testing.
- Optional Microsoft Graph app registration and least-privilege permissions.

Never commit credentials or secret values.

## Known limitations
- The initial REST server-status probe verifies endpoint reachability only, not vault authentication.
- Exact gRPC operations/authentication depend on the published M-Files Cloud API definitions and vault capabilities.
- Internal M-Files Cloud server filesystem logs are not assumed accessible.
- Exhaustive direct API tracing is not assumed; every source needs coverage metadata.
- License and Aino metrics require an official accessible data source.
- Deletion analytics require an audit/event source.

## Next task
Generate and validate the initial SQL migration, then implement the official M-Files Cloud gRPC connection and authentication/capability diagnostics before implementing the scheduled worker.
