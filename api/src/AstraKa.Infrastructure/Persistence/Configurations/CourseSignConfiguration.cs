using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class CourseSignConfiguration : IEntityTypeConfiguration<CourseSign>
{
    public void Configure(EntityTypeBuilder<CourseSign> builder)
    {
        // Explicit M:N join with a composite key ("Open to").
        builder.HasKey(cs => new { cs.CourseId, cs.ZodiacSignId });

        builder.HasOne(cs => cs.Course)
            .WithMany(c => c.CourseSigns)
            .HasForeignKey(cs => cs.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // ZodiacSign is seeded reference data, never deleted through the app - Restrict.
        builder.HasOne(cs => cs.ZodiacSign)
            .WithMany()
            .HasForeignKey(cs => cs.ZodiacSignId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
