# MFCMP API — local setup

## Prerequisites
- .NET 10 SDK
- SQL Server or SQL Server Developer in Docker

## Configure
Copy `appsettings.Development.example.json` to `appsettings.Development.json` and update the SQL Server connection string. Do not commit the local file.

Alternatively use environment variables:
- `ConnectionStrings__DefaultConnection`
- `Database__ApplyMigrationsOnStartup=true`
- `BootstrapAdmin__CreateIfMissing=true`
- `BootstrapAdmin__Password` (optional in Development; defaults to `admin` only in Development)

## Initialize the database
From `backend/src/MFCMP.Api`:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.0
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Then run:

```powershell
dotnet run
```

The initial Development portal login is `admin / admin`. The first login must be followed by a password change. In non-Development environments, provide a strong one-time bootstrap password through a secure secret provider.

## M-Files Cloud diagnostic
The first connection test now uses the official `MFiles.Grpc` client to call the documented anonymous `QueryVersion` and `GetServerCapabilities` RPCs over TLS/HTTP2 on port 443. This proves the Cloud gRPC endpoint is reachable; it does not prove that the configured monitoring account can authenticate to the vault or has the required permissions. Those checks are the next connector step.

The official .NET client package is versioned per M-Files Server release. The project currently references `MFiles.Grpc 26.11.0`; review the package/server compatibility when targeting another Cloud release.
