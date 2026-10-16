using AstraKa.Domain.Entities;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(2000)
            .IsRequired();

        // Stored as a tinyint FK to ExerciseUnits, not a free-text string - see ExerciseUnitLookup.
        builder.Property(e => e.Unit)
            .HasConversion<byte>()
            .HasColumnName("UnitId")
            .IsRequired();

        builder.HasOne<ExerciseUnitLookup>()
            .WithMany()
            .HasForeignKey(e => e.Unit)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(500);

        // The AuthorId FK itself is configured from the Starshepherd side (StarshepherdConfiguration).
        builder.Ignore(e => e.IsSystemExercise);
    }
}
