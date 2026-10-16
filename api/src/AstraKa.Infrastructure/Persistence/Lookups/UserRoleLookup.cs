using AstraKa.Domain.Enums;

namespace AstraKa.Infrastructure.Persistence.Lookups;

/// <summary>
/// Backs the <c>UserRoles</c> table. Exists purely so <see cref="Domain.Entities.User.Role"/> has a
/// real FK constraint instead of a free-text string; nothing outside Infrastructure queries this
/// type directly - see <see cref="Configurations.UserRoleLookupConfiguration"/>.
/// </summary>
public class UserRoleLookup
{
    public UserRole Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
