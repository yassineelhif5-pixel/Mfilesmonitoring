using MFCMP.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MFCMP.Api.Data.Migrations;

[DbContext(typeof(MonitoringDbContext))]
[Migration("202611150001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE [AspNetRoles] (
                [Id] nvarchar(450) NOT NULL,
                [Name] nvarchar(256) NULL,
                [NormalizedName] nvarchar(256) NULL,
                [ConcurrencyStamp] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
            );

            CREATE TABLE [AspNetUsers] (
                [Id] nvarchar(450) NOT NULL,
                [UserName] nvarchar(256) NULL,
                [NormalizedUserName] nvarchar(256) NULL,
                [Email] nvarchar(256) NULL,
                [NormalizedEmail] nvarchar(256) NULL,
                [EmailConfirmed] bit NOT NULL,
                [PasswordHash] nvarchar(max) NULL,
                [SecurityStamp] nvarchar(max) NULL,
                [ConcurrencyStamp] nvarchar(max) NULL,
                [PhoneNumber] nvarchar(max) NULL,
                [PhoneNumberConfirmed] bit NOT NULL,
                [TwoFactorEnabled] bit NOT NULL,
                [LockoutEnd] datetimeoffset(7) NULL,
                [LockoutEnabled] bit NOT NULL,
                [AccessFailedCount] int NOT NULL,
                [MustChangePassword] bit NOT NULL,
                [CreatedAtUtc] datetimeoffset(7) NOT NULL,
                CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
            );

            CREATE TABLE [Vaults] (
                [Id] uniqueidentifier NOT NULL,
                [Name] nvarchar(160) NOT NULL,
                [VaultGuid] uniqueidentifier NOT NULL,
                [CloudHost] nvarchar(512) NOT NULL,
                [Organization] nvarchar(160) NOT NULL,
                [Environment] nvarchar(32) NOT NULL,
                [MonitoringEnabled] bit NOT NULL,
                [CreatedAtUtc] datetimeoffset(7) NOT NULL,
                [UpdatedAtUtc] datetimeoffset(7) NOT NULL,
                CONSTRAINT [PK_Vaults] PRIMARY KEY ([Id])
            );

            CREATE TABLE [AspNetRoleClaims] (
                [Id] int NOT NULL IDENTITY,
                [RoleId] nvarchar(450) NOT NULL,
                [ClaimType] nvarchar(max) NULL,
                [ClaimValue] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
                    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [AspNetUserClaims] (
                [Id] int NOT NULL IDENTITY,
                [UserId] nvarchar(450) NOT NULL,
                [ClaimType] nvarchar(max) NULL,
                [ClaimValue] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [AspNetUserLogins] (
                [LoginProvider] nvarchar(450) NOT NULL,
                [ProviderKey] nvarchar(450) NOT NULL,
                [ProviderDisplayName] nvarchar(max) NULL,
                [UserId] nvarchar(450) NOT NULL,
                CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
                CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [AspNetUserRoles] (
                [UserId] nvarchar(450) NOT NULL,
                [RoleId] nvarchar(450) NOT NULL,
                CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
                CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
                    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [AspNetUserTokens] (
                [UserId] nvarchar(450) NOT NULL,
                [LoginProvider] nvarchar(450) NOT NULL,
                [Name] nvarchar(450) NOT NULL,
                [Value] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
                CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
            );

            CREATE TABLE [VaultHealthChecks] (
                [Id] uniqueidentifier NOT NULL,
                [VaultId] uniqueidentifier NOT NULL,
                [CheckedAtUtc] datetimeoffset(7) NOT NULL,
                [Status] nvarchar(40) NOT NULL,
                [HttpStatusCode] int NULL,
                [DurationMs] bigint NOT NULL,
                [AuthenticationStatus] nvarchar(40) NOT NULL,
                [ServerVersion] nvarchar(max) NULL,
                [CapabilitiesSummary] nvarchar(max) NULL,
                [Message] nvarchar(1000) NOT NULL,
                CONSTRAINT [PK_VaultHealthChecks] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_VaultHealthChecks_Vaults_VaultId]
                    FOREIGN KEY ([VaultId]) REFERENCES [Vaults] ([Id]) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName])
                WHERE [NormalizedName] IS NOT NULL;
            CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
            CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
            CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName])
                WHERE [NormalizedUserName] IS NOT NULL;
            CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
            CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
            CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
            CREATE UNIQUE INDEX [IX_Vaults_VaultGuid] ON [Vaults] ([VaultGuid]);
            CREATE INDEX [IX_Vaults_Environment_MonitoringEnabled] ON [Vaults] ([Environment], [MonitoringEnabled]);
            CREATE INDEX [IX_VaultHealthChecks_VaultId_CheckedAtUtc] ON [VaultHealthChecks] ([VaultId], [CheckedAtUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS [VaultHealthChecks];
            DROP TABLE IF EXISTS [AspNetUserTokens];
            DROP TABLE IF EXISTS [AspNetUserRoles];
            DROP TABLE IF EXISTS [AspNetUserLogins];
            DROP TABLE IF EXISTS [AspNetUserClaims];
            DROP TABLE IF EXISTS [AspNetRoleClaims];
            DROP TABLE IF EXISTS [Vaults];
            DROP TABLE IF EXISTS [AspNetUsers];
            DROP TABLE IF EXISTS [AspNetRoles];
            """);
    }
}
