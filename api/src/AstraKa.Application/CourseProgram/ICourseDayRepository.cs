namespace AstraKa.Application.CourseProgram;

/// <summary>
/// ADO.NET CRUD for <c>CourseDays</c> - this table is ADO.NET's, not EF Core's, per CLAUDE.md's
/// "ADO.NET vs EF Core boundary". Implemented in Infrastructure with Microsoft.Data.SqlClient.
/// </summary>
public interface ICourseDayRepository
{
    Task<Guid> CreateAsync(Guid courseId, int dayNumber, string title, string? description, string? meetLink, CancellationToken ct);

    Task<CourseDayRecord?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<CourseDayRecord>> GetByCourseIdAsync(Guid courseId, CancellationToken ct);

    /// <summary>Only <c>Title</c>/<c>Description</c>/<c>MeetLink</c> are mutable - mirrors the Domain entity.</summary>
    Task<bool> UpdateAsync(Guid id, string title, string? description, string? meetLink, CancellationToken ct);

    /// <summary>
    /// <see cref="DeleteOutcome.HasDependencies"/> when the day (or, cascading, one of its
    /// DayExercises) still has ExerciseCompletion rows (SQL Server error 547).
    /// </summary>
    Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed record CourseDayRecord(Guid Id, Guid CourseId, int DayNumber, string Title, string? Description, string? MeetLink);
