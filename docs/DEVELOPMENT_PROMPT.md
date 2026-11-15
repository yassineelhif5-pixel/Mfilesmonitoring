# Master development prompt — MFCMP

Act as a senior .NET, Angular, SQL Server, Azure and M-Files Cloud engineer. Build the complete application in this repository, not a mockup.

## Fixed decisions
- M-Files Cloud only.
- .NET 10 / ASP.NET Core.
- Angular + TypeScript.
- SQL Server / Azure SQL.
- M-Files gRPC primary + REST complementary.
- Azure deployment.
- Portal authentication: local bootstrap admin/admin, change password required at first login.
- The portal admin account is strictly separate from per-vault M-Files read-only monitoring identities.
- No Entra ID portal login. Microsoft Graph is a monitoring connector only.
- M-Files integration strictly read-only.

## Official documentation first
Study https://developer.m-files.com/ and the official gRPC/REST documentation and installed .proto/package definitions before implementing any method. Never invent endpoints, RPC names, authentication flows or feature availability.

## Mandatory first vertical slice
Implement this end-to-end:
1. Create .NET solution and modular projects.
2. Create Angular application shell.
3. Configure SQL Server and EF Core migrations.
4. Implement local Identity bootstrap admin exactly once; force password change.
5. Implement vault registry with encrypted Key Vault credential references.
6. Implement capability-aware M-Files Cloud connector using actual official gRPC/REST contracts.
7. Implement a safe connection diagnostic distinguishing network, protocol, server status, authentication and vault access.
8. Implement a scheduled read-only health worker.
9. Persist health history in SQL Server.
10. Display live vault list, health state, last check and latency in Angular.

## Read-only enforcement
Do not implement any M-Files write operation. Do not call object create/update/delete, workflow changes, metadata writes, permission changes, user administration or vault administration. Session login/keepalive/logout needed to read are permitted. Enforce read-only at the service interface, tests and M-Files account permission level.

## Modules after vertical slice
- Dashboard
- Health monitoring
- Incidents and error deduplication
- API observability with explicit capture source/completeness
- M-Files audit/events accessible through supported interfaces
- Document analytics
- User and service account analytics
- Entra sign-in and SCIM monitoring through Microsoft Graph
- License and Aino provider abstractions, only implement metrics with confirmed official sources
- Alerts and reporting

## Quality rules
- No fake production data.
- No dead UI controls.
- No secrets in repository or logs.
- UTC timestamps.
- Pagination and incremental cursors.
- Isolate failures per vault.
- Timeout, retry/backoff, circuit breaker and rate limits.
- Distributed job locking.
- Unit, integration and connector tests.
- DEV/TEST/PROD configuration.
- Docker and GitHub Actions.
- Update DEVELOPMENT_STATUS.md at every phase with what is implemented, tests run, known limitations and next task.

Start by inspecting the repository and environment. Implement in small compiling increments. Build and test after each phase. If credentials are unavailable, implement the connector, configuration and diagnostics honestly, mark NOT_CONFIGURED and continue with other modules. Do not claim deployment or live M-Files validation without actually performing them.
