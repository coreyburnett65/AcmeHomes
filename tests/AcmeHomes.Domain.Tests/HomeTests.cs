namespace AcmeHomes.Domain.Tests;

public class HomeTests
{
    private static readonly DateOnly MoveInDate = new(2027, 3, 15);

    [Fact]
    public void Takes_community_and_floor_plan_from_the_offering()
    {
        var offering = TestData.Offering();

        var home = new Home(offering, "Lot 12", HomeStatus.Available, 459_900m);

        Assert.Same(offering.Community, home.Community);
        Assert.Same(offering.FloorPlan, home.FloorPlan);
    }

    [Fact]
    public void Under_construction_home_requires_move_in_date() =>
        Assert.Throws<ArgumentException>(() => new Home(TestData.Offering(), "Lot 12", HomeStatus.UnderConstruction, 459_900m));

    [Fact]
    public void Available_home_does_not_require_move_in_date() =>
        Assert.Null(new Home(TestData.Offering(), "Lot 12", HomeStatus.Available, 459_900m).EstimatedMoveInDate);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_non_positive_price(decimal price) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Home(TestData.Offering(), "Lot 12", HomeStatus.Available, price));

    [Fact]
    public void UpdateInventory_applies_all_values()
    {
        var home = new Home(TestData.Offering(), "Lot 12", HomeStatus.UnderConstruction, 459_900m, MoveInDate);

        home.UpdateInventory(HomeStatus.Sold, 455_000m, null);

        Assert.Equal(HomeStatus.Sold, home.Status);
        Assert.Equal(455_000m, home.Price);
        Assert.Null(home.EstimatedMoveInDate);
    }

    [Fact]
    public void Invalid_UpdateInventory_leaves_home_unchanged()
    {
        var home = new Home(TestData.Offering(), "Lot 12", HomeStatus.UnderConstruction, 459_900m, MoveInDate);

        Assert.Throws<ArgumentOutOfRangeException>(() => home.UpdateInventory(HomeStatus.Available, -5m, null));

        Assert.Equal(HomeStatus.UnderConstruction, home.Status);
        Assert.Equal(459_900m, home.Price);
        Assert.Equal(MoveInDate, home.EstimatedMoveInDate);
    }
}
