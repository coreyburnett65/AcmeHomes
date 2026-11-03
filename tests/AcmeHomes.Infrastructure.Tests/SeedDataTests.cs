using AcmeHomes.Domain;
using AcmeHomes.Infrastructure.Persistence.Seeding;

using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Infrastructure.Tests;

public class SeedDataTests(SqlServerFixture database)
{
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Seeds_the_demo_catalog()
    {
        await using var context = database.CreateContext();

        Assert.Equal(2, await context.Markets.CountAsync(_cancellationToken));
        Assert.Equal(6, await context.Communities.CountAsync(_cancellationToken));
        Assert.Equal(8, await context.FloorPlans.CountAsync(_cancellationToken));
        Assert.Equal(36, await context.Homes.CountAsync(_cancellationToken));
    }

    [Fact]
    public async Task Running_the_seeder_again_adds_nothing()
    {
        await using var context = database.CreateContext();
        var homesBefore = await context.Homes.CountAsync(_cancellationToken);

        await SeedData.SeedAsync(context, TimeProvider.System, _cancellationToken);

        Assert.Equal(homesBefore, await context.Homes.CountAsync(_cancellationToken));
    }

    [Fact]
    public async Task Same_plan_is_priced_differently_by_community()
    {
        await using var context = database.CreateContext();

        var carverPrices = await context.CommunityFloorPlans
            .Where(o => o.FloorPlan.Slug == "carver")
            .Select(o => o.BasePrice)
            .Distinct()
            .CountAsync(_cancellationToken);

        Assert.True(carverPrices > 1);
    }

    [Fact]
    public async Task Homes_under_construction_have_move_in_dates_after_the_seed_date()
    {
        await using var context = database.CreateContext();
        var seedDate = DateOnly.FromDateTime(SqlServerFixture.SeedTime.UtcDateTime);

        var moveInDates = await context.Homes
            .Where(h => h.Status == HomeStatus.UnderConstruction)
            .Select(h => h.EstimatedMoveInDate)
            .ToListAsync(_cancellationToken);

        Assert.NotEmpty(moveInDates);
        Assert.All(moveInDates, d => Assert.True(d > seedDate));
    }
}
