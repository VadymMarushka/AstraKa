using AstraKa.Domain.Enums;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class EnrollmentStatusLookupConfiguration : IEntityTypeConfiguration<EnrollmentStatusLookup>
{
    public void Configure(EntityTypeBuilder<EnrollmentStatusLookup> builder)
    {
        builder.ToTable("EnrollmentStatuses");

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
            new EnrollmentStatusLookup { Id = EnrollmentStatus.Active, Name = nameof(EnrollmentStatus.Active) },
            new EnrollmentStatusLookup { Id = EnrollmentStatus.Left, Name = nameof(EnrollmentStatus.Left) });
    }
}
