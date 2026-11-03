using System.Text.Json;
using System.Text.Json.Serialization;

using AcmeHomes.Api.IntegrationTests;
using AcmeHomes.Infrastructure.Persistence;
using AcmeHomes.Infrastructure.Persistence.Seeding;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

// One API host and SQL Server container for the whole test assembly (see ADR 0006).
[assembly: AssemblyFixture(typeof(AcmeHomesApiFactory))]

namespace AcmeHomes.Api.IntegrationTests;

/// <summary>Runs the real API in memory against a SQL Server container with the real migrations and seed data.</summary>
public sealed class AcmeHomesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Matches the API's JSON settings: camelCase names and enums as strings.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        // Does what the MigrationService does in the running app: apply migrations, which also runs the seeder.
        var options = new DbContextOptionsBuilder<AcmeHomesDbContext>().UseSqlServer(_database.GetConnectionString());
        options.UseAcmeHomesSeedData();

        await using var context = new AcmeHomesDbContext(options.Options);
        await context.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting($"ConnectionStrings:{AcmeHomesDbContext.ConnectionName}", _database.GetConnectionString());

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
