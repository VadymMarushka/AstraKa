namespace AstraKa.Domain.Entities;

/// <summary>
/// One exercise placed on a <see cref="CourseDay"/>, with how many sets/amount to perform.
/// A null <see cref="RevelationId"/> means it is part of the base program, shown to everyone;
/// otherwise it was added by a specific <see cref="Revelation"/> for one participant.
/// </summary>
public class DayExercise
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="CourseDay"/> ("Includes").</summary>
    public Guid CourseDayId { get; private set; }

    /// <summary>FK to <see cref="Exercise"/> ("Used in").</summary>
    public Guid ExerciseId { get; private set; }

    /// <summary>FK to <see cref="Revelation"/> ("Adds"). Null = base program.</summary>
    public Guid? RevelationId { get; private set; }

    public int OrderNumber { get; private set; }

    public int Sets { get; private set; }

    /// <summary>Per set, in the exercise's <c>Unit</c> (reps / seconds / breaths).</summary>
    public int Amount { get; private set; }

    public CourseDay CourseDay { get; private set; } = null!;

    public Exercise Exercise { get; private set; } = null!;

    public Revelation? Revelation { get; private set; }

    public bool IsBaseProgram => RevelationId is null;

    private DayExercise()
    {
    }

    public DayExercise(Guid id, Guid courseDayId, Guid exerciseId, int orderNumber, int sets, int amount, Guid? revelationId = null)
    {
        if (sets <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sets), sets, "Sets must be greater than zero.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        Id = id;
        CourseDayId = courseDayId;
        ExerciseId = exerciseId;
        RevelationId = revelationId;
        OrderNumber = orderNumber;
        Sets = sets;
        Amount = amount;
    }
}
