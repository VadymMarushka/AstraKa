using AstraKa.Domain.Enums;

namespace AstraKa.Infrastructure.Persistence.Lookups;

/// <summary>
/// Backs the <c>CourseStatuses</c> table. Exists purely so <see cref="Domain.Entities.Course.Status"/>
/// has a real FK constraint instead of a free-text string; nothing outside Infrastructure queries
/// this type directly - see <see cref="Configurations.CourseStatusLookupConfiguration"/>.
/// </summary>
public class CourseStatusLookup
{
    public CourseStatus Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
