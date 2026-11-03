namespace AcmeHomes.Domain;

/// <summary>Homeowners association details for a community.</summary>
public sealed record HoaDetails
{
    public HoaDetails(decimal monthlyFee, string? description = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyFee);

        MonthlyFee = monthlyFee;
        Description = Validate.OptionalText(description);
    }

    public decimal MonthlyFee { get; }

    /// <summary>What the fee covers, for example "Lawn care, snow removal, clubhouse".</summary>
    public string? Description { get; }
}
