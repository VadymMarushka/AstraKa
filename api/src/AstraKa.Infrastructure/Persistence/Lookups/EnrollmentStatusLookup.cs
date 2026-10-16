using AstraKa.Domain.Enums;

namespace AstraKa.Infrastructure.Persistence.Lookups;

/// <summary>
/// Backs the <c>EnrollmentStatuses</c> table. Exists purely so
/// <see cref="Domain.Entities.Enrollment.Status"/> has a real FK constraint instead of a free-text
/// string; nothing outside Infrastructure queries this type directly - see
/// <see cref="Configurations.EnrollmentStatusLookupConfiguration"/>.
/// </summary>
public class EnrollmentStatusLookup
{
    public EnrollmentStatus Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
