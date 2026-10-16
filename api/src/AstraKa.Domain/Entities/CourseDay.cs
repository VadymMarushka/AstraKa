namespace AstraKa.Domain.Entities;

/// <summary>
/// One day of a <see cref="Course"/> program. A day may have only a <see cref="MeetLink"/> and no
/// exercises at all.
/// </summary>
public class CourseDay
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="Course"/> ("Consists of").</summary>
    public Guid CourseId { get; private set; }

    /// <summary>1-based. Unique within the course.</summary>
    public int DayNumber { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Video meeting with the Starshepherd, if any.</summary>
    public string? MeetLink { get; private set; }

    public Course Course { get; private set; } = null!;

    public ICollection<DayExercise> DayExercises { get; private set; } = new List<DayExercise>();

    private CourseDay()
    {
    }

    public CourseDay(Guid id, Guid courseId, int dayNumber, string title, string? description = null, string? meetLink = null)
    {
        if (dayNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayNumber), dayNumber, "Day number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        Id = id;
        CourseId = courseId;
        DayNumber = dayNumber;
        Title = title;
        Description = description;
        MeetLink = meetLink;
    }

    /// <summary>Calendar date this day falls on within a specific cohort.</summary>
    public DateOnly GetDate(DateOnly cohortStartDate)
    {
        return cohortStartDate.AddDays(DayNumber - 1);
    }
}
