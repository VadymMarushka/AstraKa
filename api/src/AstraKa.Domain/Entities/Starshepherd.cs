namespace AstraKa.Domain.Entities;

/// <summary>
/// Specialist profile, 1:1 with <see cref="User"/>. Anyone can become one; <see cref="Title"/> is a
/// self-invented free-text title ("Grand Keeper of the Seventh Orbit").
/// </summary>
public class Starshepherd
{
    /// <summary>PK and FK to <see cref="User"/>.</summary>
    public Guid UserId { get; private set; }

    public string Title { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public ICollection<Course> Courses { get; private set; } = new List<Course>();

    public ICollection<Exercise> Exercises { get; private set; } = new List<Exercise>();

    private Starshepherd()
    {
    }

    public Starshepherd(Guid userId, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        UserId = userId;
        Title = title;
    }
}
