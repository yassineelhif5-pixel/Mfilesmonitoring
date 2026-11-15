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

## Initialize and run
From `backend/src/MFCMP.Api`:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.0
dotnet ef database update
dotnet run
```

The initial Development portal login is `admin / admin`. The first login must be followed by a password change. In non-Development environments, provide a strong one-time bootstrap password through a secure secret provider.

## M-Files Cloud diagnostic
The connection test uses the official `MFiles.Grpc` 26.11.0 client and performs the documented anonymous `QueryVersion` and `GetServerCapabilities` RPCs over TLS/HTTP2 on port 443.

This confirms that the gRPC endpoint responds and reports the server version/capabilities. It does **not** validate the configured monitoring account, authenticate to the vault or prove vault-level permissions. Those checks require per-vault monitoring credentials and the authentication mechanism configured on that vault; they are the next connector step.

Official client and examples: https://github.com/M-Files/mfiles-grpc-dotnet
