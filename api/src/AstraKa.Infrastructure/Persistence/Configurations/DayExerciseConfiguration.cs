using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class DayExerciseConfiguration : IEntityTypeConfiguration<DayExercise>
{
    public void Configure(EntityTypeBuilder<DayExercise> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.OrderNumber)
            .IsRequired();

        builder.Property(d => d.Sets)
            .IsRequired();

        builder.Property(d => d.Amount)
            .IsRequired();

        builder.Ignore(d => d.IsBaseProgram);

        // Base-program order is unique per day; a Revelation's extra exercises are exempt.
        builder.HasIndex(d => new { d.CourseDayId, d.OrderNumber })
            .IsUnique()
            .HasFilter("[RevelationId] IS NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_DayExercise_Sets", "[Sets] > 0");
            t.HasCheckConstraint("CK_DayExercise_Amount", "[Amount] > 0");
        });

        // Course -> CourseDay -> DayExercise: Cascade (a program is owned by its course).
        builder.HasOne(d => d.CourseDay)
            .WithMany(cd => cd.DayExercises)
            .HasForeignKey(d => d.CourseDayId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exercise -> DayExercise: Restrict (cannot delete an exercise used in a program).
        builder.HasOne(d => d.Exercise)
            .WithMany(e => e.DayExercises)
            .HasForeignKey(d => d.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Revelation -> DayExercise: Restrict, the other half of the Enrollment/Revelation cascade
        // choice - Enrollment -> Revelation already cascades, so this side must not, or SQL Server
        // would see two cascade paths into DayExercises.
        builder.HasOne(d => d.Revelation)
            .WithMany(r => r.ExtraExercises)
            .HasForeignKey(d => d.RevelationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
