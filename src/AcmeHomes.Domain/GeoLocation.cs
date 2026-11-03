namespace AcmeHomes.Domain;

/// <summary>
/// A latitude/longitude pair. Stored as plain columns rather than a SQL geography type because
/// distance queries are handled by Azure AI Search, not the database.
/// </summary>
public sealed record GeoLocation
{
    public GeoLocation(double latitude, double longitude)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }
}
