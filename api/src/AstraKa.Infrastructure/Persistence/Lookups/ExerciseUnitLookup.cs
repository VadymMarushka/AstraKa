using AstraKa.Domain.Enums;

namespace AstraKa.Infrastructure.Persistence.Lookups;

/// <summary>
/// Backs the <c>ExerciseUnits</c> table. Exists purely so <see cref="Domain.Entities.Exercise.Unit"/>
/// has a real FK constraint instead of a free-text string; nothing outside Infrastructure queries
/// this type directly - see <see cref="Configurations.ExerciseUnitLookupConfiguration"/>.
/// </summary>
public class ExerciseUnitLookup
{
    public ExerciseUnit Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
