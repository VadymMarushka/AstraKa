namespace AstraKa.Domain.Enums;

/// <summary>
/// Who a <see cref="Entities.User"/> is in the order: a client, a specialist, or the admin.
/// </summary>
public enum UserRole
{
    Wanderer = 1,
    Starshepherd = 2,
    Oracle = 3,
}
