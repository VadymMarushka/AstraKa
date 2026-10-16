namespace AstraKa.Application.CourseProgram;

/// <summary>
/// ADO.NET CRUD for <c>DayExercises</c> - this table is ADO.NET's, not EF Core's, per CLAUDE.md's
/// "ADO.NET vs EF Core boundary". Implemented in Infrastructure with Microsoft.Data.SqlClient.
/// </summary>
public interface IDayExerciseRepository
{
    Task<Guid> CreateAsync(Guid courseDayId, Guid exerciseId, int orderNumber, int sets, int amount, Guid? revelationId, CancellationToken ct);

    Task<DayExerciseRecord?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<DayExerciseRecord>> GetByCourseDayIdAsync(Guid courseDayId, CancellationToken ct);

    /// <summary>Only <c>OrderNumber</c>/<c>Sets</c>/<c>Amount</c> are mutable - mirrors the Domain entity.</summary>
    Task<bool> UpdateAsync(Guid id, int orderNumber, int sets, int amount, CancellationToken ct);

    /// <summary><see cref="DeleteOutcome.HasDependencies"/> when it still has ExerciseCompletion rows (SQL Server error 547).</summary>
    Task<DeleteOutcome> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed record DayExerciseRecord(Guid Id, Guid CourseDayId, Guid ExerciseId, Guid? RevelationId, int OrderNumber, int Sets, int Amount);
