using AcmeHomes.Domain;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Infrastructure.Tests;

/// <summary>Checks mapping behavior that only a real SQL Server can prove.</summary>
public class PersistenceTests(SqlServerFixture database)
{
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Amenities_round_trip_through_the_json_column()
    {
        await using var context = database.CreateContext();

        var community = await context.Communities.AsNoTracking().SingleAsync(c => c.Slug == "juniper-hollow", _cancellationToken);

        Assert.Equal(["Walking trails", "Playground", "Dog park"], community.Amenities);
    }

    [Fact]
    public async Task Optional_hoa_round_trips_as_null_or_value()
    {
        await using var context = database.CreateContext();

        var noHoa = await context.Communities.AsNoTracking().SingleAsync(c => c.Slug == "linden-fields", _cancellationToken);
        var withHoa = await context.Communities.AsNoTracking().SingleAsync(c => c.Slug == "stonebrook-meadows", _cancellationToken);

        Assert.Null(noHoa.Hoa);
        Assert.Equal(140m, withHoa.Hoa?.MonthlyFee);
    }

    [Fact]
    public async Task Database_rejects_a_home_whose_plan_is_not_offered_in_its_community()
    {
        await using var context = database.CreateContext();

        // Juniper Hollow doesn't offer the Hartwell plan. Raw SQL bypasses the domain model to prove the database guards this too.
        var communityId = await context.Communities.Where(c => c.Slug == "juniper-hollow").Select(c => c.Id).SingleAsync(_cancellationToken);
        var floorPlanId = await context.FloorPlans.Where(p => p.Slug == "hartwell").Select(p => p.Id).SingleAsync(_cancellationToken);

        var exception = await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO Homes (CommunityId, FloorPlanId, LotNumber, Status, Price)
            VALUES ({communityId}, {floorPlanId}, '999', 'Available', 700000)
            """,
            _cancellationToken));

        Assert.Contains("FK_Homes_CommunityFloorPlans", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Concurrent_inventory_updates_to_the_same_home_are_detected()
    {
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();

        var homeId = await first.Homes.Where(h => h.Status == HomeStatus.Available).Select(h => h.Id).FirstAsync(_cancellationToken);
        var firstCopy = await first.Homes.SingleAsync(h => h.Id == homeId, _cancellationToken);
        var secondCopy = await second.Homes.SingleAsync(h => h.Id == homeId, _cancellationToken);

        firstCopy.UpdateInventory(HomeStatus.Available, firstCopy.Price + 1_000m, null);
        await first.SaveChangesAsync(_cancellationToken);

        // The second context still holds the old RowVersion, so its write must fail rather than overwrite the first.
        secondCopy.UpdateInventory(HomeStatus.Sold, secondCopy.Price, null);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(_cancellationToken));
    }
}
