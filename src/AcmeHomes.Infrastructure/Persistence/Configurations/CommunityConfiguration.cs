using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeHomes.Infrastructure.Persistence.Configurations;

internal sealed class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100);
        builder.Property(c => c.Slug).HasMaxLength(100).IsUnicode(false);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.StateCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        builder.Property(c => c.SchoolDistrict).HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(2000);

        // Slugs are unique across all communities because front-end routes are /communities/{slug}.
        builder.HasIndex(c => c.Slug).IsUnique();

        // Value objects are complex types: their properties become columns on the Communities table
        // (Latitude, Longitude, HoaMonthlyFee, HoaDescription) rather than separate tables with their own keys.
        builder.ComplexProperty(c => c.Location, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("Latitude");
            location.Property(l => l.Longitude).HasColumnName("Longitude");
        });

        builder.ComplexProperty(c => c.Hoa, hoa =>
        {
            hoa.Property(h => h.MonthlyFee).HasColumnName("HoaMonthlyFee").HasPrecision(8, 2);
            hoa.Property(h => h.Description).HasColumnName("HoaDescription").HasMaxLength(500);
        });

        // A primitive collection, stored as a JSON array in a single nvarchar(max) column. Amenities are only
        // displayed and indexed for search, never queried relationally, so a separate table would add joins for nothing.
        builder.PrimitiveCollection(c => c.Amenities);

        builder.HasOne(c => c.Market)
            .WithMany()
            .HasForeignKey(c => c.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a community removes its plan offerings. (Homes still block the delete; see HomeConfiguration.)
        builder.HasMany(c => c.CommunityFloorPlans)
            .WithOne(o => o.Community)
            .HasForeignKey(o => o.CommunityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
