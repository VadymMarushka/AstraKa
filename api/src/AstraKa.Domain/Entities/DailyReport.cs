using AstraKa.Domain.Exceptions;

namespace AstraKa.Domain.Entities;

/// <summary>
/// One Wanderer's check-in for one day of an <see cref="Enrollment"/>: which exercises were done and
/// how they felt on the Chaos-Imbalance-Harmony scale. At most one report per day per enrollment is a
/// DB-level uniqueness constraint, enforced by Infrastructure, not here.
/// </summary>
public class DailyReport
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="Enrollment"/> ("Submits"), together with <see cref="CohortId"/>.</summary>
    public Guid WandererId { get; private set; }

    public Guid CohortId { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>1-10: 1 = Chaos, middle = Imbalance, 10 = Harmony.</summary>
    public byte HarmonyLevel { get; private set; }

    public Enrollment Enrollment { get; private set; } = null!;

    public ICollection<ExerciseCompletion> Completions { get; private set; } = new List<ExerciseCompletion>();

    private DailyReport()
    {
    }

    public DailyReport(Guid id, Guid wandererId, Guid cohortId, DateOnly date, byte harmonyLevel)
    {
        if (harmonyLevel < 1 || harmonyLevel > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(harmonyLevel), harmonyLevel, "Harmony level must be between 1 and 10.");
        }

        Id = id;
        WandererId = wandererId;
        CohortId = cohortId;
        Date = date;
        HarmonyLevel = harmonyLevel;
    }

    /// <summary>
    /// Marks <paramref name="dayExerciseId"/> as done. <paramref name="allowedDayExerciseIds"/> is the
    /// set of exercises this enrollment may report against for the day (base program plus this
    /// enrollment's revelations), computed by the Application handler - Domain only enforces that the
    /// id is in that set and that it has not already been marked.
    /// </summary>
    public void AddCompletion(Guid dayExerciseId, IReadOnlyCollection<Guid> allowedDayExerciseIds, DateTime completedAt)
    {
        if (!allowedDayExerciseIds.Contains(dayExerciseId))
        {
            throw new DomainException("This exercise is not part of today's plan.");
        }

        if (Completions.Any(c => c.DayExerciseId == dayExerciseId))
        {
            throw new DomainException("This exercise has already been marked as completed.");
        }

        Completions.Add(new ExerciseCompletion(Guid.NewGuid(), Id, dayExerciseId, completedAt));
    }
}
