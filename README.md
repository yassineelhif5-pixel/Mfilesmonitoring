# MFCMP — M-Files Cloud Monitoring Portal

Read-only monitoring portal for M-Files Cloud support teams.

## Stack
- ASP.NET Core .NET 10 API
- Angular 21 / TypeScript
- SQL Server / EF Core
- Official MFiles.Grpc .NET client
- Azure Key Vault for per-vault technical credentials

## Local setup
1. Install .NET 10 SDK, Node.js 22 and SQL Server.
2. Copy backend/src/MFCMP.Api/appsettings.Development.example.json to appsettings.Development.json.
3. Set ConnectionStrings:DefaultConnection to your local SQL Server.
4. From backend/src/MFCMP.Api run dotnet run. Database migrations apply when Database:ApplyMigrationsOnStartup is true.
5. In another terminal, from frontend run npm install then npm start.
6. Open http://localhost:4200.

Development bootstrap portal account: admin / admin. It is created once and must be changed on first login. Do not use this account as an M-Files identity.

## Per-vault Key Vault secret formats
Credentials:
{ "username": "mfcmp-monitor", "password": "the real password" }

Token:
{ "token": "the identity-provider token", "pluginName": "optional plugin", "configurationScope": "optional scope" }

Create these secrets directly in Azure Key Vault. Register only the secret URI in MFCMP. The API identity must have permission to read the secret. Secret URIs are restricted to Azure Key Vault DNS suffixes for public, US Government and China clouds.

## Current capabilities
- Add M-Files Cloud vaults and configure per-vault authentication.
- Manually test gRPC connectivity, authentication and vault session.
- Automatic health checks with configurable per-vault cadence.
- Rolling seven-day created-object counts and current non-deleted object totals.
- Dashboard status, incidents, deduplication and incident state management.
- Metrics collection failures are tracked as incidents and retried according to the configured interval.
- Strictly read-only M-Files operations.

## Metrics semantics
- The built-in Created property is ID 20 and is a timestamp.
- Daily created-object counts include objects visible to the monitoring account; to include deleted objects, that account also needs the M-Files right to see deleted objects.
- The total object count explicitly excludes deleted objects.
- M-Files exact/approximate count flags are retained. Counts are filtered by the monitoring account's permissions and are not necessarily privileged all-vault counts.
- Daily Created counts are historical searches, not a live audit event stream.

## Important boundaries
- M-Files API calls are read-only except session lifecycle (login and logout).
- Internal M-Files Cloud server filesystem logs are not assumed accessible.
- This project does not claim exhaustive tracing of API calls made directly by unrelated client applications.
- Microsoft Graph SSO/SCIM, Aino usage, licensing, notifications and production Azure infrastructure are future integrations.
