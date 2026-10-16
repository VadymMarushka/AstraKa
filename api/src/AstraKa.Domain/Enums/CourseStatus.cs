namespace AstraKa.Domain.Enums;

/// <summary>
/// Lifecycle of a <see cref="Entities.Course"/>. Only <see cref="Published"/> courses accept enrollments.
/// </summary>
public enum CourseStatus
{
    Draft,
    Published,
}
