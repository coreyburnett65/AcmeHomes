using System.Globalization;

using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Infrastructure.Persistence.Seeding;

/// <summary>
/// Fictional demo catalog. Markets, communities, plans, and features are hand-written so they read realistically;
/// homes are generated from a fixed random seed so every environment gets the same inventory.
/// Runs through EF Core's UseAsyncSeeding hook after migrations are applied, and only seeds an empty database.
/// </summary>
public static class SeedData
{
    private const int RandomSeed = 20261102;
    private const int HomesPerCommunity = 6;

    /// <summary>Registers the seeder so it runs whenever <c>Database.MigrateAsync</c> is called.</summary>
    public static DbContextOptionsBuilder UseAcmeHomesSeedData(this DbContextOptionsBuilder builder, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var clock = timeProvider ?? TimeProvider.System;
        return builder.UseAsyncSeeding((context, _, cancellationToken) => SeedAsync(context, clock, cancellationToken));
    }

    public static async Task SeedAsync(DbContext context, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (await context.Set<Market>().AnyAsync(cancellationToken))
        {
            return;
        }

        var features = Features.ToDictionary(f => f.Slug, f => new Feature(f.Name, f.Slug));
        var plans = Plans.ToDictionary(p => p.Slug, p => CreatePlan(p, features));

        var random = new Random(RandomSeed);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        foreach (var marketSeed in Markets)
        {
            var market = new Market(marketSeed.Name, marketSeed.Slug, marketSeed.StateCode);
            context.Add(market);

            foreach (var communitySeed in marketSeed.Communities)
            {
                var community = CreateCommunity(market, communitySeed, plans);
                context.Add(community);
                context.AddRange(CreateHomes(community, features, random, today));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static FloorPlan CreatePlan(PlanSeed seed, Dictionary<string, Feature> features)
    {
        var plan = new FloorPlan(
            seed.Name, seed.Slug, seed.Bedrooms, seed.Bathrooms, seed.SquareFeet, seed.Stories, seed.PrimarySuite, seed.Description);

        foreach (var slug in seed.FeatureSlugs)
        {
            plan.AddFeature(features[slug]);
        }

        return plan;
    }

    private static Community CreateCommunity(Market market, CommunitySeed seed, Dictionary<string, FloorPlan> plans)
    {
        var community = new Community(
            market,
            seed.Name,
            seed.Slug,
            seed.City,
            "MD",
            new GeoLocation(seed.Latitude, seed.Longitude),
            seed.SchoolDistrict,
            seed.HoaMonthlyFee is { } fee ? new HoaDetails(fee, seed.HoaDescription) : null,
            seed.Amenities,
            seed.Description);

        foreach (var slug in seed.PlanSlugs)
        {
            community.OfferFloorPlan(plans[slug], CommunityPrice(Plans.Single(p => p.Slug == slug).ReferencePrice, seed.PriceFactor));
        }

        return community;
    }

    /// <summary>
    /// Scales the plan's reference price by the community's land and market factor, rounded to the
    /// "$xxx,900" style builders use.
    /// </summary>
    private static decimal CommunityPrice(decimal referencePrice, decimal priceFactor) =>
        (Math.Round(referencePrice * priceFactor / 1000m) * 1000m) - 100m;

    private static IEnumerable<Home> CreateHomes(Community community, Dictionary<string, Feature> features, Random random, DateOnly today)
    {
        var offerings = community.CommunityFloorPlans.ToList();
        var lotNumber = random.Next(10, 40);

        for (var i = 0; i < HomesPerCommunity; i++)
        {
            var offering = offerings[random.Next(offerings.Count)];
            lotNumber += random.Next(1, 4);

            // Roughly 35% move-in ready, 45% under construction, 20% sold.
            var roll = random.Next(100);
            var status = roll < 35 ? HomeStatus.Available : roll < 80 ? HomeStatus.UnderConstruction : HomeStatus.Sold;
            DateOnly? moveInDate = status == HomeStatus.UnderConstruction ? today.AddDays(random.Next(30, 241)) : null;

            // Base price plus a lot premium and selected options, in builder-style increments.
            var price = offering.BasePrice + (random.Next(0, 11) * 2_500m) + (random.Next(0, 9) * 5_000m);

            var home = new Home(offering, lotNumber.ToString(CultureInfo.InvariantCulture), status, price, moveInDate);

            if (offering.FloorPlan.Stories > 1 && random.Next(100) < 30)
            {
                home.AddFeature(features["finished-basement"]);
            }

            if (random.Next(100) < 20)
            {
                home.AddFeature(features["smart-home-package"]);
            }

            yield return home;
        }
    }

    private sealed record FeatureSeed(string Name, string Slug);

    private sealed record PlanSeed(
        string Name,
        string Slug,
        int Bedrooms,
        decimal Bathrooms,
        int SquareFeet,
        int Stories,
        PrimarySuiteLocation PrimarySuite,
        decimal ReferencePrice,
        string[] FeatureSlugs,
        string Description);

    private sealed record CommunitySeed(
        string Name,
        string Slug,
        string City,
        double Latitude,
        double Longitude,
        string SchoolDistrict,
        decimal? HoaMonthlyFee,
        string? HoaDescription,
        string[] Amenities,
        string[] PlanSlugs,
        decimal PriceFactor,
        string Description);

    private sealed record MarketSeed(string Name, string Slug, string StateCode, CommunitySeed[] Communities);

    private static readonly FeatureSeed[] Features =
    [
        new("Covered patio", "covered-patio"),
        new("Walk-in pantry", "walk-in-pantry"),
        new("Loft", "loft"),
        new("Study", "study"),
        new("Morning room", "morning-room"),
        new("Gourmet kitchen", "gourmet-kitchen"),
        new("Three-car garage", "three-car-garage"),
        new("Sunroom", "sunroom"),
        new("Finished basement", "finished-basement"),
        new("Main-level guest suite", "main-level-guest-suite"),
        new("Smart home package", "smart-home-package"),
    ];

    // Reference prices are for a typical community; each community scales them by its PriceFactor (see ADR 0004).
    private static readonly PlanSeed[] Plans =
    [
        new("Granville", "granville", 2, 2m, 1450, 1, PrimarySuiteLocation.FirstFloor, 349_900m,
            ["walk-in-pantry", "covered-patio"],
            "A low-maintenance single-level plan with an open great room and a private primary suite."),
        new("Ashford", "ashford", 3, 2m, 1650, 1, PrimarySuiteLocation.FirstFloor, 389_900m,
            ["covered-patio", "walk-in-pantry"],
            "A single-story ranch with a split-bedroom layout and a covered patio off the kitchen."),
        new("Brookline", "brookline", 3, 2.5m, 1900, 2, PrimarySuiteLocation.SecondFloor, 414_900m,
            ["loft"],
            "A two-story starter plan with an upstairs loft and an open kitchen and family room."),
        new("Carver", "carver", 4, 2.5m, 2350, 2, PrimarySuiteLocation.FirstFloor, 449_900m,
            ["study", "morning-room"],
            "A first-floor primary suite with three bedrooms upstairs, a study, and a sunny morning room."),
        new("Dalton", "dalton", 4, 3m, 2600, 2, PrimarySuiteLocation.SecondFloor, 479_900m,
            ["gourmet-kitchen", "loft", "main-level-guest-suite"],
            "A main-level guest suite, a gourmet kitchen, and a flexible upstairs loft."),
        new("Ellison", "ellison", 4, 3.5m, 2900, 2, PrimarySuiteLocation.FirstFloor, 529_900m,
            ["three-car-garage", "gourmet-kitchen", "study"],
            "A spacious first-floor primary suite, a three-car garage, and a private study."),
        new("Fairmont", "fairmont", 5, 3.5m, 3300, 2, PrimarySuiteLocation.SecondFloor, 574_900m,
            ["three-car-garage", "sunroom", "main-level-guest-suite"],
            "Five bedrooms, a sunroom, and a main-level guest suite for visiting family."),
        new("Hartwell", "hartwell", 5, 4.5m, 3800, 3, PrimarySuiteLocation.SecondFloor, 649_900m,
            ["three-car-garage", "gourmet-kitchen", "study", "finished-basement"],
            "The largest plan, with a finished lower level, a gourmet kitchen, and a three-car garage."),
    ];

    // All names are fictional. Cities and school districts are real places so locations and searches feel realistic.
    private static readonly MarketSeed[] Markets =
    [
        new("Frederick", "frederick", "MD",
        [
            new("Juniper Hollow", "juniper-hollow", "Frederick", 39.4390, -77.4400,
                "Frederick County Public Schools", 95m, "Common area maintenance and trail upkeep",
                ["Walking trails", "Playground", "Dog park"],
                ["ashford", "brookline", "carver", "dalton"], 1.00m,
                "Tree-lined streets on the north side of Frederick, minutes from downtown and Route 15."),
            new("Stonebrook Meadows", "stonebrook-meadows", "Urbana", 39.3260, -77.3510,
                "Frederick County Public Schools", 140m, "Clubhouse, pool, and common area maintenance",
                ["Clubhouse", "Pool", "Fitness center", "Walking trails"],
                ["carver", "dalton", "ellison", "fairmont", "hartwell"], 1.15m,
                "Larger homesites near the I-270 corridor with a resort-style clubhouse and pool."),
            new("Linden Fields", "linden-fields", "Walkersville", 39.4860, -77.3520,
                "Frederick County Public Schools", null, null,
                ["Community garden", "Playground"],
                ["granville", "ashford", "brookline", "carver"], 0.92m,
                "A small-town setting with no HOA and some of the most affordable new homes in the county."),
        ]),
        new("Baltimore", "baltimore", "MD",
        [
            new("Bramblewood", "bramblewood", "Ellicott City", 39.2670, -76.7980,
                "Howard County Public School System", 120m, "Pool, tennis courts, and common areas",
                ["Pool", "Tennis courts", "Walking trails"],
                ["carver", "dalton", "ellison", "fairmont"], 1.30m,
                "Wooded homesites in Howard County with easy access to Route 29 and I-70."),
            new("Cedar Hollow", "cedar-hollow", "Bel Air", 39.5360, -76.3480,
                "Harford County Public Schools", 85m, "Common area maintenance",
                ["Playground", "Pavilion", "Walking trails"],
                ["granville", "ashford", "brookline", "carver", "dalton"], 1.05m,
                "A family-friendly neighborhood near downtown Bel Air with a central park and pavilion."),
            new("Harrow Glen", "harrow-glen", "Towson", 39.4015, -76.6019,
                "Baltimore County Public Schools", 175m, "Lawn care, snow removal, and clubhouse",
                ["Clubhouse", "Fitness center", "Pool"],
                ["granville", "ashford", "ellison", "hartwell"], 1.25m,
                "Low-maintenance living close to Towson's shops and restaurants, with lawn care and snow removal included."),
        ]),
    ];
}
