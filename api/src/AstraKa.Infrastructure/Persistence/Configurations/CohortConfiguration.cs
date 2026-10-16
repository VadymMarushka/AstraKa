using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class CohortConfiguration : IEntityTypeConfiguration<Cohort>
{
    public void Configure(EntityTypeBuilder<Cohort> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SeatLimit)
            .IsRequired();

        builder.Property(c => c.SeatsTaken)
            .IsRequired();

        // Concurrency token: two simultaneous enrollments for the last seat hit the same row, and
        // this catches the conflict. Maps to SQL Server's `rowversion` type.
        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        builder.ToTable(t => t.HasCheckConstraint("CK_Cohort_SeatLimit", "[SeatLimit] > 0"));

        // Course -> Cohort: Restrict (paid enrollments are never silently lost).
        builder.HasOne(c => c.Course)
            .WithMany(c => c.Cohorts)
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
