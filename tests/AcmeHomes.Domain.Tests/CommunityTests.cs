namespace AcmeHomes.Domain.Tests;

public class CommunityTests
{
    [Fact]
    public void Amenities_are_trimmed_and_deduplicated_case_insensitively()
    {
        var community = new Community(
            TestData.Market(),
            "Willow Creek",
            "willow-creek",
            "Frederick",
            "MD",
            new GeoLocation(39.41, -77.41),
            "Frederick County Public Schools",
            amenities: ["Pool", " pool ", "Walking trails"]);

        Assert.Equal(["Pool", "Walking trails"], community.Amenities);
    }

    [Fact]
    public void OfferFloorPlan_creates_offering_with_community_specific_price()
    {
        var community = TestData.Community();
        var plan = TestData.FloorPlan();

        var offering = community.OfferFloorPlan(plan, 449_900m);

        Assert.Same(community, offering.Community);
        Assert.Same(plan, offering.FloorPlan);
        Assert.Equal(449_900m, offering.BasePrice);
        Assert.Contains(offering, community.CommunityFloorPlans);
    }

    [Fact]
    public void Same_plan_can_have_different_prices_in_different_communities()
    {
        var plan = TestData.FloorPlan();

        var frederick = TestData.Community().OfferFloorPlan(plan, 449_900m);
        var baltimore = TestData.Community(new Market("Baltimore", "baltimore", "MD")).OfferFloorPlan(plan, 519_900m);

        Assert.NotEqual(frederick.BasePrice, baltimore.BasePrice);
    }

    [Fact]
    public void OfferFloorPlan_rejects_plan_already_offered()
    {
        var community = TestData.Community();
        var plan = TestData.FloorPlan();
        community.OfferFloorPlan(plan, 449_900m);

        Assert.Throws<InvalidOperationException>(() => community.OfferFloorPlan(plan, 459_900m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OfferFloorPlan_rejects_non_positive_price(decimal basePrice) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.Community().OfferFloorPlan(TestData.FloorPlan(), basePrice));
}
