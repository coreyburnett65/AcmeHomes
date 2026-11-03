namespace AcmeHomes.Domain.Tests;

public class ValidateTests
{
    [Theory]
    [InlineData("frederick")]
    [InlineData("willow-creek")]
    [InlineData("the-ashford-2")]
    public void Slug_accepts_lowercase_hyphenated_values(string slug) =>
        Assert.Equal(slug, Validate.Slug(slug));

    [Theory]
    [InlineData("Willow-Creek")]
    [InlineData("willow creek")]
    [InlineData("willow--creek")]
    [InlineData("-willow")]
    [InlineData("willow-")]
    [InlineData("willow_creek")]
    public void Slug_rejects_invalid_formats(string slug) =>
        Assert.Throws<ArgumentException>(() => Validate.Slug(slug));

    [Theory]
    [InlineData("md")]
    [InlineData("MDX")]
    [InlineData("M1")]
    public void StateCode_rejects_values_that_are_not_two_uppercase_letters(string stateCode) =>
        Assert.Throws<ArgumentException>(() => Validate.StateCode(stateCode));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredText_rejects_null_or_whitespace(string? value)
    {
        // ThrowIfNullOrWhiteSpace throws ArgumentNullException for null and ArgumentException otherwise.
        Assert.ThrowsAny<ArgumentException>(() => Validate.RequiredText(value));
    }

    [Fact]
    public void RequiredText_trims_surrounding_whitespace() =>
        Assert.Equal("Frederick", Validate.RequiredText("  Frederick "));

    [Fact]
    public void OptionalText_normalizes_whitespace_to_null() =>
        Assert.Null(Validate.OptionalText("   "));
}
