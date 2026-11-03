namespace AcmeHomes.Domain.Tests;

public class ValueObjectTests
{
    [Theory]
    [InlineData(90.1, 0)]
    [InlineData(-90.1, 0)]
    [InlineData(0, 180.1)]
    [InlineData(0, -180.1)]
    public void GeoLocation_rejects_out_of_range_coordinates(double latitude, double longitude) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(latitude, longitude));

    [Fact]
    public void GeoLocations_with_same_coordinates_are_equal() =>
        Assert.Equal(new GeoLocation(39.41, -77.41), new GeoLocation(39.41, -77.41));

    [Fact]
    public void HoaDetails_rejects_negative_fee() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new HoaDetails(-1m));

    [Fact]
    public void HoaDetails_allows_zero_fee() =>
        Assert.Equal(0m, new HoaDetails(0m).MonthlyFee);
}
