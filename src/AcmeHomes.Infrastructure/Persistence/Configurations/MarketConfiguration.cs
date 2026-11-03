using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeHomes.Infrastructure.Persistence.Configurations;

internal sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.Property(m => m.Name).HasMaxLength(100);
        builder.Property(m => m.Slug).HasMaxLength(100).IsUnicode(false);
        builder.Property(m => m.StateCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);

        builder.HasIndex(m => m.Slug).IsUnique();
    }
}
