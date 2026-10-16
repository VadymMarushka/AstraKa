using AstraKa.Domain.Entities;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        // Explicit M:N join with a composite key ("Joins" / "In") - no surrogate Id.
        builder.HasKey(e => new { e.WandererId, e.CohortId });

        builder.Property(e => e.EnrolledAt)
            .IsRequired();

        // Stored as a tinyint FK to EnrollmentStatuses, not a free-text string - see EnrollmentStatusLookup.
        builder.Property(e => e.Status)
            .HasConversion<byte>()
            .HasColumnName("StatusId")
            .IsRequired();

        builder.HasOne<EnrollmentStatusLookup>()
            .WithMany()
            .HasForeignKey(e => e.Status)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.PaidPrice)
            .HasPrecision(10, 2);

        // Wanderer -> Enrollment: Restrict, not in the doc's cascade list - a paid enrollment should
        // never vanish just because the profile row is touched.
        builder.HasOne(e => e.Wanderer)
            .WithMany(w => w.Enrollments)
            .HasForeignKey(e => e.WandererId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cohort -> Enrollment: Restrict (paid enrollments are never silently lost).
        builder.HasOne(e => e.Cohort)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CohortId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
