using AstraKa.Domain.Entities;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.Promise)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Price)
            .HasPrecision(10, 2);

        // Stored as a tinyint FK to CourseStatuses, not a free-text string - see CourseStatusLookup.
        builder.Property(c => c.Status)
            .HasConversion<byte>()
            .HasColumnName("StatusId")
            .IsRequired();

        builder.HasOne<CourseStatusLookup>()
            .WithMany()
            .HasForeignKey(c => c.Status)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // The AuthorId FK itself is configured from the Starshepherd side (StarshepherdConfiguration).
    }
}
