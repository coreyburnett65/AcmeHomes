namespace AcmeHomes.Domain;

/// <summary>A metro area the builder operates in, such as Frederick or Baltimore.</summary>
public sealed class Market
{
    public Market(string name, string slug, string stateCode)
    {
        Name = Validate.RequiredText(name);
        Slug = Validate.Slug(slug);
        StateCode = Validate.StateCode(stateCode);
    }

    // EF Core materializes entities through this constructor and then sets properties directly.
    // The null-forgiving initializers on non-nullable properties are safe because EF always populates them.
    private Market()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string StateCode { get; private set; } = null!;
}
