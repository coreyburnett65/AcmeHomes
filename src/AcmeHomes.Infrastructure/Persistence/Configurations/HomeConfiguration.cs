using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeHomes.Infrastructure.Persistence.Configurations;

internal sealed class HomeConfiguration : IEntityTypeConfiguration<Home>
{
    public void Configure(EntityTypeBuilder<Home> builder)
    {
        builder.Property(h => h.LotNumber).HasMaxLength(20);

        // A lot can only hold one home within a community.
        builder.HasIndex(h => new { h.CommunityId, h.LotNumber }).IsUnique();

        // SQL Server rowversion for optimistic concurrency. The inventory sync worker and any future admin edits
        // could update the same home; EF includes this in the UPDATE's WHERE clause so a stale write fails instead
        // of silently overwriting. It's a shadow property because the domain model has no use for it.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        // The database-level guarantee behind the Home constructor taking a CommunityFloorPlan:
        // (CommunityId, FloorPlanId) must match an existing offering.
        builder.HasOne<CommunityFloorPlan>()
            .WithMany()
            .HasForeignKey(h => new { h.CommunityId, h.FloorPlanId })
            .OnDelete(DeleteBehavior.Restrict);

        // These navigations share the same columns as the composite key above. They exist so queries can
        // write home.Community.Name instead of going through the offering.
        builder.HasOne(h => h.Community)
            .WithMany()
            .HasForeignKey(h => h.CommunityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.FloorPlan)
            .WithMany()
            .HasForeignKey(h => h.FloorPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(h => h.Features)
            .WithMany()
            .UsingEntity("HomeFeatures");
    }
}
