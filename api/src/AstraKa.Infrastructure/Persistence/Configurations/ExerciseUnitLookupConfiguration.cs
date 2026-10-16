using AstraKa.Domain.Enums;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class ExerciseUnitLookupConfiguration : IEntityTypeConfiguration<ExerciseUnitLookup>
{
    public void Configure(EntityTypeBuilder<ExerciseUnitLookup> builder)
    {
        builder.ToTable("ExerciseUnits");

        builder.HasKey(l => l.Id);

        // Fixed, enum-driven ids - not DB-generated.
        builder.Property(l => l.Id)
            .HasConversion<byte>()
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(l => l.Name).IsUnique();

        builder.HasData(
            new ExerciseUnitLookup { Id = ExerciseUnit.Reps, Name = nameof(ExerciseUnit.Reps) },
            new ExerciseUnitLookup { Id = ExerciseUnit.Seconds, Name = nameof(ExerciseUnit.Seconds) },
            new ExerciseUnitLookup { Id = ExerciseUnit.Breaths, Name = nameof(ExerciseUnit.Breaths) });
    }
}
