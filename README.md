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

Create these secrets directly in Azure Key Vault. Register only the secret URI in MFCMP. The API identity must have permission to read the secret.

## Current capabilities
- Add M-Files Cloud vaults and configure per-vault authentication.
- Manually test gRPC connectivity, authentication and vault session.
- Automatic health checks with configurable per-vault cadence.
- Rolling seven-day created-object counts and current non-deleted object totals.
- Dashboard status, incidents, deduplication and incident state management.
- Strictly read-only M-Files operations.

## Important
- Counts are filtered by the permissions of the configured M-Files monitoring account.
- M-Files exact/approximate count flags are retained.
- Daily created-object counts are historical searches on the built-in Created property (ID 20), not a live audit stream.
- Internal M-Files Cloud server filesystem logs are not assumed accessible.
- This project does not claim exhaustive tracing of API calls made directly by unrelated client applications.
- Microsoft Graph SSO/SCIM, Aino usage, licensing, notifications and production Azure infrastructure are future integrations.
