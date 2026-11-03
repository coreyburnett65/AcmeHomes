using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeHomes.Infrastructure.Persistence.Configurations;

internal sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.Property(f => f.Name).HasMaxLength(100);
        builder.Property(f => f.Slug).HasMaxLength(100).IsUnicode(false);

        builder.HasIndex(f => f.Slug).IsUnique();
    }
}
