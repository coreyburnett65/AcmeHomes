namespace AcmeHomes.Domain;

/// <summary>A neighborhood within a market where the builder offers floor plans and builds homes.</summary>
public sealed class Community
{
    private readonly List<CommunityFloorPlan> _communityFloorPlans = [];

    public Community(
        Market market,
        string name,
        string slug,
        string city,
        string stateCode,
        GeoLocation location,
        string schoolDistrict,
        HoaDetails? hoa = null,
        IEnumerable<string>? amenities = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(market);
        ArgumentNullException.ThrowIfNull(location);

        Market = market;
        Name = Validate.RequiredText(name);
        Slug = Validate.Slug(slug);
        City = Validate.RequiredText(city);
        StateCode = Validate.StateCode(stateCode);
        Location = location;
        SchoolDistrict = Validate.RequiredText(schoolDistrict);
        Hoa = hoa;
        Amenities = (amenities ?? [])
            .Select(a => Validate.RequiredText(a, nameof(amenities)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Description = Validate.OptionalText(description);
    }

    private Community()
    {
    }

    public int Id { get; private set; }

    public int MarketId { get; private set; }

    public Market Market { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string City { get; private set; } = null!;

    // Stored per community rather than inherited from the market because a metro area can cross state lines.
    public string StateCode { get; private set; } = null!;

    public GeoLocation Location { get; private set; } = null!;

    public string SchoolDistrict { get; private set; } = null!;

    /// <summary>Null when the community has no homeowners association.</summary>
    public HoaDetails? Hoa { get; private set; }

    public List<string> Amenities { get; private set; } = [];

    public string? Description { get; private set; }

    /// <summary>The floor plans offered in this community, each with its community-specific price.</summary>
    public IReadOnlyCollection<CommunityFloorPlan> CommunityFloorPlans => _communityFloorPlans;

    /// <summary>
    /// Offers a floor plan in this community at the given base price. The check for duplicates only sees
    /// offerings already loaded into memory; the database's composite primary key is the final guard.
    /// </summary>
    public CommunityFloorPlan OfferFloorPlan(FloorPlan floorPlan, decimal basePrice)
    {
        ArgumentNullException.ThrowIfNull(floorPlan);

        if (_communityFloorPlans.Any(o => o.FloorPlan == floorPlan))
        {
            throw new InvalidOperationException($"Floor plan '{floorPlan.Name}' is already offered in community '{Name}'.");
        }

        var offering = new CommunityFloorPlan(this, floorPlan, basePrice);
        _communityFloorPlans.Add(offering);
        return offering;
    }
}
