using AstraKa.Domain.Enums;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class CourseStatusLookupConfiguration : IEntityTypeConfiguration<CourseStatusLookup>
{
    public void Configure(EntityTypeBuilder<CourseStatusLookup> builder)
    {
        builder.ToTable("CourseStatuses");

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
            new CourseStatusLookup { Id = CourseStatus.Draft, Name = nameof(CourseStatus.Draft) },
            new CourseStatusLookup { Id = CourseStatus.Published, Name = nameof(CourseStatus.Published) });
    }
}
