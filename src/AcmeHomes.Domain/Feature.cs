namespace AcmeHomes.Domain;

/// <summary>A searchable tag attached to floor plans or homes, such as "Finished basement" or "Three-car garage".</summary>
public sealed class Feature
{
    public Feature(string name, string slug)
    {
        Name = Validate.RequiredText(name);
        Slug = Validate.Slug(slug);
    }

    private Feature()
    {
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;
}
