using MFCMP.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MFCMP.Api.Data.Migrations;

[DbContext(typeof(MonitoringDbContext))]
[Migration("202611170001_TenantsServiceAccountsAndLogs")]
public sealed class TenantsServiceAccountsAndLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE [Tenants] (
                [Id] uniqueidentifier NOT NULL,
                [VaultId] uniqueidentifier NOT NULL,
                [Name] nvarchar(160) NOT NULL,
                [Code] nvarchar(80) NOT NULL,
                [IsActive] bit NOT NULL,
                [CreatedAtUtc] datetimeoffset(7) NOT NULL,
                CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Tenants_Vaults_VaultId] FOREIGN KEY ([VaultId])
                    REFERENCES [Vaults] ([Id]) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX [IX_Tenants_VaultId] ON [Tenants] ([VaultId]);
            CREATE UNIQUE INDEX [IX_Tenants_Code] ON [Tenants] ([Code]);

            INSERT INTO [Tenants] ([Id], [VaultId], [Name], [Code], [IsActive], [CreatedAtUtc])
            SELECT NEWID(), [Id], [Name], CONCAT('VAULT-', REPLACE(CONVERT(nvarchar(36), [Id]), '-', '')), 1, SYSUTCDATETIME()
            FROM [Vaults];

            CREATE TABLE [ServiceAccounts] (
                [Id] uniqueidentifier NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [Name] nvarchar(160) NOT NULL,
                [AccountName] nvarchar(256) NOT NULL,
                [MFilesAccountId] nvarchar(128) NULL,
                [AuthenticationMode] nvarchar(24) NOT NULL,
                [CredentialSecretUri] nvarchar(2048) NOT NULL,
                [IsPrimary] bit NOT NULL,
                [IsEnabled] bit NOT NULL,
                [CreatedAtUtc] datetimeoffset(7) NOT NULL,
                [LastUsedAtUtc] datetimeoffset(7) NULL,
                CONSTRAINT [PK_ServiceAccounts] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_ServiceAccounts_Tenants_TenantId] FOREIGN KEY ([TenantId])
                    REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX [IX_ServiceAccounts_TenantId_AccountName] ON [ServiceAccounts] ([TenantId], [AccountName]);

            CREATE TABLE [MonitoringLogs] (
                [Id] uniqueidentifier NOT NULL,
                [TenantId] uniqueidentifier NOT NULL,
                [VaultId] uniqueidentifier NOT NULL,
                [ServiceAccountId] uniqueidentifier NULL,
                [OccurredAtUtc] datetimeoffset(7) NOT NULL,
                [Source] nvarchar(40) NOT NULL,
                [Category] nvarchar(40) NOT NULL,
                [Level] nvarchar(16) NOT NULL,
                [EventName] nvarchar(160) NOT NULL,
                [Message] nvarchar(2000) NOT NULL,
                [Details] nvarchar(max) NULL,
                [ErrorCode] nvarchar(128) NULL,
                [CorrelationId] nvarchar(128) NULL,
                CONSTRAINT [PK_MonitoringLogs] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_MonitoringLogs_Tenants_TenantId] FOREIGN KEY ([TenantId])
                    REFERENCES [Tenants] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_MonitoringLogs_Vaults_VaultId] FOREIGN KEY ([VaultId])
                    REFERENCES [Vaults] ([Id]) ON DELETE NO ACTION,
                CONSTRAINT [FK_MonitoringLogs_ServiceAccounts_ServiceAccountId] FOREIGN KEY ([ServiceAccountId])
                    REFERENCES [ServiceAccounts] ([Id]) ON DELETE SET NULL
            );
            CREATE INDEX [IX_MonitoringLogs_TenantId_OccurredAtUtc] ON [MonitoringLogs] ([TenantId], [OccurredAtUtc]);
            CREATE INDEX [IX_MonitoringLogs_TenantId_Category_Level_OccurredAtUtc] ON [MonitoringLogs] ([TenantId], [Category], [Level], [OccurredAtUtc]);
            CREATE INDEX [IX_MonitoringLogs_ServiceAccountId_OccurredAtUtc] ON [MonitoringLogs] ([ServiceAccountId], [OccurredAtUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS [MonitoringLogs];
            DROP TABLE IF EXISTS [ServiceAccounts];
            DROP TABLE IF EXISTS [Tenants];
            """);
    }
}
