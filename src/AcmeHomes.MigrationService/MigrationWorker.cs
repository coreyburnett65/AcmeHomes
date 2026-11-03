using System.Diagnostics;

using AcmeHomes.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.MigrationService;

/// <summary>
/// Applies pending EF Core migrations (which also runs the seeder) and then stops the process. Locally, the Aspire
/// AppHost starts the API only after this exits successfully; in Azure the same project can run as a Container Apps
/// Job during deployment, so the API never needs permission to change the schema.
/// </summary>
internal sealed partial class MigrationWorker(
    IServiceProvider services,
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

    // Source-generated logging avoids boxing and message parsing on every call (CA1848).
    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations and seeding completed.")]
    private static partial void LogMigrationCompleted(ILogger logger);
}
