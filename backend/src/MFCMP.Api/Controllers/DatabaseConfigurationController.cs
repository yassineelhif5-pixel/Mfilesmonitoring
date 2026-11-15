using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MFCMP.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalPasswordChanged")]
[Route("api/configuration/database")]
public sealed class DatabaseConfigurationController(IConfiguration configuration, ILogger<DatabaseConfigurationController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return Ok(new { configured = false, provider = "Microsoft SQL Server", message = "La chaîne ConnectionStrings:DefaultConnection est absente." });

        var builder = new SqlConnectionStringBuilder(connectionString);
        return Ok(new
        {
            configured = true,
            provider = "Microsoft SQL Server",
            server = builder.DataSource,
            database = builder.InitialCatalog,
            authentication = builder.IntegratedSecurity ? "Windows / Integrated Security" :
                string.IsNullOrWhiteSpace(builder.UserID) ? "Authentication not specified" : "SQL Server Authentication",
            encrypt = builder.Encrypt.ToString(),
            trustServerCertificate = builder.TrustServerCertificate
        });
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return Ok(new { success = false, code = "MFCMP_DB_NOT_CONFIGURED", message = "Aucune chaîne SQL n'est configurée.", explanation = "Renseigne ConnectionStrings:DefaultConnection dans la configuration de l'API, puis redémarre le backend." });

        var builder = new SqlConnectionStringBuilder(connectionString);
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    CAST(SERVERPROPERTY('ServerName') AS nvarchar(256)) AS ServerName,
                    DB_NAME() AS DatabaseName,
                    SUSER_SNAME() AS LoginName,
                    CAST(DATABASEPROPERTYEX(DB_NAME(), 'Status') AS nvarchar(60)) AS DatabaseStatus,
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') AS CanCreateTable,
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER ANY SCHEMA') AS CanAlterSchema;
                """;
            await using var reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            var canCreateTable = reader.GetInt32(4) == 1;
            var canAlterSchema = reader.GetInt32(5) == 1;
            var permissionsOk = canCreateTable && canAlterSchema;
            return Ok(new
            {
                success = true,
                code = permissionsOk ? "MFCMP_DB_READY" : "MFCMP_DB_CONNECTED_LIMITED_PERMISSIONS",
                server = reader.GetString(0),
                database = reader.GetString(1),
                login = reader.IsDBNull(2) ? null : reader.GetString(2),
                databaseStatus = reader.GetString(3),
                canCreateTable,
                canAlterSchema,
                migrationsReady = permissionsOk,
                message = permissionsOk ? "Connexion SQL réussie. Les permissions nécessaires aux migrations sont détectées." : "Connexion SQL réussie, mais les permissions de création/modification du schéma sont insuffisantes.",
                explanation = permissionsOk ? "Le portail atteint la base et le compte SQL peut créer des tables et modifier un schéma. Les migrations EF Core peuvent être lancées sous réserve des autres politiques SQL." : "Le portail peut ouvrir une session SQL, mais le compte ne dispose pas de CREATE TABLE et/ou ALTER ANY SCHEMA. Demande ces droits au DBA pour appliquer les migrations EF Core. En production, privilégie un compte de déploiement dédié pour les migrations."
            });
        }
        catch (SqlException ex)
        {
            logger.LogWarning(ex, "SQL configuration test failed for server {Server} and database {Database}", builder.DataSource, builder.InitialCatalog);
            var explanation = ex.Number switch
            {
                2 or 53 or 10060 => "Serveur SQL introuvable ou inaccessible. Vérifie le nom du serveur, le port TCP, le DNS, le pare-feu et que TCP/IP est activé.",
                18456 => "Échec d'authentification SQL. Vérifie le login, le mot de passe et le mode d'authentification autorisé.",
                4060 => "La base demandée est introuvable ou le login n'a pas le droit d'y accéder. Vérifie Initial Catalog et les droits du compte.",
                262 or 916 => "Le login n'est pas autorisé à accéder à la base. Le DBA doit créer l'utilisateur correspondant et lui attribuer les droits requis.",
                233 or 10054 => "La connexion a été interrompue par le serveur ou le réseau. Vérifie TLS, le chiffrement, le pare-feu et les journaux SQL Server.",
                _ => "Erreur SQL Server. Consulte le numéro d'erreur et le message technique ci-dessous, puis vérifie la configuration réseau, TLS et les droits du compte."
            };
            return Ok(new { success = false, code = $"SQL_{ex.Number}", message = "Le test de connexion SQL a échoué.", explanation, technicalMessage = ex.Message, sqlErrorNumber = ex.Number, server = builder.DataSource, database = builder.InitialCatalog });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected SQL configuration test error");
            return Ok(new { success = false, code = "MFCMP_DB_TEST_ERROR", message = "Une erreur inattendue empêche le test SQL.", explanation = "Vérifie la configuration du backend et les journaux de l'application. Le détail technique est journalisé côté serveur.", technicalMessage = ex.Message });
        }
    }
}
