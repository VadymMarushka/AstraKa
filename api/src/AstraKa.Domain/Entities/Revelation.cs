using AstraKa.Domain.Exceptions;

namespace AstraKa.Domain.Entities;

/// <summary>
/// Personal note from a Starshepherd to one participant for an upcoming day, optionally paired with
/// extra <see cref="DayExercise"/> rows that reference this revelation. "Only the course author may
/// write one" needs the Course and the current user loaded, so that half of the rule is enforced by
/// the Application handler, not here.
/// </summary>
public class Revelation
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="Enrollment"/> ("Receives"), together with <see cref="CohortId"/>.</summary>
    public Guid WandererId { get; private set; }

    public Guid CohortId { get; private set; }

    /// <summary>FK to <see cref="CourseDay"/> ("Targets") - the day this note is meant for.</summary>
    public Guid CourseDayId { get; private set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public Enrollment Enrollment { get; set; } = null!;

    public CourseDay CourseDay { get; set; } = null!;

    public ICollection<DayExercise> ExtraExercises { get; private set; } = new List<DayExercise>();

    private Revelation()
    {
    }

    /// <summary>
    /// <paramref name="targetDayDate"/> and <paramref name="today"/> are only used to guard "only for
    /// a day that has not passed yet" - they are not stored as entity state.
    /// </summary>
    public Revelation(Guid id, Guid wandererId, Guid cohortId, Guid courseDayId, string message, DateTime createdAt, DateOnly targetDayDate, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }

        if (targetDayDate < today)
        {
            throw new DomainException("This day has already passed.");
        }

        Id = id;
        WandererId = wandererId;
        CohortId = cohortId;
        CourseDayId = courseDayId;
        Message = message;
        CreatedAt = createdAt;
    }
}
