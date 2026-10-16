using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class ExerciseCompletionConfiguration : IEntityTypeConfiguration<ExerciseCompletion>
{
    public void Configure(EntityTypeBuilder<ExerciseCompletion> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompletedAt)
            .IsRequired();

        builder.HasIndex(c => new { c.DailyReportId, c.DayExerciseId }).IsUnique();

        // Enrollment -> DailyReport -> ExerciseCompletion: Cascade.
        builder.HasOne(c => c.DailyReport)
            .WithMany(r => r.Completions)
            .HasForeignKey(c => c.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // DayExercise -> ExerciseCompletion: Restrict, so SQL Server never sees a second cascade
        // path into ExerciseCompletions alongside the DailyReport one above.
        builder.HasOne(c => c.DayExercise)
            .WithMany()
            .HasForeignKey(c => c.DayExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
