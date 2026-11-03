namespace AcmeHomes.Domain;

/// <summary>
/// A home design that can be built in one or more communities. Pricing lives on <see cref="CommunityFloorPlan"/>
/// because the same plan is priced differently in each community (see ADR 0004).
/// </summary>
public sealed class FloorPlan
{
    private readonly List<CommunityFloorPlan> _communityFloorPlans = [];
    private readonly List<Feature> _features = [];

    public FloorPlan(
        string name,
        string slug,
        int bedrooms,
        decimal bathrooms,
        int squareFeet,
        int stories,
        PrimarySuiteLocation primarySuiteLocation,
        string? description = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bedrooms);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(squareFeet);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stories);
        ArgumentOutOfRangeException.ThrowIfLessThan(bathrooms, 1m);

        // Half baths are counted as 0.5, so 2.5 means two full baths and one half bath.
        if (bathrooms % 0.5m != 0)
        {
            throw new ArgumentException("Bathrooms must be a whole or half number, such as 2 or 2.5.", nameof(bathrooms));
        }

        if (!Enum.IsDefined(primarySuiteLocation))
        {
            throw new ArgumentOutOfRangeException(nameof(primarySuiteLocation));
        }

        if (stories == 1 && primarySuiteLocation != PrimarySuiteLocation.FirstFloor)
        {
            throw new ArgumentException("A single-story plan must have its primary suite on the first floor.", nameof(primarySuiteLocation));
        }

        Name = Validate.RequiredText(name);
        Slug = Validate.Slug(slug);
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        SquareFeet = squareFeet;
        Stories = stories;
        PrimarySuiteLocation = primarySuiteLocation;
        Description = Validate.OptionalText(description);
    }

    private FloorPlan()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public int Bedrooms { get; private set; }

    public decimal Bathrooms { get; private set; }

    public int SquareFeet { get; private set; }

    public int Stories { get; private set; }

    public PrimarySuiteLocation PrimarySuiteLocation { get; private set; }

    public string? Description { get; private set; }

    /// <summary>The communities this plan is offered in, each with its community-specific price.</summary>
    public IReadOnlyCollection<CommunityFloorPlan> CommunityFloorPlans => _communityFloorPlans;

    public IReadOnlyCollection<Feature> Features => _features;

    /// <summary>Adds a feature tag. Adding a feature the plan already has does nothing.</summary>
    public void AddFeature(Feature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (!_features.Contains(feature))
        {
            _features.Add(feature);
        }
    }
}
