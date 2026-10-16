namespace AstraKa.Application.CourseProgram;

/// <summary>
/// The one ADO.NET transaction over Courses + CourseDays + DayExercises per CLAUDE.md: "create
/// course with full program". Commits only if every row inserts cleanly; any failure (e.g. an
/// <see cref="CourseProgramExerciseInput.ExerciseId"/> that doesn't exist) rolls back everything,
/// including the Courses row.
/// </summary>
public interface ICourseProgramRepository
{
    Task<Guid> CreateCourseWithProgramAsync(CourseProgramInput input, CancellationToken ct);
}

public sealed record CourseProgramInput(
    Guid AuthorId,
    string Name,
    string Description,
    string Promise,
    decimal Price,
    IReadOnlyList<CourseProgramDayInput> Days);

public sealed record CourseProgramDayInput(
    int DayNumber,
    string Title,
    string? Description,
    string? MeetLink,
    IReadOnlyList<CourseProgramExerciseInput> Exercises);

/// <summary>Always base program - created days never carry a RevelationId.</summary>
public sealed record CourseProgramExerciseInput(Guid ExerciseId, int OrderNumber, int Sets, int Amount);
