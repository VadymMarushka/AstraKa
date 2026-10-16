namespace AstraKa.Application.CourseProgram;

/// <summary>
/// Result of an ADO.NET delete that can fail for a business reason, not just "not found" -
/// see <see cref="ICourseDayRepository.DeleteAsync"/> and <see cref="IDayExerciseRepository.DeleteAsync"/>.
/// </summary>
public enum DeleteOutcome
{
    Deleted,
    NotFound,
    HasDependencies,
}
