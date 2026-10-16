namespace AstraKa.Domain.Entities;

/// <summary>
/// Client profile, 1:1 with <see cref="User"/>. Birth date is never stored - only the zodiac sign,
/// which is enough to derive the astral address.
/// </summary>
public class Wanderer
{
    /// <summary>PK and FK to <see cref="User"/>.</summary>
    public Guid UserId { get; private set; }

    public int ZodiacSignId { get; set; }

    public User User { get; set; } = null!;

    public ZodiacSign ZodiacSign { get; set; } = null!;

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    private Wanderer()
    {
    }

    public Wanderer(Guid userId, int zodiacSignId)
    {
        UserId = userId;
        ZodiacSignId = zodiacSignId;
    }
}
