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

The initial Development portal login is `admin / admin`. The first login must be followed by a password change. In non-Development environments, provide a strong one-time bootstrap password through Azure Key Vault or another secure secret provider.

## Initial implementation boundary
The current connection test calls MFWS `GET /REST/server/status`. A successful response proves endpoint reachability only; it does not validate credentials, vault-level access or gRPC. Those are separate implementation steps. The probe is read-only.
