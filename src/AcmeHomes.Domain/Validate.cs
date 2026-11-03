using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AcmeHomes.Domain;

/// <summary>
/// Argument checks shared by the domain types. The BCL throw helpers (ArgumentOutOfRangeException.ThrowIf...)
/// cover numeric rules; this class covers the text formats specific to this domain.
/// </summary>
internal static partial class Validate
{
    public static string RequiredText(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value.Trim();
    }

    public static string? OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>A URL-safe identifier such as "willow-creek". Used for front-end routes instead of numeric IDs.</summary>
    public static string Slug(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        if (!SlugPattern().IsMatch(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a valid slug. Use lowercase letters and digits separated by single hyphens.", paramName);
        }

        return value;
    }

    /// <summary>A two-letter US state code such as "MD".</summary>
    public static string StateCode(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        if (!StateCodePattern().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a valid two-letter uppercase state code.", paramName);
        }

        return value;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex StateCodePattern();
}
