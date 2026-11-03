using AcmeHomes.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeHomes.Infrastructure.Persistence.Configurations;

internal sealed class CommunityFloorPlanConfiguration : IEntityTypeConfiguration<CommunityFloorPlan>
{
    public void Configure(EntityTypeBuilder<CommunityFloorPlan> builder)
    {
        // The composite key means a plan can only be offered once per community. It is also the target of
        // the Homes foreign key that ensures a home is built from a plan its community actually offers.
        builder.HasKey(o => new { o.CommunityId, o.FloorPlanId });

        // Relationships to Community and FloorPlan are configured from the principal side in their configurations.
    }
}
