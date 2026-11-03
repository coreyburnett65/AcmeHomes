namespace AcmeHomes.Domain;

/// <summary>Where a home is in its sales and construction lifecycle.</summary>
public enum HomeStatus
{
    Available = 1,
    UnderConstruction = 2,
    Sold = 3,
}
