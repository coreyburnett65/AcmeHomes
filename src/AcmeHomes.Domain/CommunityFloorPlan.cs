namespace AcmeHomes.Domain;

/// <summary>
/// A floor plan offered in a specific community, with that community's base price (see ADR 0004).
/// Search treats each offering as a buildable result, as opposed to a <see cref="Home"/>, which is actual inventory.
/// Created through <see cref="Community.OfferFloorPlan"/>.
/// </summary>
public sealed class CommunityFloorPlan
{
    internal CommunityFloorPlan(Community community, FloorPlan floorPlan, decimal basePrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(basePrice);

        Community = community;
        FloorPlan = floorPlan;
        BasePrice = basePrice;
    }

    private CommunityFloorPlan()
    {
    }

    public int CommunityId { get; private set; }

    public Community Community { get; private set; } = null!;

    public int FloorPlanId { get; private set; }

    public FloorPlan FloorPlan { get; private set; } = null!;

    /// <summary>The starting price for building this plan in this community, before lot premiums and options.</summary>
    public decimal BasePrice { get; private set; }

    public void ChangeBasePrice(decimal basePrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(basePrice);
        BasePrice = basePrice;
    }
}
