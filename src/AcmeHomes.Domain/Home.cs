namespace AcmeHomes.Domain;

/// <summary>
/// An actual home in inventory: a floor plan built (or being built) on a specific lot in a community.
/// Search treats homes as move-in-ready or soon-available results, as opposed to buildable <see cref="CommunityFloorPlan"/> offerings.
/// </summary>
public sealed class Home
{
    private readonly List<Feature> _features = [];

    /// <summary>
    /// Creates a home from a community's floor plan offering. Taking the offering rather than a separate
    /// community and floor plan guarantees a home is only built from a plan that community actually offers.
    /// </summary>
    public Home(CommunityFloorPlan offering, string lotNumber, HomeStatus status, decimal price, DateOnly? estimatedMoveInDate = null)
    {
        ArgumentNullException.ThrowIfNull(offering);

        Community = offering.Community;
        FloorPlan = offering.FloorPlan;
        LotNumber = Validate.RequiredText(lotNumber);
        UpdateInventory(status, price, estimatedMoveInDate);
    }

    private Home()
    {
    }

    public int Id { get; private set; }

    public int CommunityId { get; private set; }

    public Community Community { get; private set; } = null!;

    public int FloorPlanId { get; private set; }

    public FloorPlan FloorPlan { get; private set; } = null!;

    public string LotNumber { get; private set; } = null!;

    public HomeStatus Status { get; private set; }

    public decimal Price { get; private set; }

    /// <summary>Required while under construction. Optional when available, which usually means move-in ready now.</summary>
    public DateOnly? EstimatedMoveInDate { get; private set; }

    public IReadOnlyCollection<Feature> Features => _features;

    /// <summary>
    /// Applies a status, price, and move-in date change. The inventory sync worker calls this for each
    /// update it receives, so all three values are validated together.
    /// </summary>
    public void UpdateInventory(HomeStatus status, decimal price, DateOnly? estimatedMoveInDate)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        if (status == HomeStatus.UnderConstruction && estimatedMoveInDate is null)
        {
            throw new ArgumentException("A home under construction must have an estimated move-in date.", nameof(estimatedMoveInDate));
        }

        Status = status;
        Price = price;
        EstimatedMoveInDate = estimatedMoveInDate;
    }

    /// <summary>Adds a feature tag. Adding a feature the home already has does nothing.</summary>
    public void AddFeature(Feature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (!_features.Contains(feature))
        {
            _features.Add(feature);
        }
    }
}
