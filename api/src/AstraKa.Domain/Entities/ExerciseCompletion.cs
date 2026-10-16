namespace AstraKa.Domain.Entities;

/// <summary>
/// Marks one <see cref="DayExercise"/> as done within a <see cref="DailyReport"/>. Created only
/// through <see cref="DailyReport.AddCompletion"/>, which enforces the uniqueness and "allowed
/// exercise" rules.
/// </summary>
public class ExerciseCompletion
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="DailyReport"/> ("Contains").</summary>
    public Guid DailyReportId { get; private set; }

    /// <summary>FK to <see cref="DayExercise"/> ("Marks").</summary>
    public Guid DayExerciseId { get; private set; }

    public DateTime CompletedAt { get; private set; }

    public DailyReport DailyReport { get; set; } = null!;

    public DayExercise DayExercise { get; set; } = null!;

    private ExerciseCompletion()
    {
    }

    internal ExerciseCompletion(Guid id, Guid dailyReportId, Guid dayExerciseId, DateTime completedAt)
    {
        Id = id;
        DailyReportId = dailyReportId;
        DayExerciseId = dayExerciseId;
        CompletedAt = completedAt;
    }
}
