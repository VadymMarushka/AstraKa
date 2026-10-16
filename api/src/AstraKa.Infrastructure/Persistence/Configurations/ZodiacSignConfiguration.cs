using AstraKa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class ZodiacSignConfiguration : IEntityTypeConfiguration<ZodiacSign>
{
    public void Configure(EntityTypeBuilder<ZodiacSign> builder)
    {
        builder.HasKey(z => z.Id);

        builder.Property(z => z.Name)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(z => z.Name).IsUnique();

        builder.Property(z => z.SequenceNumber)
            .IsRequired();
        builder.HasIndex(z => z.SequenceNumber).IsUnique();

        // AzimuthDegrees is derived in code, never persisted.
        builder.Ignore(z => z.AzimuthDegrees);
    }
}
