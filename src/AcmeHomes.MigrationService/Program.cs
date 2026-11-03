using AcmeHomes.Infrastructure.Persistence;
using AcmeHomes.Infrastructure.Persistence.Seeding;
using AcmeHomes.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

// Seeding is registered only here, so the API never writes seed data.
builder.AddSqlServerDbContext<AcmeHomesDbContext>(
    AcmeHomesDbContext.ConnectionName,
    configureDbContextOptions: options => options.UseAcmeHomesSeedData());

builder.Services.AddHostedService<MigrationWorker>();

builder.Build().Run();
