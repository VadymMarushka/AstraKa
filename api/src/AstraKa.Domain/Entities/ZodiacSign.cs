namespace AstraKa.Domain.Entities;

/// <summary>
/// Reference table of the 12 zodiac signs, seeded once and never edited through the app.
/// Gives the direction of a Wanderer's astral address as a compass azimuth.
/// </summary>
public class ZodiacSign
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>1-12, Aries = 1. Drives <see cref="AzimuthDegrees"/>.</summary>
    public int SequenceNumber { get; private set; }

    /// <summary>
    /// Compass direction "home" for this sign, 0 deg = north, clockwise. Derived, not stored:
    /// Aries = 0 deg, Taurus = 30 deg, ... Libra = 180 deg, Scorpio = 210 deg, ... Pisces = 330 deg.
    /// </summary>
    public int AzimuthDegrees => (SequenceNumber - 1) * 30;

    private ZodiacSign()
    {
    }

    public ZodiacSign(int id, string name, int sequenceNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Zodiac sign name cannot be empty.", nameof(name));
        }

        if (sequenceNumber < 1 || sequenceNumber > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), sequenceNumber, "Sequence number must be between 1 and 12.");
        }

        Id = id;
        Name = name;
        SequenceNumber = sequenceNumber;
    }
}
