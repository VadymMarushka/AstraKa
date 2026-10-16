using AstraKa.Domain.Enums;

namespace AstraKa.Domain.Entities;

/// <summary>
/// Every person who signs in. Authentication itself is done by an external OIDC provider; this
/// entity only stores the claims the app needs plus the role. Oracle is just a User with role
/// <see cref="UserRole.Oracle"/> - it has no profile table of its own.
/// </summary>
public class User
{
    public Guid Id { get; private set; }

    /// <summary>The `sub` claim from the provider's JWT. Unique.</summary>
    public string ExternalId { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public UserRole Role { get; private set; }

    /// <summary>Set when <see cref="Role"/> is <see cref="UserRole.Wanderer"/>.</summary>
    public Wanderer? Wanderer { get; set; }

    /// <summary>Set when <see cref="Role"/> is <see cref="UserRole.Starshepherd"/>.</summary>
    public Starshepherd? Starshepherd { get; set; }

    private User()
    {
    }

    public User(Guid id, string externalId, string email, string name, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("External id cannot be empty.", nameof(externalId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        Id = id;
        ExternalId = externalId;
        Email = email;
        Name = name;
        Role = role;
    }
}
