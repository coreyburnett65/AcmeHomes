namespace AcmeHomes.Domain.Tests;

/// <summary>Valid default objects so each test only spells out the values it cares about.</summary>
internal static class TestData
{
    public static Market Market() => new("Frederick", "frederick", "MD");

    public static Community Community(Market? market = null) => new(
        market ?? Market(),
        "Willow Creek",
        "willow-creek",
        "Frederick",
        "MD",
        new GeoLocation(39.4143, -77.4105),
        "Frederick County Public Schools");

    public static FloorPlan FloorPlan(string name = "Ashford", string slug = "ashford") => new(
        name,
        slug,
        bedrooms: 4,
        bathrooms: 2.5m,
        squareFeet: 2400,
        stories: 2,
        PrimarySuiteLocation.FirstFloor);

    public static CommunityFloorPlan Offering() => Community().OfferFloorPlan(FloorPlan(), 449_900m);
}
