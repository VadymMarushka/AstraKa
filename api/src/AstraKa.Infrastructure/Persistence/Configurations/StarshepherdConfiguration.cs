using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class StarshepherdConfiguration : IEntityTypeConfiguration<Starshepherd>
{
    public void Configure(EntityTypeBuilder<Starshepherd> builder)
    {
        // PK + FK to User; the User side of the relationship is configured in UserConfiguration.
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.Title)
            .HasMaxLength(100)
            .IsRequired();

        // Courses/Exercises authored by a Starshepherd are never mentioned in the doc's delete list -
        // Restrict, so a specialist's work can't vanish silently.
        builder.HasMany(s => s.Courses)
            .WithOne(c => c.Author)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Exercises)
            .WithOne(e => e.Author)
            .HasForeignKey(e => e.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
