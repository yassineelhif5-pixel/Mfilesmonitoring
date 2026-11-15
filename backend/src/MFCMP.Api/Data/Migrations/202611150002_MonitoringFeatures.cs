using MFCMP.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MFCMP.Api.Data.Migrations;

[DbContext(typeof(MonitoringDbContext))]
[Migration("202611150002_MonitoringFeatures")]
public sealed class MonitoringFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE [Vaults] ADD
                [AuthenticationMode] nvarchar(24) NOT NULL CONSTRAINT [DF_Vaults_AuthenticationMode] DEFAULT N'None',
                [CredentialSecretUri] nvarchar(2048) NULL,
                [AuthenticationPlugin] nvarchar(160) NULL,
                [AuthenticationScope] nvarchar(512) NULL,
                [HealthCheckIntervalSeconds] int NOT NULL CONSTRAINT [DF_Vaults_HealthCheckIntervalSeconds] DEFAULT 60,
                [MetricsCollectionIntervalMinutes] int NOT NULL CONSTRAINT [DF_Vaults_MetricsCollectionIntervalMinutes] DEFAULT 15;

            ALTER TABLE [VaultHealthChecks] ADD
                [AuthenticatedAccountName] nvarchar(256) NULL;

            CREATE TABLE [VaultDailyObjectStatistics] (
                [Id] uniqueidentifier NOT NULL,
                [VaultId] uniqueidentifier NOT NULL,
                [DateUtc] date NOT NULL,
                [CreatedCount] bigint NOT NULL,
                [CreatedCountIsExact] bit NOT NULL,
                [TotalObjectCount] bigint NOT NULL,
                [TotalObjectCountIsExact] bit NOT NULL,
                [CollectedAtUtc] datetimeoffset(7) NOT NULL,
                CONSTRAINT [PK_VaultDailyObjectStatistics] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_VaultDailyObjectStatistics_Vaults_VaultId]
                    FOREIGN KEY ([VaultId]) REFERENCES [Vaults] ([Id]) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX [IX_VaultDailyObjectStatistics_VaultId_DateUtc]
                ON [VaultDailyObjectStatistics] ([VaultId], [DateUtc]);

            CREATE TABLE [Incidents] (
                [Id] uniqueidentifier NOT NULL,
                [VaultId] uniqueidentifier NOT NULL,
                [Category] nvarchar(40) NOT NULL,
                [Severity] nvarchar(8) NOT NULL,
                [Status] nvarchar(24) NOT NULL,
                [Title] nvarchar(200) NOT NULL,
                [Description] nvarchar(2000) NOT NULL,
                [Fingerprint] nvarchar(128) NOT NULL,
                [FirstSeenUtc] datetimeoffset(7) NOT NULL,
                [LastSeenUtc] datetimeoffset(7) NOT NULL,
                [Occurrences] int NOT NULL,
                [AcknowledgedAtUtc] datetimeoffset(7) NULL,
                [ResolvedAtUtc] datetimeoffset(7) NULL,
                [LastErrorCode] nvarchar(100) NULL,
                CONSTRAINT [PK_Incidents] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Incidents_Vaults_VaultId]
                    FOREIGN KEY ([VaultId]) REFERENCES [Vaults] ([Id]) ON DELETE CASCADE
            );

            CREATE INDEX [IX_Incidents_Status_LastSeenUtc] ON [Incidents] ([Status], [LastSeenUtc]);
            CREATE INDEX [IX_Incidents_VaultId_FirstSeenUtc] ON [Incidents] ([VaultId], [FirstSeenUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS [Incidents];
            DROP TABLE IF EXISTS [VaultDailyObjectStatistics];
            ALTER TABLE [VaultHealthChecks] DROP COLUMN [AuthenticatedAccountName];
            ALTER TABLE [Vaults] DROP CONSTRAINT [DF_Vaults_AuthenticationMode];
            ALTER TABLE [Vaults] DROP CONSTRAINT [DF_Vaults_HealthCheckIntervalSeconds];
            ALTER TABLE [Vaults] DROP CONSTRAINT [DF_Vaults_MetricsCollectionIntervalMinutes];
            ALTER TABLE [Vaults] DROP COLUMN [AuthenticationMode], [CredentialSecretUri],
                [AuthenticationPlugin], [AuthenticationScope], [HealthCheckIntervalSeconds],
                [MetricsCollectionIntervalMinutes];
            """);
    }
}
