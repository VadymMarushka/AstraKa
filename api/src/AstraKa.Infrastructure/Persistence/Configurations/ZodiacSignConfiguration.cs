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

        // The 12 signs, Id = SequenceNumber, Aries = 1 clockwise to Pisces = 12.
        builder.HasData(
            new ZodiacSign(1, "Aries", 1),
            new ZodiacSign(2, "Taurus", 2),
            new ZodiacSign(3, "Gemini", 3),
            new ZodiacSign(4, "Cancer", 4),
            new ZodiacSign(5, "Leo", 5),
            new ZodiacSign(6, "Virgo", 6),
            new ZodiacSign(7, "Libra", 7),
            new ZodiacSign(8, "Scorpio", 8),
            new ZodiacSign(9, "Sagittarius", 9),
            new ZodiacSign(10, "Capricorn", 10),
            new ZodiacSign(11, "Aquarius", 11),
            new ZodiacSign(12, "Pisces", 12));
    }
}
