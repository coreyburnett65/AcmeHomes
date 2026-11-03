using AcmeHomes.Infrastructure.Persistence;
using AcmeHomes.Infrastructure.Persistence.Seeding;
using AcmeHomes.Infrastructure.Tests;

using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

// One SQL Server container for the whole test assembly; starting it takes several seconds (see ADR 0006).
[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace AcmeHomes.Infrastructure.Tests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>The fixed "today" used for seeding, so generated move-in dates are the same on every run.</summary>
    public static readonly DateTimeOffset SeedTime = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();

        // Applies the real migrations, which also runs the seeder registered with UseAcmeHomesSeedData.
        await context.Database.MigrateAsync();
    }

    public AcmeHomesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AcmeHomesDbContext>().UseSqlServer(_container.GetConnectionString());
        options.UseAcmeHomesSeedData(new FixedTimeProvider(SeedTime));
        return new AcmeHomesDbContext(options.Options);
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
