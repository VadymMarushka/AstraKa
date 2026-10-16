using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
{
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Date)
            .IsRequired();

        // At most one report per day per enrollment.
        builder.HasIndex(r => new { r.WandererId, r.CohortId, r.Date }).IsUnique();

        builder.Property(r => r.HarmonyLevel)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_DailyReport_HarmonyLevel", "[HarmonyLevel] BETWEEN 1 AND 10"));

        // Enrollment -> DailyReport -> ExerciseCompletion: Cascade.
        builder.HasOne(r => r.Enrollment)
            .WithMany(e => e.DailyReports)
            .HasForeignKey(r => new { r.WandererId, r.CohortId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
