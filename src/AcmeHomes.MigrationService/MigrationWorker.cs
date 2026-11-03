using System.Diagnostics;

using AcmeHomes.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.MigrationService;

/// <summary>
/// Applies pending EF Core migrations (which also runs the seeder), grants the API read access in Azure, and then
/// stops the process. Locally, the Aspire AppHost starts the API only after this exits successfully; in Azure it runs
/// as a Container Apps Job before each API deployment, so the API never needs permission to change the schema.
/// </summary>
internal sealed partial class MigrationWorker(
    IServiceProvider services,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    public const string ActivitySourceName = "AcmeHomes.MigrationService";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrate database", ActivityKind.Client);

        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AcmeHomesDbContext>();

            // EF Core takes a database lock during migration, so concurrent runs can't apply the same migration twice.
            await context.Database.MigrateAsync(stoppingToken);
            LogMigrationCompleted(logger);

            await GrantApiReadAccessAsync(context, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);

            // A non-zero exit code tells Aspire's WaitForCompletion (and a Container Apps Job) that migration failed.
            Environment.ExitCode = 1;
            throw;
        }

        lifetime.StopApplication();
    }

    /// <summary>
    /// In Azure, makes sure the API's managed identity has a read-only database user (see ADR 0008). Skipped locally,
    /// where both services connect with the SQL Server container's admin login and these settings aren't present.
    /// </summary>
    private async Task GrantApiReadAccessAsync(AcmeHomesDbContext context, CancellationToken cancellationToken)
    {
        var identityName = configuration["DatabaseAccess:ApiIdentityName"];
        var clientIdValue = configuration["DatabaseAccess:ApiIdentityClientId"];

        if (string.IsNullOrWhiteSpace(identityName) || !Guid.TryParse(clientIdValue, out var clientId))
        {
            return;
        }

        // CREATE USER ... WITH SID, TYPE = E maps the user to the managed identity by its client ID, so the SQL server
        // doesn't need Microsoft Graph permissions to look the identity up by name. User and role names can't be SQL
        // parameters, so they're added with QUOTENAME in dynamic SQL; the values themselves stay parameterized.
        // Both statements are idempotent, so this runs safely on every deployment.
        await context.Database.ExecuteSqlAsync(
            $"""
            DECLARE @name sysname = {identityName};
            DECLARE @sid varbinary(16) = CONVERT(varbinary(16), {clientId});

            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @name)
            BEGIN
                DECLARE @create nvarchar(max) =
                    N'CREATE USER ' + QUOTENAME(@name) + N' WITH SID = ' + CONVERT(nvarchar(max), @sid, 1) + N', TYPE = E;';
                EXEC sp_executesql @create;
            END;

            IF IS_ROLEMEMBER(N'db_datareader', @name) = 0
            BEGIN
                DECLARE @grant nvarchar(max) = N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@name) + N';';
                EXEC sp_executesql @grant;
            END;
            """,
            cancellationToken);

        LogApiReadAccessGranted(logger, identityName);
    }

    // Source-generated logging avoids boxing and message parsing on every call (CA1848).
    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations and seeding completed.")]
    private static partial void LogMigrationCompleted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Read-only database access confirmed for managed identity {IdentityName}.")]
    private static partial void LogApiReadAccessGranted(ILogger logger, string identityName);
}
