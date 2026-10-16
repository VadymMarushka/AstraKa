namespace AstraKa.Domain.Enums;

/// <summary>
/// Stored status of an <see cref="Entities.Enrollment"/>. "Completed" is computed, not stored - see
/// <see cref="Entities.Enrollment.IsCompleted"/>.
/// </summary>
public enum EnrollmentStatus
{
    Active,
    Left,
}
