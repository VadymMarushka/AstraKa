using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class RevelationConfiguration : IEntityTypeConfiguration<Revelation>
{
    public void Configure(EntityTypeBuilder<Revelation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Enrollment -> Revelation: Cascade (chosen cascade path of the doc's "pick one" pair;
        // Revelation -> DayExercise stays Restrict, see DayExerciseConfiguration).
        builder.HasOne(r => r.Enrollment)
            .WithMany(e => e.Revelations)
            .HasForeignKey(r => new { r.WandererId, r.CohortId })
            .OnDelete(DeleteBehavior.Cascade);

        // CourseDay -> Revelation ("Targets"): Restrict, not in the doc's cascade list - a day that
        // still has a Revelation aimed at it should not disappear silently.
        builder.HasOne(r => r.CourseDay)
            .WithMany()
            .HasForeignKey(r => r.CourseDayId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
