namespace AcmeHomes.Domain.Tests;

public class FloorPlanTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2.5)]
    [InlineData(4)]
    public void Accepts_whole_and_half_bathrooms(decimal bathrooms) =>
        Assert.Equal(bathrooms, CreatePlan(bathrooms: bathrooms).Bathrooms);

    [Theory]
    [InlineData(0.5)]
    [InlineData(2.25)]
    public void Rejects_invalid_bathroom_counts(decimal bathrooms) =>
        Assert.ThrowsAny<ArgumentException>(() => CreatePlan(bathrooms: bathrooms));

    [Fact]
    public void Single_story_plan_must_have_first_floor_primary_suite() =>
        Assert.Throws<ArgumentException>(() => CreatePlan(stories: 1, primarySuiteLocation: PrimarySuiteLocation.SecondFloor));

    [Fact]
    public void Two_story_plan_may_have_second_floor_primary_suite() =>
        Assert.Equal(PrimarySuiteLocation.SecondFloor, CreatePlan(stories: 2, primarySuiteLocation: PrimarySuiteLocation.SecondFloor).PrimarySuiteLocation);

    [Fact]
    public void Rejects_undefined_primary_suite_location() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePlan(primarySuiteLocation: (PrimarySuiteLocation)99));

    [Fact]
    public void AddFeature_ignores_duplicates()
    {
        var plan = TestData.FloorPlan();
        var basement = new Feature("Finished basement", "finished-basement");

        plan.AddFeature(basement);
        plan.AddFeature(basement);

        Assert.Single(plan.Features);
    }

    private static FloorPlan CreatePlan(
        decimal bathrooms = 2.5m,
        int stories = 2,
        PrimarySuiteLocation primarySuiteLocation = PrimarySuiteLocation.FirstFloor) =>
        new("Ashford", "ashford", 4, bathrooms, 2400, stories, primarySuiteLocation);
}
