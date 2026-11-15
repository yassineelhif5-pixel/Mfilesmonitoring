# Development status

Updated: 2026-11-15

## Repository
- Repository: yassineelhif5-pixel/Mfilesmonitoring
- Default branch: main
- Initial repository state: empty

## Confirmed decisions
- M-Files Cloud only.
- .NET 10 / ASP.NET Core.
- Angular + TypeScript.
- SQL Server / Azure SQL.
- M-Files gRPC + REST hybrid, with gRPC as primary connector.
- Azure hosting.
- Local portal authentication; bootstrap administrator `admin/admin`.
- The portal administrator identity is separate from all M-Files monitoring identities.
- Microsoft Graph is a monitoring data source for SSO/SCIM, not the portal login provider.
- Strictly read-only access to M-Files vault content and configuration.

## Completed
- [x] Repository access verified.
- [x] Confirmed default branch `main`.
- [x] Confirmed repository was empty before initialization.
- [x] Added initial project documentation and development conventions.

## In progress
- [ ] Create .NET solution and modular projects.
- [ ] Create Angular application shell and navigation.
- [ ] Configure SQL Server and EF Core migrations.
- [ ] Implement secure local bootstrap administrator.
- [ ] Implement vault registry and connection diagnostics.
- [ ] Implement M-Files Cloud gRPC connector after checking official package/proto/API support.
- [ ] Implement read-only health worker and first dashboard.

## Not yet implemented
All runtime application modules, real API connectors, database migrations, tests, Azure resources and deployment pipelines.

## External inputs required for live integration tests
- M-Files Cloud test vault host and Vault GUID.
- Dedicated M-Files monitoring account with minimum required read-only permissions.
- Approved authentication mechanism for that test vault.
- SQL Server/Azure SQL connection for deployment testing.
- Optional Microsoft Graph application registration and minimum permissions for identity monitoring.

Do not commit credentials or secret values.

## Known limitations to validate
- Exact gRPC operations and authentication support depend on the M-Files Cloud deployment and published API definitions.
- M-Files Cloud internal server filesystem logs are not assumed accessible.
- Exhaustive direct API call tracing is not assumed; collection sources and completeness must be labelled.
- License and Aino usage metrics require confirmation of an official accessible source.
- Deletion statistics require an appropriate audit/event source.

## Next task
Implement the first vertical slice: local login, vault configuration, read-only connection diagnostics, scheduled health checks, SQL persistence and Angular health dashboard.
