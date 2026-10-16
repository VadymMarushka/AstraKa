using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class WandererConfiguration : IEntityTypeConfiguration<Wanderer>
{
    public void Configure(EntityTypeBuilder<Wanderer> builder)
    {
        // PK + FK to User; the User side of the relationship is configured in UserConfiguration.
        builder.HasKey(w => w.UserId);

        // ZodiacSign is seeded reference data, never deleted through the app - Restrict, not the
        // doc's cascade list, but consistent with "never silently lose business data".
        builder.HasOne(w => w.ZodiacSign)
            .WithMany()
            .HasForeignKey(w => w.ZodiacSignId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
