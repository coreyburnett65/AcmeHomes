using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeHomes.Infrastructure.Persistence.Configurations;

internal sealed class FloorPlanConfiguration : IEntityTypeConfiguration<FloorPlan>
{
    public void Configure(EntityTypeBuilder<FloorPlan> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.Property(p => p.Slug).HasMaxLength(100).IsUnicode(false);
        builder.Property(p => p.Bathrooms).HasPrecision(3, 1);
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.HasIndex(p => p.Slug).IsUnique();

        // A plan can't be deleted while any community still offers it.
        builder.HasMany(p => p.CommunityFloorPlans)
            .WithOne(o => o.FloorPlan)
            .HasForeignKey(o => o.FloorPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many with no extra columns, so EF manages the join table and Feature doesn't need a back-navigation.
        builder.HasMany(p => p.Features)
            .WithMany()
            .UsingEntity("FloorPlanFeatures");
    }
}
