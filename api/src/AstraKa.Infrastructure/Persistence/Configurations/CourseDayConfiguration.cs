using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class CourseDayConfiguration : IEntityTypeConfiguration<CourseDay>
{
    public void Configure(EntityTypeBuilder<CourseDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DayNumber)
            .IsRequired();
        builder.HasIndex(d => new { d.CourseId, d.DayNumber }).IsUnique();

        builder.Property(d => d.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.MeetLink)
            .HasMaxLength(500);

        // Course -> CourseDay: Cascade (a program is owned by its course).
        builder.HasOne(d => d.Course)
            .WithMany(c => c.CourseDays)
            .HasForeignKey(d => d.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
