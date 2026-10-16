using AstraKa.Domain.Enums;

namespace AstraKa.Domain.Entities;

/// <summary>
/// Catalog entry for an astral exercise. A null <see cref="AuthorId"/> marks a system exercise,
/// seeded and editable only by the Oracle - that check is authorization and lives in the Api's
/// resource-based <c>IAuthorizationHandler</c> (lab 3), not here.
/// </summary>
public class Exercise
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="Starshepherd"/>. Null = system exercise.</summary>
    public Guid? AuthorId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>How to perform the exercise, separate from <see cref="Description"/>'s lore line.</summary>
    public string Instructions { get; private set; } = string.Empty;

    public ExerciseUnit Unit { get; private set; }

    public string? VideoUrl { get; private set; }

    public Starshepherd? Author { get; private set; }

    public ICollection<DayExercise> DayExercises { get; private set; } = new List<DayExercise>();

    public bool IsSystemExercise => AuthorId is null;

    private Exercise()
    {
    }

    public Exercise(Guid id, Guid? authorId, string name, string description, string instructions, ExerciseUnit unit, string? videoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Exercise name cannot be empty.", nameof(name));
        }

        Id = id;
        AuthorId = authorId;
        Name = name;
        Description = description;
        Instructions = instructions;
        Unit = unit;
        VideoUrl = videoUrl;
    }

    /// <summary>Full update of the mutable fields - same non-empty-name guard as the constructor.</summary>
    public void Update(string name, string description, string instructions, ExerciseUnit unit, string? videoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Exercise name cannot be empty.", nameof(name));
        }

        Name = name;
        Description = description;
        Instructions = instructions;
        Unit = unit;
        VideoUrl = videoUrl;
    }
}
