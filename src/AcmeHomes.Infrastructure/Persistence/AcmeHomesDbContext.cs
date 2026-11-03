using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Infrastructure.Persistence;

public sealed class AcmeHomesDbContext(DbContextOptions<AcmeHomesDbContext> options) : DbContext(options)
{
    /// <summary>The connection string name, matching the database resource name in the Aspire AppHost.</summary>
    public const string ConnectionName = "acmehomes";

    public DbSet<Market> Markets => Set<Market>();

    public DbSet<Community> Communities => Set<Community>();

    public DbSet<FloorPlan> FloorPlans => Set<FloorPlan>();

    public DbSet<CommunityFloorPlan> CommunityFloorPlans => Set<CommunityFloorPlan>();

    public DbSet<Home> Homes => Set<Home>();

    public DbSet<Feature> Features => Set<Feature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in this assembly, one class per entity.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcmeHomesDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money columns. Individual properties (such as bathrooms) override this where needed.
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);

        // Store enums as readable text rather than integers, so the data makes sense in SQL queries
        // and reordering enum members can never silently change what existing rows mean.
        configurationBuilder.Properties<HomeStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<PrimarySuiteLocation>().HaveConversion<string>().HaveMaxLength(20);
    }
}
